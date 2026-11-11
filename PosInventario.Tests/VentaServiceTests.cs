using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class VentaServiceTests
{
    private sealed class PruebaVentas : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public InventarioService InventarioService { get; }
        public CajaService CajaService { get; }
        public VentaService VentaService { get; }

        private PruebaVentas(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
            CajaService = new CajaService(db, Sesion);
            VentaService = new VentaService(db, Sesion);
        }

        public static async Task<PruebaVentas> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaVentas(conexion, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _conexion.DisposeAsync();
        }

        public async Task<Usuario> CrearAdministradorEnSesionAsync()
        {
            var resultado = await UsuarioService.CrearPrimerAdministradorAsync(
                "admin", "Administrador", "claveSegura123");

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);
            return resultado.Valor!;
        }

        public async Task<Producto> CrearProductoAsync(string sku)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, $"Producto {sku}", null, 100m, 50m, 0);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task AbrirCajaAsync()
        {
            var resultado = await CajaService.AbrirAsync(1000m);
            Assert.True(resultado.Correcta, resultado.Mensaje);
        }

        public async Task<Venta> VenderAsync(
            Producto producto, MetodoPago metodo = MetodoPago.Efectivo)
        {
            var total = producto.PrecioVenta;
            var recibido = metodo == MetodoPago.Efectivo ? total : total;

            var resultado = await VentaService.RegistrarAsync(
                [new LineaVenta(producto.Id, 1)], metodo, recibido);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task ObtenerDelDiaDevuelveSoloCompletadasDeHoy()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-001");
        await prueba.InventarioService.RegistrarEntradaAsync(producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var hoy = await prueba.VenderAsync(producto);
        var anulada = await prueba.VenderAsync(producto);
        var ayer = await prueba.VenderAsync(producto);

        var anuladaEnBase = await prueba.Db.Ventas.FirstAsync(v => v.Id == anulada.Id);
        anuladaEnBase.Fecha = DateTime.Today.AddHours(8);
        await prueba.Db.SaveChangesAsync();

        var resultadoAnulacion = await prueba.VentaService.AnularAsync(anulada.Id);
        Assert.True(resultadoAnulacion.Correcta, resultadoAnulacion.Mensaje);

        var ayerEnBase = await prueba.Db.Ventas.FirstAsync(v => v.Id == ayer.Id);
        ayerEnBase.Fecha = DateTime.Today.AddDays(-1).AddHours(10);
        await prueba.Db.SaveChangesAsync();

        var delDia = await prueba.VentaService.ObtenerDelDiaAsync();

        Assert.Single(delDia);
        Assert.Equal(hoy.Id, delDia[0].Id);
    }

    [Fact]
    public async Task ObtenerDelDiaIncluyeUsuario()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        var admin = await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-002");
        await prueba.InventarioService.RegistrarEntradaAsync(producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        await prueba.VenderAsync(producto);

        var delDia = await prueba.VentaService.ObtenerDelDiaAsync();

        Assert.Single(delDia);
        Assert.NotNull(delDia[0].Usuario);
        Assert.Equal(admin.Nombre, delDia[0].Usuario!.Nombre);
    }

    [Fact]
    public async Task ObtenerRecientesOrdenaDescendenteYRespetaLimite()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-003");
        await prueba.InventarioService.RegistrarEntradaAsync(producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var primera = await prueba.VenderAsync(producto);
        var segunda = await prueba.VenderAsync(producto);
        var tercera = await prueba.VenderAsync(producto);

        var recientes = await prueba.VentaService.ObtenerRecientesAsync();
        var limitadas = await prueba.VentaService.ObtenerRecientesAsync(2);

        Assert.Equal(3, recientes.Count);
        Assert.Equal(tercera.Id, recientes[0].Id);
        Assert.Equal(segunda.Id, recientes[1].Id);
        Assert.Equal(primera.Id, recientes[2].Id);

        Assert.Equal(2, limitadas.Count);
        Assert.Equal(tercera.Id, limitadas[0].Id);
        Assert.Equal(segunda.Id, limitadas[1].Id);
    }

    [Fact]
    public async Task ObtenerRecientesIncluyeAnuladas()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-004");
        await prueba.InventarioService.RegistrarEntradaAsync(producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var primera = await prueba.VenderAsync(producto);
        await prueba.VenderAsync(producto);

        var anulacion = await prueba.VentaService.AnularAsync(primera.Id);
        Assert.True(anulacion.Correcta, anulacion.Mensaje);

        var recientes = await prueba.VentaService.ObtenerRecientesAsync();

        Assert.Equal(2, recientes.Count);
        Assert.Contains(recientes, v => v.Estado == EstadoVenta.Anulada && v.Id == primera.Id);
    }

    [Fact]
    public async Task ObtenerRecientesIncluyeDetallesParaElTicket()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-005");
        await prueba.InventarioService.RegistrarEntradaAsync(producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        await prueba.VenderAsync(producto);

        var recientes = await prueba.VentaService.ObtenerRecientesAsync(1);

        Assert.Single(recientes);
        Assert.Single(recientes[0].Detalles);
        Assert.NotNull(recientes[0].Detalles.First().Producto);
    }

    [Fact]
    public async Task RegistrarConDescuentoGuardaSubtotalYTotal()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-006");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 2)], MetodoPago.Efectivo, 200m, 20m);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Equal(200m, resultado.Valor!.Subtotal);
        Assert.Equal(20m, resultado.Valor.Descuento);
        Assert.Equal(180m, resultado.Valor.Total);
        Assert.Equal(20m, resultado.Valor.Vuelto);

        var movimiento = await prueba.Db.MovimientosCaja
            .SingleAsync(m => m.IdVenta == resultado.Valor.Id);

        Assert.Equal(180m, movimiento.Monto);
    }
}
