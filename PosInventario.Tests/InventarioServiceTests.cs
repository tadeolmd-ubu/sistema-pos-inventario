using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class InventarioServiceTests
{
    private const string MotivoEntrada = "Carga inicial";
    private const string MotivoSalida = "Merma";
    private const string MotivoAjuste = "Conteo físico";

    private sealed class PruebaInventario : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public InventarioService InventarioService { get; }

        private PruebaInventario(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
        }

        public static async Task<PruebaInventario> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaInventario(conexion, db);
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

        public async Task<Usuario> CrearCajeroEnSesionAsync()
        {
            var resultado = await UsuarioService.AgregarAsync(
                "laura", "Laura Gómez", "claveDeLaura1", RolUsuario.Cajero);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);

            return resultado.Valor!;
        }

        public async Task<Producto> CrearProductoAsync(
            string sku = "ABC-001", string nombre = "Producto de prueba")
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, nombre, null, 100m, 50m, 0);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task<Producto> RecargarProductoAsync(int id)
            => await Db.Productos
                .AsNoTracking()
                .FirstAsync(p => p.Id == id);

        public async Task<int> ContarMovimientosAsync()
            => await Db.MovimientosInventario.CountAsync();
    }

    [Fact]
    public async Task EntradaAumentaStockYCreaMovimiento()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        var resultado = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, MotivoEntrada);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var actualizado = await prueba.RecargarProductoAsync(producto.Id);
        Assert.Equal(10, actualizado.Stock);

        var movimiento = resultado.Valor!;
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.Equal(10, movimiento.Cantidad);
        Assert.Equal(0, movimiento.StockAnterior);
        Assert.Equal(10, movimiento.StockResultante);
        Assert.Equal(MotivoEntrada, movimiento.Referencia);
        Assert.Equal(1, await prueba.ContarMovimientosAsync());
    }

    [Fact]
    public async Task SalidaReduceStock()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, MotivoEntrada);

        var resultado = await prueba.InventarioService.RegistrarSalidaAsync(
            producto.Id, 3, MotivoSalida);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var actualizado = await prueba.RecargarProductoAsync(producto.Id);
        Assert.Equal(7, actualizado.Stock);
        Assert.Equal(3, resultado.Valor!.Cantidad);
        Assert.Equal(10, resultado.Valor.StockAnterior);
        Assert.Equal(7, resultado.Valor.StockResultante);
    }

    [Fact]
    public async Task SalidaRechazaStockInsuficiente()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, MotivoEntrada);

        var resultado = await prueba.InventarioService.RegistrarSalidaAsync(
            producto.Id, 10, MotivoSalida);

        Assert.False(resultado.Correcta);

        var actualizado = await prueba.RecargarProductoAsync(producto.Id);
        Assert.Equal(5, actualizado.Stock);
        Assert.Equal(1, await prueba.ContarMovimientosAsync());
    }

    [Fact]
    public async Task AjusteHaciaArribaRegistraLaDiferencia()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, MotivoEntrada);

        var resultado = await prueba.InventarioService.RegistrarAjusteAsync(
            producto.Id, 15, MotivoAjuste);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var actualizado = await prueba.RecargarProductoAsync(producto.Id);
        Assert.Equal(15, actualizado.Stock);
        Assert.Equal(5, resultado.Valor!.Cantidad);
        Assert.Equal(10, resultado.Valor.StockAnterior);
        Assert.Equal(15, resultado.Valor.StockResultante);
    }

    [Fact]
    public async Task AjusteHaciaAbajoRegistraLaDiferencia()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, MotivoEntrada);

        var resultado = await prueba.InventarioService.RegistrarAjusteAsync(
            producto.Id, 4, MotivoAjuste);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var actualizado = await prueba.RecargarProductoAsync(producto.Id);
        Assert.Equal(4, actualizado.Stock);
        Assert.Equal(6, resultado.Valor!.Cantidad);
        Assert.Equal(10, resultado.Valor.StockAnterior);
        Assert.Equal(4, resultado.Valor.StockResultante);
    }

    [Fact]
    public async Task AjusteSinCambiosEsRechazado()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        var resultado = await prueba.InventarioService.RegistrarAjusteAsync(
            producto.Id, 0, MotivoAjuste);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task CantidadCeroONegativaEsRechazada()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        var cero = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 0, MotivoEntrada);
        var negativo = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, -5, MotivoEntrada);

        Assert.False(cero.Correcta);
        Assert.False(negativo.Correcta);
        Assert.Equal(0, await prueba.ContarMovimientosAsync());
    }

    [Fact]
    public async Task ProductoArchivadoEsRechazado()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        var archivado = await prueba.ProductoService.ArchivarAsync(producto.Id);
        Assert.True(archivado.Correcta, archivado.Mensaje);

        var resultado = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, MotivoEntrada);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task MotivoVacioEsRechazado()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        var resultado = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "   ");

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task MotivoInvalidoParaElTipoEsRechazado()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        var resultado = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, MotivoSalida);

        Assert.False(resultado.Correcta);
        Assert.Equal(0, await prueba.ContarMovimientosAsync());
    }

    [Fact]
    public async Task ElMovimientoGuardaElUsuarioDeLaSesion()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        var admin = await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        var resultado = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, MotivoEntrada);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Equal(admin.Id, resultado.Valor!.IdUsuario);

        var movimientos = await prueba.InventarioService.ObtenerMovimientosAsync();
        Assert.Equal(admin.Nombre, movimientos[0].Usuario.Nombre);
    }

    [Fact]
    public async Task ObtenerMovimientosFiltraYOrdena()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var productoA = await prueba.CrearProductoAsync("ABC-001", "Producto A");
        var productoB = await prueba.CrearProductoAsync("ABC-002", "Producto B");

        await prueba.InventarioService.RegistrarEntradaAsync(
            productoA.Id, 5, MotivoEntrada);
        await prueba.InventarioService.RegistrarSalidaAsync(
            productoA.Id, 2, MotivoSalida);
        await prueba.InventarioService.RegistrarEntradaAsync(
            productoB.Id, 10, MotivoEntrada);

        var deA = await prueba.InventarioService.ObtenerMovimientosAsync(productoA.Id);
        var todos = await prueba.InventarioService.ObtenerMovimientosAsync();

        Assert.Equal(2, deA.Count);
        Assert.Equal(TipoMovimiento.Salida, deA[0].Tipo);
        Assert.Equal(3, todos.Count);
    }

    [Fact]
    public async Task ObtenerMovimientosPorRangoFiltraFechasYProducto()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var productoA = await prueba.CrearProductoAsync("ABC-001", "Producto A");
        var productoB = await prueba.CrearProductoAsync("ABC-002", "Producto B");

        await prueba.InventarioService.RegistrarEntradaAsync(
            productoA.Id, 5, MotivoEntrada);
        await prueba.InventarioService.RegistrarEntradaAsync(
            productoB.Id, 8, MotivoEntrada);

        var movimientos = await prueba.Db.MovimientosInventario
            .OrderBy(m => m.Id)
            .ToListAsync();

        movimientos[0].Fecha = new DateTime(2026, 1, 10, 9, 0, 0);
        movimientos[1].Fecha = new DateTime(2026, 2, 10, 9, 0, 0);
        await prueba.Db.SaveChangesAsync();

        var febrero = await prueba.InventarioService
            .ObtenerMovimientosPorRangoAsync(
                new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        Assert.Single(febrero);
        Assert.Equal(productoB.Id, febrero[0].IdProducto);
        Assert.NotNull(febrero[0].Producto);
        Assert.NotNull(febrero[0].Usuario);

        var deAEnEnero = await prueba.InventarioService
            .ObtenerMovimientosPorRangoAsync(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                productoA.Id);

        Assert.Single(deAEnEnero);
        Assert.Equal(productoA.Id, deAEnEnero[0].IdProducto);
    }

    [Fact]
    public async Task MovimientosPaginadosFiltranFechasProductoYDevuelvenTotal()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var productoA = await prueba.CrearProductoAsync("PAG-001", "Producto A");
        var productoB = await prueba.CrearProductoAsync("PAG-002", "Producto B");

        await prueba.InventarioService.RegistrarEntradaAsync(
            productoA.Id, 10, MotivoEntrada);
        await prueba.InventarioService.RegistrarSalidaAsync(
            productoA.Id, 2, MotivoSalida);
        await prueba.InventarioService.RegistrarEntradaAsync(
            productoB.Id, 8, MotivoEntrada);

        var paginaUno = await prueba.InventarioService
            .ObtenerMovimientosPorRangoPaginadosAsync(
                DateTime.Today.AddDays(-1), DateTime.Today,
                idProducto: null, pagina: 1, tamanoPagina: 2);

        Assert.Equal(3, paginaUno.TotalRegistros);
        Assert.Equal(2, paginaUno.Items.Count);
        Assert.NotNull(paginaUno.Items[0].Producto);
        Assert.NotNull(paginaUno.Items[0].Usuario);

        var paginaDos = await prueba.InventarioService
            .ObtenerMovimientosPorRangoPaginadosAsync(
                DateTime.Today.AddDays(-1), DateTime.Today,
                idProducto: null, pagina: 2, tamanoPagina: 2);

        Assert.Single(paginaDos.Items);

        var soloA = await prueba.InventarioService
            .ObtenerMovimientosPorRangoPaginadosAsync(
                DateTime.Today.AddDays(-1), DateTime.Today,
                idProducto: productoA.Id, pagina: 1, tamanoPagina: 10);

        Assert.Equal(2, soloA.TotalRegistros);
        Assert.All(soloA.Items, m => Assert.Equal(productoA.Id, m.IdProducto));
    }

    [Fact]
    public async Task RegistroFallidoNoCambiaStockNiCreaMovimiento()
    {
        await using var prueba = await PruebaInventario.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, MotivoEntrada);

        await prueba.InventarioService.RegistrarSalidaAsync(
            producto.Id, 99, MotivoSalida);

        var actualizado = await prueba.RecargarProductoAsync(producto.Id);

        Assert.Equal(5, actualizado.Stock);
        Assert.Equal(1, await prueba.ContarMovimientosAsync());
    }

    [Fact]
    public async Task NoAdministradorNoPuedeRegistrarMovimientos()
    {
        await using var prueba = await PruebaInventario.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        await prueba.CrearCajeroEnSesionAsync();

        var resultado = await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, MotivoEntrada);

        Assert.False(resultado.Correcta);
        Assert.Equal(0, await prueba.ContarMovimientosAsync());
    }
}
