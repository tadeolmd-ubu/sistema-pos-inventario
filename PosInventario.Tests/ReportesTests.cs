using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class ReportesTests
{
    private sealed class PruebaReportes : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public InventarioService InventarioService { get; }
        public CajaService CajaService { get; }
        public VentaService VentaService { get; }
        public ReporteService ReporteService { get; }

        private PruebaReportes(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
            CajaService = new CajaService(db, Sesion);
            VentaService = new VentaService(db, Sesion);
            ReporteService = new ReporteService(db, Sesion);
        }

        public static async Task<PruebaReportes> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaReportes(conexion, db);
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

        public async Task<Producto> CrearProductoAsync(
            string sku, decimal precio = 100m, decimal costo = 50m)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, $"Producto {sku}", null, precio, costo, 0);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task AbrirCajaAsync()
        {
            var resultado = await CajaService.AbrirAsync(1000m);
            Assert.True(resultado.Correcta, resultado.Mensaje);
        }

        public async Task<Venta> VenderAsync(
            Producto producto,
            MetodoPago metodo = MetodoPago.Efectivo,
            int cantidad = 1,
            decimal descuento = 0m)
        {
            var total = producto.PrecioVenta * cantidad - descuento;

            var resultado = await VentaService.RegistrarAsync(
                [new LineaVenta(producto.Id, cantidad)],
                [
                    new LineaPago(
                        metodo,
                        total,
                        metodo == MetodoPago.Efectivo ? total : null)
                ],
                descuento);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task CambiarFechaAsync(Venta venta, DateTime fecha)
        {
            venta.Fecha = fecha;
            await Db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task SoloElAdministradorPuedeConsultarReportes()
    {
        await using var prueba = await PruebaReportes.CrearAsync();

        var resultado = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia);

        Assert.False(resultado.Correcta);
        Assert.Null(resultado.Valor);
    }

    [Fact]
    public async Task CalculaVentasMetodosUtilidadYProductos()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("REP-001", 100m, 40m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 20, "Carga inicial");
        await prueba.AbrirCajaAsync();

        await prueba.VenderAsync(producto, MetodoPago.Efectivo, 2);
        await prueba.VenderAsync(producto, MetodoPago.Tarjeta, 1);
        await prueba.VenderAsync(producto, MetodoPago.Transferencia, 2, 40m);

        var resultado = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        var reporte = resultado.Valor!;

        Assert.Equal(3, reporte.CantidadVentas);
        Assert.Equal(460m, reporte.VentasBrutas);
        Assert.Equal(0m, reporte.Devoluciones);
        Assert.Equal(460m, reporte.VentasNetas);
        Assert.Equal(200m, reporte.Costo);
        Assert.Equal(260m, reporte.Utilidad);
        Assert.Equal(260m / 460m, reporte.Margen);
        Assert.Equal(460m / 3m, reporte.TicketPromedio);

        Assert.Collection(
            reporte.Metodos,
            efectivo =>
            {
                Assert.Equal(MetodoPago.Efectivo, efectivo.Metodo);
                Assert.Equal(200m, efectivo.Monto);
                Assert.Equal(1, efectivo.CantidadVentas);
            },
            tarjeta =>
            {
                Assert.Equal(MetodoPago.Tarjeta, tarjeta.Metodo);
                Assert.Equal(100m, tarjeta.Monto);
                Assert.Equal(1, tarjeta.CantidadVentas);
            },
            transferencia =>
            {
                Assert.Equal(MetodoPago.Transferencia, transferencia.Metodo);
                Assert.Equal(160m, transferencia.Monto);
                Assert.Equal(1, transferencia.CantidadVentas);
            });

        Assert.Equal(
            100m,
            reporte.Metodos.Sum(m => m.Porcentaje));

        var vendido = Assert.Single(reporte.Productos);
        Assert.Equal(producto.Id, vendido.IdProducto);
        Assert.Equal(5, vendido.Cantidad);
        Assert.Equal(460m, vendido.Ventas);
        Assert.Equal(200m, vendido.Costo);
        Assert.Equal(260m, vendido.Utilidad);
        Assert.Equal(1m, vendido.Proporcion);

        var periodo = Assert.Single(reporte.Periodos);
        Assert.Equal(3, periodo.CantidadVentas);
        Assert.Equal(460m, periodo.VentasNetas);
        Assert.Equal(260m, periodo.Utilidad);
        Assert.Equal(1m, periodo.Proporcion);
    }

    [Fact]
    public async Task DescuentaLasDevolucionesParcialesDelProductoYLaUtilidad()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("REP-002", 100m, 60m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VenderAsync(producto, MetodoPago.Efectivo, 3);
        var detalle = venta.Detalles.First();

        var devolucion = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(detalle.Id, 1)]);
        Assert.True(devolucion.Correcta, devolucion.Mensaje);

        var resultado = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        var reporte = resultado.Valor!;

        Assert.Equal(1, reporte.CantidadVentas);
        Assert.Equal(300m, reporte.VentasBrutas);
        Assert.Equal(100m, reporte.Devoluciones);
        Assert.Equal(200m, reporte.VentasNetas);
        Assert.Equal(120m, reporte.Costo);
        Assert.Equal(80m, reporte.Utilidad);

        var vendido = Assert.Single(reporte.Productos);
        Assert.Equal(2, vendido.Cantidad);
        Assert.Equal(200m, vendido.Ventas);
        Assert.Equal(120m, vendido.Costo);
        Assert.Equal(80m, vendido.Utilidad);

        var efectivo = Assert.Single(
            reporte.Metodos, m => m.Metodo == MetodoPago.Efectivo);
        Assert.Equal(200m, efectivo.Monto);
        Assert.Equal(1, efectivo.CantidadVentas);
    }

    [Fact]
    public async Task ExcluyeLasVentasTotalmenteAnuladas()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("REP-003", 100m, 50m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VenderAsync(producto, MetodoPago.Efectivo, 2);

        var anulacion = await prueba.VentaService.AnularAsync(venta.Id);
        Assert.True(anulacion.Correcta, anulacion.Mensaje);

        var resultado = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        var reporte = resultado.Valor!;

        Assert.Equal(0, reporte.CantidadVentas);
        Assert.Equal(0m, reporte.VentasBrutas);
        Assert.Equal(0m, reporte.VentasNetas);
        Assert.Empty(reporte.Periodos);
        Assert.Empty(reporte.Productos);
    }

    [Fact]
    public async Task UsaElCostoGuardadoAlMomentoDeLaVenta()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("REP-004", 100m, 50m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        await prueba.VenderAsync(producto, MetodoPago.Efectivo, 1);

        // El costo del catálogo cambia después de la venta.
        var actualizacion = await prueba.ProductoService.ActualizarAsync(
            producto.Id, producto.Sku, producto.Nombre, null, 100m, 80m, 0);
        Assert.True(actualizacion.Correcta, actualizacion.Mensaje);

        var resultado = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        var reporte = resultado.Valor!;

        Assert.Equal(50m, reporte.Costo);
        Assert.Equal(50m, reporte.Utilidad);
    }

    [Fact]
    public async Task OrdenaLosProductosPorUnidadesNetasYRespetaElTop()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var primero = await prueba.CrearProductoAsync("REP-005", 100m, 50m);
        var segundo = await prueba.CrearProductoAsync("REP-006", 50m, 20m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            primero.Id, 10, "Carga inicial");
        await prueba.InventarioService.RegistrarEntradaAsync(
            segundo.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        await prueba.VenderAsync(primero, MetodoPago.Efectivo, 2);
        await prueba.VenderAsync(segundo, MetodoPago.Efectivo, 4);

        var resultado = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia, topProductos: 1);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        var reporte = resultado.Valor!;

        var vendido = Assert.Single(reporte.Productos);
        Assert.Equal(segundo.Id, vendido.IdProducto);
        Assert.Equal(4, vendido.Cantidad);
        Assert.Equal(200m, vendido.Ventas);
        Assert.Equal(80m, vendido.Costo);
        Assert.Equal(120m, vendido.Utilidad);
    }

    [Fact]
    public async Task AgrupaPorDiaSemanaYMes()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("REP-007", 100m, 50m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var lunesManana = await prueba.VenderAsync(producto);
        var lunesTarde = await prueba.VenderAsync(producto);
        var martes = await prueba.VenderAsync(producto);
        var noviembre = await prueba.VenderAsync(producto);

        await prueba.CambiarFechaAsync(
            lunesManana, new DateTime(2026, 10, 5, 9, 0, 0));
        await prueba.CambiarFechaAsync(
            lunesTarde, new DateTime(2026, 10, 5, 18, 0, 0));
        await prueba.CambiarFechaAsync(
            martes, new DateTime(2026, 10, 6, 12, 0, 0));
        await prueba.CambiarFechaAsync(
            noviembre, new DateTime(2026, 11, 2, 12, 0, 0));

        var desde = new DateTime(2026, 10, 1);
        var hasta = new DateTime(2026, 11, 30);

        var porDia = (await prueba.ReporteService.ObtenerAsync(
            desde, hasta, AgrupacionReporte.Dia)).Valor!;

        Assert.Collection(
            porDia.Periodos,
            dia =>
            {
                Assert.Equal("05/10/2026", dia.Etiqueta);
                Assert.Equal(2, dia.CantidadVentas);
                Assert.Equal(200m, dia.VentasNetas);
            },
            dia =>
            {
                Assert.Equal("06/10/2026", dia.Etiqueta);
                Assert.Equal(1, dia.CantidadVentas);
            },
            dia =>
            {
                Assert.Equal("02/11/2026", dia.Etiqueta);
                Assert.Equal(1, dia.CantidadVentas);
            });

        var porSemana = (await prueba.ReporteService.ObtenerAsync(
            desde, hasta, AgrupacionReporte.Semana)).Valor!;

        Assert.Collection(
            porSemana.Periodos,
            semana =>
            {
                Assert.Equal("05/10 – 11/10/2026", semana.Etiqueta);
                Assert.Equal(3, semana.CantidadVentas);
                Assert.Equal(300m, semana.VentasNetas);
            },
            semana =>
            {
                Assert.Equal("02/11 – 08/11/2026", semana.Etiqueta);
                Assert.Equal(1, semana.CantidadVentas);
            });

        var porMes = (await prueba.ReporteService.ObtenerAsync(
            desde, hasta, AgrupacionReporte.Mes)).Valor!;

        Assert.Collection(
            porMes.Periodos,
            mes =>
            {
                Assert.Equal("Octubre 2026", mes.Etiqueta);
                Assert.Equal(3, mes.CantidadVentas);
                Assert.Equal(300m, mes.VentasNetas);
            },
            mes =>
            {
                Assert.Equal("Noviembre 2026", mes.Etiqueta);
                Assert.Equal(1, mes.CantidadVentas);
            });
    }

    [Fact]
    public async Task ElViewModelCargaElReporteYExponeLosTotales()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("REP-008", 100m, 50m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();
        await prueba.VenderAsync(producto, MetodoPago.Efectivo, 2);

        var vm = new ReportesViewModel(prueba.ReporteService)
        {
            Desde = new DateTimeOffset(DateTime.Today),
            Hasta = new DateTimeOffset(DateTime.Today)
        };

        await vm.CargarAsync();

        Assert.True(vm.HayDatos);
        Assert.Equal(1, vm.CantidadVentas);
        Assert.Equal("200.00", vm.VentasNetasTexto);
        Assert.Equal("100.00", vm.UtilidadTexto);
        Assert.Equal("50.0%", vm.MargenTexto);
        Assert.Single(vm.Periodos);
        Assert.Equal(3, vm.Metodos.Count);
        Assert.Single(vm.Productos);
        Assert.StartsWith("Reporte actualizado", vm.MensajeOperacion);
    }

    [Fact]
    public async Task SinServicioDeReportesAvisaEnLugarDeQuedarseCallado()
    {
        var vm = new ReportesViewModel();

        await vm.CargarAsync();

        Assert.Contains("no está disponible", vm.MensajeOperacion);
    }

    [Fact]
    public async Task LosRangosRapidosAjustanLasFechas()
    {
        var vm = new ReportesViewModel();

        await vm.VerHoyCommand.ExecuteAsync(null);

        Assert.Equal(DateTime.Today, vm.Desde?.Date);
        Assert.Equal(DateTime.Today, vm.Hasta?.Date);

        await vm.VerMesAnteriorCommand.ExecuteAsync(null);

        var primerDiaDelMes = new DateTime(
            DateTime.Today.Year, DateTime.Today.Month, 1);

        Assert.Equal(primerDiaDelMes.AddMonths(-1), vm.Desde?.Date);
        Assert.Equal(primerDiaDelMes.AddDays(-1), vm.Hasta?.Date);
    }

    [Fact]
    public async Task UnRangoInvalidoMuestraUnMensajeClaro()
    {
        var vm = new ReportesViewModel
        {
            Desde = new DateTimeOffset(DateTime.Today),
            Hasta = new DateTimeOffset(DateTime.Today.AddDays(-1))
        };

        await vm.CargarAsync();

        Assert.Contains("no puede ser anterior", vm.MensajeOperacion);
        Assert.False(vm.HayDatos);
    }

    [Fact]
    public async Task ElViewModelPermiteExportarElReporteGenerado()
    {
        await using var prueba = await PruebaReportes.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("REP-009", 100m, 50m);
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();
        await prueba.VenderAsync(producto, MetodoPago.Efectivo, 2);

        var vm = new ReportesViewModel(prueba.ReporteService)
        {
            Desde = new DateTimeOffset(DateTime.Today),
            Hasta = new DateTimeOffset(DateTime.Today)
        };

        Assert.False(vm.PuedeExportar);

        await vm.CargarAsync();

        Assert.True(vm.PuedeExportar);
        Assert.EndsWith(".xlsx", vm.NombreArchivoExportacion);
        Assert.NotEmpty(vm.GenerarExcel());
    }
}
