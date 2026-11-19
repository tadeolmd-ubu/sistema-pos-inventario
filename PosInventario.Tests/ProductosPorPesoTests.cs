using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class ProductosPorPesoTests
{
    private sealed class PruebaPeso : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public ProveedorService ProveedorService { get; }
        public InventarioService InventarioService { get; }
        public CompraService CompraService { get; }
        public CajaService CajaService { get; }
        public VentaService VentaService { get; }
        public ReporteService ReporteService { get; }
        public ImportacionDatosService ImportacionService { get; }

        private PruebaPeso(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            ProveedorService = new ProveedorService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
            CompraService = new CompraService(db, Sesion);
            CajaService = new CajaService(db, Sesion);
            VentaService = new VentaService(db, Sesion);
            ReporteService = new ReporteService(db, Sesion);
            ImportacionService = new ImportacionDatosService(db, Sesion);
        }

        public static async Task<PruebaPeso> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaPeso(conexion, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _conexion.DisposeAsync();
        }

        public async Task CrearAdministradorEnSesionAsync()
        {
            var resultado = await UsuarioService.CrearPrimerAdministradorAsync(
                "admin", "Administrador", "claveSegura123");

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);
        }

        public async Task<Producto> CrearProductoAsync(
            string sku,
            UnidadMedida unidad,
            decimal precio = 120m,
            decimal costo = 80m,
            decimal stockMinimo = 0m)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, $"Producto {sku}", null, precio, costo, stockMinimo, unidad);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task AbrirCajaAsync()
        {
            var resultado = await CajaService.AbrirAsync(1000m);
            Assert.True(resultado.Correcta, resultado.Mensaje);
        }

        public async Task<Proveedor> CrearProveedorAsync()
        {
            var resultado = await ProveedorService.AgregarAsync(
                "Distribuidora Central", "555-1000", null, null);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task VendeProductoPorPesoYDescuentaStock()
    {
        await using var prueba = await PruebaPeso.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync(
            "KG-001", UnidadMedida.Kilogramo);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 2.500m, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 0.250m)],
            [new LineaPago(MetodoPago.Efectivo, 30m, 30m)]);

        Assert.True(venta.Correcta, venta.Mensaje);
        Assert.Equal(30m, venta.Valor!.Total);
        Assert.Equal(0.250m, venta.Valor.Detalles.Single().Cantidad);
        Assert.Equal(0.250m, venta.Valor.Detalles.Single().CantidadPendiente);

        var actualizado = await prueba.Db.Productos
            .SingleAsync(p => p.Id == producto.Id);

        Assert.Equal(2.250m, actualizado.Stock);

        var reporte = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia);

        var vendido = reporte.Valor!.Productos.Single();
        Assert.Equal(0.250m, vendido.Cantidad);
        Assert.Equal("0.25 kg", vendido.CantidadTexto);
    }

    [Fact]
    public async Task RechazaDecimalesEnProductoPorPieza()
    {
        await using var prueba = await PruebaPeso.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync(
            "PZA-001", UnidadMedida.Pieza);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5m, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 0.5m)],
            [new LineaPago(MetodoPago.Efectivo, 60m, 60m)]);

        Assert.False(venta.Correcta);
        Assert.Contains("entero", venta.Mensaje);

        var actualizado = await prueba.Db.Productos
            .SingleAsync(p => p.Id == producto.Id);

        Assert.Equal(5m, actualizado.Stock);
    }

    [Fact]
    public async Task CompraPorPesoActualizaStockYCosto()
    {
        await using var prueba = await PruebaPeso.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync(
            "KG-002", UnidadMedida.Kilogramo);
        var proveedor = await prueba.CrearProveedorAsync();

        var compra = await prueba.CompraService.RegistrarAsync(
            proveedor.Id,
            "FAC-001",
            [new LineaCompra(producto.Id, 1.250m, 80m)]);

        Assert.True(compra.Correcta, compra.Mensaje);
        Assert.Equal(100m, compra.Valor!.Total);
        Assert.Equal(1.250m, compra.Valor.Detalles.Single().Cantidad);

        var actualizado = await prueba.Db.Productos
            .SingleAsync(p => p.Id == producto.Id);

        Assert.Equal(1.250m, actualizado.Stock);
        Assert.Equal(80m, actualizado.Costo);
    }

    [Fact]
    public async Task DevolucionPorPesoRegresaStockYReembolso()
    {
        await using var prueba = await PruebaPeso.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync(
            "KG-003", UnidadMedida.Kilogramo);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 2.500m, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = (await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 0.750m)],
            [new LineaPago(MetodoPago.Efectivo, 90m, 90m)])).Valor!;

        var detalle = venta.Detalles.Single();
        var devolucion = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(detalle.Id, 0.250m)]);

        Assert.True(devolucion.Correcta, devolucion.Mensaje);
        Assert.Equal(0.250m, detalle.CantidadDevuelta);
        Assert.Equal(30m, venta.TotalDevuelto);

        var actualizado = await prueba.Db.Productos
            .SingleAsync(p => p.Id == producto.Id);

        Assert.Equal(2.000m, actualizado.Stock);
    }

    [Fact]
    public async Task ImportacionAceptaUnidadKgYDecimales()
    {
        await using var prueba = await PruebaPeso.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var ruta = Path.Combine(
            Path.GetTempPath(), $"pos-peso-{Guid.NewGuid():N}.csv");

        try
        {
            await File.WriteAllTextAsync(
                ruta,
                "sku,nombre,unidad,precio_venta,costo,stock_minimo,stock_inicial\r\n" +
                "KG-004,Carne molida,kg,120,80,1.5,2.5\r\n",
                new UTF8Encoding(true));

            var lectura = await prueba.ImportacionService.LeerAsync(ruta);

            Assert.True(lectura.Correcta, lectura.Mensaje);
            Assert.Empty(lectura.Valor!.Errores);
            Assert.Equal(2.5m, lectura.Valor.UnidadesStock);

            var importacion = await prueba.ImportacionService.ImportarAsync(
                lectura.Valor);

            Assert.True(importacion.Correcta, importacion.Mensaje);

            var producto = await prueba.Db.Productos.SingleAsync();

            Assert.Equal(UnidadMedida.Kilogramo, producto.UnidadMedida);
            Assert.Equal(2.5m, producto.Stock);
            Assert.Equal(1.5m, producto.StockMinimo);

            var movimiento = await prueba.Db.MovimientosInventario
                .SingleAsync();

            Assert.Equal(2.5m, movimiento.Cantidad);
        }
        finally
        {
            File.Delete(ruta);
        }
    }

    [Fact]
    public async Task CajaPidePesoYCalculaSubtotal()
    {
        var producto = new Producto
        {
            Id = 1,
            Sku = "KG-005",
            Nombre = "Carne molida",
            UnidadMedida = UnidadMedida.Kilogramo,
            PrecioVenta = 120m,
            Stock = 10m
        };

        var vm = new CajaViewModel();
        vm.CantidadPesoPreguntada += (_, _) =>
            Task.FromResult<decimal?>(0.750m);

        await vm.AgregarProductoAsync(producto);

        var linea = Assert.Single(vm.LineasVentaItems);

        Assert.Equal(0.750m, linea.Cantidad);
        Assert.Equal(90m, linea.Subtotal);
        Assert.Equal("0.75 kg", linea.CantidadConUnidadTexto);
        Assert.Equal(90m, vm.SubtotalVenta);
    }

    [Fact]
    public async Task MigracionConvierteStockExistenteAMilesimas()
    {
        var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();

        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conexion)
                .Options);

        var migrator = db.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20261112191746_AddCostoDetalleVenta");

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Producto " +
            "(id, sku, nombre, descripcion, precioVenta, costo, stock, " +
            "stockMinimo, fechaArchivado, fechaCreacion, fechaModificacion) " +
            "VALUES (1, 'LEG-1', 'Producto anterior', NULL, '10.00', '5.00', " +
            "5, 2, NULL, '2026-01-01 00:00:00', '2026-01-01 00:00:00');");

        await db.Database.MigrateAsync();

        var producto = await db.Productos.SingleAsync();

        Assert.Equal(5m, producto.Stock);
        Assert.Equal(2m, producto.StockMinimo);
        Assert.Equal(UnidadMedida.Pieza, producto.UnidadMedida);

        await conexion.DisposeAsync();
    }

    [Fact]
    public async Task NoCambiaLaUnidadDeUnProductoConHistorial()
    {
        await using var prueba = await PruebaPeso.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync(
            "KG-006", UnidadMedida.Kilogramo);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 1m, "Carga inicial");

        var cambio = await prueba.ProductoService.ActualizarAsync(
            producto.Id, producto.Sku, producto.Nombre, null,
            120m, 80m, 0m, UnidadMedida.Pieza);

        Assert.False(cambio.Correcta);
        Assert.Contains("unidad", cambio.Mensaje);
    }
}
