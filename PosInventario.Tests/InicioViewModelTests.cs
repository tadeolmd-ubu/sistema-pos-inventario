using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class InicioViewModelTests
{
    private sealed class PruebaInicio : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public ProveedorService ProveedorService { get; }
        public InventarioService InventarioService { get; }
        public CajaService CajaService { get; }
        public CompraService CompraService { get; }
        public VentaService VentaService { get; }

        private PruebaInicio(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            ProveedorService = new ProveedorService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
            CajaService = new CajaService(db, Sesion);
            CompraService = new CompraService(db, Sesion);
            VentaService = new VentaService(db, Sesion);
        }

        public static async Task<PruebaInicio> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaInicio(conexion, db);
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
                "cajero", "Cajero", "claveCajero1", RolUsuario.Cajero);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);
            return resultado.Valor!;
        }

        public async Task<Producto> CrearProductoAsync(
            string sku, int stock = 0, int stockMinimo = 0, decimal precio = 100m)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, $"Producto {sku}", null, precio, 50m, stockMinimo);

            Assert.True(resultado.Correcta, resultado.Mensaje);

            if (stock > 0)
            {
                var entrada = await InventarioService.RegistrarEntradaAsync(
                    resultado.Valor!.Id, stock, "Carga inicial");

                Assert.True(entrada.Correcta, entrada.Mensaje);
            }

            return resultado.Valor!;
        }

        public async Task AbrirCajaAsync(decimal monto = 1000m)
        {
            var resultado = await CajaService.AbrirAsync(monto);
            Assert.True(resultado.Correcta, resultado.Mensaje);
        }

        public async Task<Venta> VenderAsync(
            Producto producto, MetodoPago metodo = MetodoPago.Efectivo)
        {
            var total = producto.PrecioVenta;

            var resultado = await VentaService.RegistrarAsync(
                [new LineaVenta(producto.Id, 1)],
                [
                    new LineaPago(
                        metodo,
                        total,
                        metodo == MetodoPago.Efectivo ? total : null)
                ]);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task<Proveedor> CrearProveedorAsync()
        {
            var resultado = await ProveedorService.AgregarAsync(
                "Proveedor dashboard", "11 2222-3333", null, null);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task CrearCompraAsync(Producto producto, Proveedor proveedor)
        {
            var resultado = await CompraService.RegistrarAsync(
                proveedor.Id, null, [new LineaCompra(producto.Id, 1, 50m)]);

            Assert.True(resultado.Correcta, resultado.Mensaje);
        }

        public InicioViewModel CrearVista()
            => new(VentaService, ProductoService, CajaService, CompraService, Sesion);
    }

    [Fact]
    public async Task SinVentasMuestraCeros()
    {
        await using var prueba = await PruebaInicio.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var vm = prueba.CrearVista();
        await vm.CargarAsync();

        Assert.Equal(0, vm.VentasDelDia);
        Assert.Equal(0m, vm.TotalDelDia);
        Assert.Equal(0m, vm.TicketPromedio);
        Assert.Equal(0m, vm.TotalEfectivo);
        Assert.False(vm.CajaAbierta);
        Assert.Equal("Cerrada", vm.EstadoCajaTexto);
        Assert.True(vm.EsAdministrador);
        Assert.False(vm.HayUltimasVentas);
    }

    [Fact]
    public async Task CalculaTotalesDelDiaYMetodos()
    {
        await using var prueba = await PruebaInicio.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("INI-001", stock: 10);
        await prueba.AbrirCajaAsync(1000m);

        await prueba.VenderAsync(producto, MetodoPago.Efectivo);
        await prueba.VenderAsync(producto, MetodoPago.Tarjeta);
        await prueba.VenderAsync(producto, MetodoPago.Transferencia);

        var vm = prueba.CrearVista();
        await vm.CargarAsync();

        Assert.Equal(3, vm.VentasDelDia);
        Assert.Equal(300m, vm.TotalDelDia);
        Assert.Equal(100m, vm.TotalEfectivo);
        Assert.Equal(100m, vm.TotalTarjeta);
        Assert.Equal(100m, vm.TotalTransferencia);
        Assert.Equal(100m, vm.TicketPromedio);
        Assert.True(vm.CajaAbierta);
        Assert.Equal("Abierta", vm.EstadoCajaTexto);
        Assert.Equal(1100m, vm.EfectivoEsperado);
        Assert.Equal(3, vm.UltimasVentas.Count);
        Assert.True(vm.HayUltimasVentas);
    }

    [Fact]
    public async Task VentaAnuladaNoSumaAlDia()
    {
        await using var prueba = await PruebaInicio.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("INI-002", stock: 5);
        await prueba.AbrirCajaAsync(1000m);

        var venta = await prueba.VenderAsync(producto);
        var anulacion = await prueba.VentaService.AnularAsync(venta.Id);
        Assert.True(anulacion.Correcta, anulacion.Mensaje);

        var vm = prueba.CrearVista();
        await vm.CargarAsync();

        Assert.Equal(0, vm.VentasDelDia);
        Assert.Equal(0m, vm.TotalDelDia);
        Assert.Equal(0m, vm.TotalEfectivo);
        Assert.Equal(1000m, vm.EfectivoEsperado);
        Assert.Single(vm.UltimasVentas);
        Assert.Equal(EstadoVenta.Anulada, vm.UltimasVentas[0].Estado);
        Assert.True(vm.HayUltimasVentas);
    }

    [Fact]
    public async Task AdminCargaStockBajoYCompras()
    {
        await using var prueba = await PruebaInicio.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var bajo = await prueba.CrearProductoAsync(
            "INI-BAJO", stock: 2, stockMinimo: 5);
        var paraCompra = await prueba.CrearProductoAsync("INI-COMPRA");
        var proveedor = await prueba.CrearProveedorAsync();

        await prueba.CrearCompraAsync(paraCompra, proveedor);

        var vm = prueba.CrearVista();
        await vm.CargarAsync();

        Assert.True(vm.EsAdministrador);
        Assert.Equal(2, vm.ProductosActivos);
        Assert.Equal(1, vm.ProductosStockBajo);
        Assert.Single(vm.StockBajo);
        Assert.Equal(bajo.Id, vm.StockBajo[0].Id);
        Assert.Single(vm.UltimasCompras);
        Assert.True(vm.HayCompras);
        Assert.True(vm.HayStockBajo);
    }

    [Fact]
    public async Task CajeroNoVeDatosDeAdmin()
    {
        await using var prueba = await PruebaInicio.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync(
            "INI-CAJERO", stock: 2, stockMinimo: 5);
        var proveedor = await prueba.CrearProveedorAsync();
        await prueba.CrearCompraAsync(producto, proveedor);
        await prueba.AbrirCajaAsync(1000m);
        await prueba.VenderAsync(producto);

        await prueba.CrearCajeroEnSesionAsync();

        var vm = prueba.CrearVista();
        await vm.CargarAsync();

        Assert.False(vm.EsAdministrador);
        Assert.Equal(0, vm.ProductosActivos);
        Assert.Equal(0, vm.ProductosStockBajo);
        Assert.Empty(vm.StockBajo);
        Assert.Empty(vm.UltimasCompras);
        Assert.True(vm.CajaAbierta);
        Assert.Equal(1, vm.VentasDelDia);
        Assert.Single(vm.UltimasVentas);
    }

    [Fact]
    public async Task CalculaTotalesDeUnaVentaMixta()
    {
        await using var prueba = await PruebaInicio.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-INI", stock: 5);
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [
                new LineaPago(MetodoPago.Efectivo, 40m, 40m),
                new LineaPago(MetodoPago.Tarjeta, 60m)
            ]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var vm = prueba.CrearVista();
        await vm.CargarAsync();

        Assert.Equal(1, vm.VentasDelDia);
        Assert.Equal(100m, vm.TotalDelDia);
        Assert.Equal(40m, vm.TotalEfectivo);
        Assert.Equal(60m, vm.TotalTarjeta);
        Assert.Equal(0m, vm.TotalTransferencia);
    }
}
