using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

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

        public async Task CerrarCajaAsync(decimal montoFinal)
        {
            var resultado = await CajaService.CerrarAsync(montoFinal, null);
            Assert.True(resultado.Correcta, resultado.Mensaje);
        }

        public async Task<Usuario> CrearCajeroAsync(string nombreUsuario)
        {
            var resultado = await UsuarioService.AgregarAsync(
                nombreUsuario, $"Cajero {nombreUsuario}",
                "claveCajero123", RolUsuario.Cajero);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task<Venta> VenderAsync(
            Producto producto, MetodoPago metodo = MetodoPago.Efectivo,
            int cantidad = 1, decimal descuento = 0m)
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
    public async Task ObtenerHistorialFiltraPorRangoInclusive()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-007");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var hoy = await prueba.VenderAsync(producto);
        var vieja = await prueba.VenderAsync(producto);

        var viejaEnBase = await prueba.Db.Ventas.FirstAsync(v => v.Id == vieja.Id);
        viejaEnBase.Fecha = DateTime.Today.AddDays(-10).AddHours(10);
        await prueba.Db.SaveChangesAsync();

        var historial = await prueba.VentaService.ObtenerHistorialAsync(
            DateTime.Today.AddDays(-1), DateTime.Today);

        Assert.Single(historial);
        Assert.Equal(hoy.Id, historial[0].Id);
        Assert.NotNull(historial[0].Usuario);
        Assert.NotEmpty(historial[0].Detalles);
    }

    [Fact]
    public async Task VentasViewModelOcultaAnuladasPorDefecto()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-008");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var primera = await prueba.VenderAsync(producto);
        await prueba.VenderAsync(producto);

        var anulacion = await prueba.VentaService.AnularAsync(primera.Id);
        Assert.True(anulacion.Correcta, anulacion.Mensaje);

        var vm = new VentasViewModel(prueba.VentaService)
        {
            Desde = new DateTimeOffset(DateTime.Today.AddDays(-1)),
            Hasta = new DateTimeOffset(DateTime.Today)
        };

        await vm.CargarAsync();

        Assert.Single(vm.VentasItems);
        Assert.Equal("100.00", vm.TotalVendidoTexto);

        vm.VerAnuladas = true;

        Assert.Equal(2, vm.VentasItems.Count);
        Assert.Equal(2, vm.CantidadVentas);
        Assert.Equal("100.00", vm.TotalVendidoTexto);
        Assert.Contains(vm.VentasItems, v => v.Estado == EstadoVenta.Anulada);
    }

    [Fact]
    public async Task ResumenPagosSeparaMetodosYExcluyeAnuladas()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("VTA-009");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        await prueba.VenderAsync(producto, MetodoPago.Efectivo);
        await prueba.VenderAsync(producto, MetodoPago.Tarjeta);
        var transferencia = await prueba.VenderAsync(
            producto, MetodoPago.Transferencia);

        var anulacion = await prueba.VentaService.AnularAsync(transferencia.Id);
        Assert.True(anulacion.Correcta, anulacion.Mensaje);

        var caja = await prueba.CajaService.ObtenerCajaAbiertaAsync();
        var resumen = await prueba.CajaService.ObtenerResumenPagosAsync(caja!.Id);

        Assert.Equal(2, resumen.CantidadVentas);
        Assert.Equal(100m, resumen.Efectivo);
        Assert.Equal(100m, resumen.Tarjeta);
        Assert.Equal(0m, resumen.Transferencia);
        Assert.Equal(200m, resumen.Total);
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
            [new LineaVenta(producto.Id, 2)],
            [new LineaPago(MetodoPago.Efectivo, 180m, 200m)],
            20m);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Equal(200m, resultado.Valor!.Subtotal);
        Assert.Equal(20m, resultado.Valor.Descuento);
        Assert.Equal(180m, resultado.Valor.Total);
        Assert.Equal(20m, resultado.Valor.Vuelto);

        var movimiento = await prueba.Db.MovimientosCaja
            .SingleAsync(m => m.IdVenta == resultado.Valor.Id);

        Assert.Equal(180m, movimiento.Monto);
    }

    [Fact]
    public async Task DevolverPermiteVendedorOriginalYAdminPeroNoAOtroCajero()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        var admin = await prueba.CrearAdministradorEnSesionAsync();
        var cajero = await prueba.CrearCajeroAsync("laura");
        var otro = await prueba.CrearCajeroAsync("pedro");

        var producto = await prueba.CrearProductoAsync("DEV-001");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        prueba.Sesion.Iniciar(cajero);

        var venta = await prueba.VenderAsync(producto, cantidad: 2);
        var idDetalle = venta.Detalles.First().Id;

        prueba.Sesion.Iniciar(otro);

        var rechazada = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);
        Assert.False(rechazada.Correcta);

        prueba.Sesion.Iniciar(cajero);

        var parcial = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);
        Assert.True(parcial.Correcta, parcial.Mensaje);

        prueba.Sesion.Iniciar(admin);

        var restante = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);
        Assert.True(restante.Correcta, restante.Mensaje);

        var ventaFinal = await prueba.Db.Ventas
            .Include(v => v.Detalles)
            .FirstAsync(v => v.Id == venta.Id);

        Assert.Equal(EstadoVenta.Anulada, ventaFinal.Estado);
        Assert.Equal(2, ventaFinal.Detalles.First().CantidadDevuelta);
        Assert.Equal(admin.Id, ventaFinal.IdUsuarioAnulacion);
    }

    [Fact]
    public async Task DevolucionTotalDeCajaCerradaRegresaStockYUsaCajaAbierta()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("DEV-002");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VenderAsync(producto, cantidad: 2);
        var idDetalle = venta.Detalles.First().Id;

        await prueba.CerrarCajaAsync(1200m);
        await prueba.AbrirCajaAsync();

        var cajaAbierta = await prueba.Db.Cajas
            .FirstAsync(c => c.Estado == EstadoCaja.Abierta);

        var resultado = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 2)]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var ventaFinal = await prueba.Db.Ventas
            .Include(v => v.Detalles)
            .FirstAsync(v => v.Id == venta.Id);

        Assert.Equal(EstadoVenta.Anulada, ventaFinal.Estado);
        Assert.Equal(200m, ventaFinal.TotalDevuelto);
        Assert.Equal(2, ventaFinal.Detalles.First().CantidadDevuelta);
        Assert.NotNull(ventaFinal.FechaAnulacion);

        var productoFinal = await prueba.Db.Productos
            .FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(5, productoFinal.Stock);

        var movimientoInventario = await prueba.Db.MovimientosInventario
            .SingleAsync(m => m.IdVenta == venta.Id &&
                             m.Tipo == TipoMovimiento.AnulacionVenta);
        Assert.Equal($"Devolución venta V-{venta.Id:000000}",
            movimientoInventario.Referencia);

        var movimientoCaja = await prueba.Db.MovimientosCaja
            .SingleAsync(m => m.IdVenta == venta.Id &&
                              m.Tipo == TipoMovimientoCaja.AnulacionVenta);
        Assert.Equal(200m, movimientoCaja.Monto);
        Assert.Equal(cajaAbierta.Id, movimientoCaja.IdCaja);
        Assert.Equal($"Devolución venta V-{venta.Id:000000}",
            movimientoCaja.Descripcion);

        // La caja cerrada original queda intacta.
        var cajaOriginal = await prueba.Db.Cajas
            .AsNoTracking()
            .FirstAsync(c => c.Id == venta.IdCaja);
        Assert.Equal(EstadoCaja.Cerrada, cajaOriginal.Estado);
        Assert.Equal(1200m, cajaOriginal.MontoEsperado);
        Assert.Equal(1200m, cajaOriginal.MontoFinal);
        Assert.Equal(0m, cajaOriginal.Diferencia);

        var esperado = await prueba.CajaService
            .CalcularMontoEsperadoAsync(cajaAbierta.Id);
        Assert.Equal(800m, esperado);
    }

    [Fact]
    public async Task DevolucionParcialDejaPendienteYDescuentaCajaAbierta()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("DEV-003");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VenderAsync(producto, cantidad: 3);
        var idDetalle = venta.Detalles.First().Id;

        var parcial = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);

        Assert.True(parcial.Correcta, parcial.Mensaje);

        var ventaEnBase = await prueba.Db.Ventas
            .Include(v => v.Detalles)
            .FirstAsync(v => v.Id == venta.Id);

        Assert.Equal(EstadoVenta.Completada, ventaEnBase.Estado);
        Assert.Equal(100m, ventaEnBase.TotalDevuelto);
        Assert.Equal(1, ventaEnBase.Detalles.First().CantidadDevuelta);
        Assert.Equal(2, ventaEnBase.Detalles.First().CantidadPendiente);

        var productoEnBase = await prueba.Db.Productos
            .FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(3, productoEnBase.Stock);

        var movimiento = await prueba.Db.MovimientosCaja
            .SingleAsync(m => m.IdVenta == venta.Id &&
                              m.Tipo == TipoMovimientoCaja.AnulacionVenta);
        Assert.Equal(100m, movimiento.Monto);
        Assert.Equal(venta.IdCaja, movimiento.IdCaja);

        var esperado = await prueba.CajaService
            .CalcularMontoEsperadoAsync(venta.IdCaja);
        Assert.Equal(1200m, esperado);

        // Cancelar la venta después de una devolución parcial solo
        // devuelve lo que falta.
        var anulacion = await prueba.VentaService.AnularAsync(venta.Id);
        Assert.True(anulacion.Correcta, anulacion.Mensaje);

        var ventaFinal = await prueba.Db.Ventas
            .Include(v => v.Detalles)
            .FirstAsync(v => v.Id == venta.Id);

        Assert.Equal(EstadoVenta.Anulada, ventaFinal.Estado);
        Assert.Equal(300m, ventaFinal.TotalDevuelto);
        Assert.Equal(3, ventaFinal.Detalles.First().CantidadDevuelta);

        var productoFinal = await prueba.Db.Productos
            .FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(5, productoFinal.Stock);

        var esperadoFinal = await prueba.CajaService
            .CalcularMontoEsperadoAsync(venta.IdCaja);
        Assert.Equal(1000m, esperadoFinal);
    }

    [Fact]
    public async Task DevolucionProrrateaDescuentoYCobraSaldoExactoAlCompletarla()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("DEV-004");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VenderAsync(
            producto, cantidad: 3, descuento: 30m);
        var idDetalle = venta.Detalles.First().Id;

        Assert.Equal(270m, venta.Total);

        var parcial = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);

        Assert.True(parcial.Correcta, parcial.Mensaje);

        var ventaParcial = await prueba.Db.Ventas
            .FirstAsync(v => v.Id == venta.Id);
        Assert.Equal(90m, ventaParcial.TotalDevuelto);
        Assert.Equal(EstadoVenta.Completada, ventaParcial.Estado);

        var total = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 2)]);

        Assert.True(total.Correcta, total.Mensaje);

        var ventaFinal = await prueba.Db.Ventas
            .FirstAsync(v => v.Id == venta.Id);
        Assert.Equal(270m, ventaFinal.TotalDevuelto);
        Assert.Equal(EstadoVenta.Anulada, ventaFinal.Estado);

        var reembolsos = await prueba.Db.MovimientosCaja
            .Where(m => m.IdVenta == venta.Id &&
                        m.Tipo == TipoMovimientoCaja.AnulacionVenta)
            .ToListAsync();
        Assert.Equal(270m, reembolsos.Sum(m => m.Monto));
    }

    [Fact]
    public async Task SinCajaAbiertaBloqueaEfectivoPeroPermiteTarjeta()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("DEV-005");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var ventaEfectivo = await prueba.VenderAsync(producto);
        var ventaTarjeta = await prueba.VenderAsync(
            producto, MetodoPago.Tarjeta);

        await prueba.CerrarCajaAsync(1200m);

        var bloqueada = await prueba.VentaService.DevolverAsync(
            ventaEfectivo.Id,
            [new LineaDevolucion(ventaEfectivo.Detalles.First().Id, 1)]);
        Assert.False(bloqueada.Correcta);
        Assert.Contains("No hay una caja abierta", bloqueada.Mensaje);

        var tarjeta = await prueba.VentaService.DevolverAsync(
            ventaTarjeta.Id,
            [new LineaDevolucion(ventaTarjeta.Detalles.First().Id, 1)]);
        Assert.True(tarjeta.Correcta, tarjeta.Mensaje);

        var ventaFinal = await prueba.Db.Ventas
            .FirstAsync(v => v.Id == ventaTarjeta.Id);
        Assert.Equal(EstadoVenta.Anulada, ventaFinal.Estado);
        Assert.Equal(100m, ventaFinal.TotalDevuelto);

        var productoFinal = await prueba.Db.Productos
            .FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(4, productoFinal.Stock);

        var sinMovimiento = await prueba.Db.MovimientosCaja
            .AnyAsync(m => m.IdVenta == ventaTarjeta.Id &&
                           m.Tipo == TipoMovimientoCaja.AnulacionVenta);
        Assert.False(sinMovimiento);
    }

    [Fact]
    public async Task NoSePuedeDevolverMasDeLoVendidoNiDosVecesLoMismo()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("DEV-006");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VenderAsync(producto, cantidad: 2);
        var idDetalle = venta.Detalles.First().Id;

        var exceso = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 3)]);
        Assert.False(exceso.Correcta);

        var repetidos = await prueba.VentaService.DevolverAsync(
            venta.Id,
            [
                new LineaDevolucion(idDetalle, 1),
                new LineaDevolucion(idDetalle, 1)
            ]);
        Assert.False(repetidos.Correcta);

        var vacia = await prueba.VentaService.DevolverAsync(venta.Id, []);
        Assert.False(vacia.Correcta);

        var primera = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);
        Assert.True(primera.Correcta, primera.Mensaje);

        var segunda = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 2)]);
        Assert.False(segunda.Correcta);

        var final = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);
        Assert.True(final.Correcta, final.Mensaje);

        var doble = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(idDetalle, 1)]);
        Assert.False(doble.Correcta);
        Assert.Equal("La venta ya está anulada.", doble.Mensaje);
    }

    [Fact]
    public async Task ResumenPagosYTotalVendidoSonNetosDeDevoluciones()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("DEV-007");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var efectivo = await prueba.VenderAsync(producto, cantidad: 3);
        await prueba.VenderAsync(producto, MetodoPago.Tarjeta);

        var parcial = await prueba.VentaService.DevolverAsync(
            efectivo.Id, [new LineaDevolucion(efectivo.Detalles.First().Id, 1)]);
        Assert.True(parcial.Correcta, parcial.Mensaje);

        var caja = await prueba.CajaService.ObtenerCajaAbiertaAsync();
        var resumen = await prueba.CajaService.ObtenerResumenPagosAsync(caja!.Id);

        Assert.Equal(2, resumen.CantidadVentas);
        Assert.Equal(200m, resumen.Efectivo);
        Assert.Equal(100m, resumen.Tarjeta);
        Assert.Equal(300m, resumen.Total);

        var vm = new VentasViewModel(prueba.VentaService)
        {
            Desde = new DateTimeOffset(DateTime.Today.AddDays(-1)),
            Hasta = new DateTimeOffset(DateTime.Today)
        };

        await vm.CargarAsync();

        Assert.Equal("300.00", vm.TotalVendidoTexto);

        var ventaEfectivo = vm.VentasItems.Single(v => v.Id == efectivo.Id);
        Assert.Equal(200m, ventaEfectivo.TotalNeto);
        Assert.True(ventaEfectivo.HayDevolucion);
    }

    [Fact]
    public async Task CancelarVentaAbreElChecklistConLasUnidadesPendientes()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        var admin = await prueba.CrearAdministradorEnSesionAsync();
        var cajero = await prueba.CrearCajeroAsync("laura");
        var otro = await prueba.CrearCajeroAsync("pedro");

        var producto = await prueba.CrearProductoAsync("DEV-009");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 10, "Carga inicial");
        await prueba.AbrirCajaAsync();

        prueba.Sesion.Iniciar(cajero);

        var venta = await prueba.VenderAsync(producto, cantidad: 4);
        var anulada = await prueba.VenderAsync(producto, cantidad: 1);

        prueba.Sesion.Iniciar(admin);

        await prueba.VentaService.DevolverAsync(
            anulada.Id, [new LineaDevolucion(anulada.Detalles.First().Id, 1)]);

        var caja = await prueba.CajaService.ObtenerCajaAbiertaAsync();
        var vm = new CancelarVentaViewModel(
            prueba.VentaService, prueba.Sesion, caja!.Id);

        await vm.CargarAsync();

        vm.SelectedVenta = vm.VentasItems.Single(v => v.Id == anulada.Id);
        Assert.False(vm.PuedeDevolver);

        vm.SelectedVenta = vm.VentasItems.Single(v => v.Id == venta.Id);
        Assert.True(vm.PuedeDevolver);

        var checklist = vm.CrearDevolucionViewModel();
        Assert.NotNull(checklist);

        var item = Assert.Single(checklist!.Items);
        Assert.True(item.EsGrupo);
        Assert.Equal("Producto DEV-009 - x4", item.TituloGrupo);
        Assert.Equal(4, item.Unidades.Count);
        Assert.All(item.Unidades, u => Assert.False(u.Seleccionada));

        prueba.Sesion.Iniciar(otro);
        Assert.False(vm.PuedeDevolver);
    }

    [Fact]
    public async Task RegistrarPagoMixtoGuardaPagosYMovimientoSoloDeEfectivo()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-001");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [
                new LineaPago(MetodoPago.Efectivo, 40m, 50m),
                new LineaPago(MetodoPago.Tarjeta, 60m)
            ]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var venta = resultado.Valor!;

        Assert.Equal(MetodoPago.Mixto, venta.MetodoPago);
        Assert.Equal(2, venta.Pagos.Count);
        Assert.Equal(
            40m,
            venta.Pagos.Single(p => p.Metodo == MetodoPago.Efectivo).Monto);
        Assert.Equal(
            60m,
            venta.Pagos.Single(p => p.Metodo == MetodoPago.Tarjeta).Monto);
        Assert.Equal(10m, venta.Vuelto);
        Assert.Equal(110m, venta.MontoRecibido);

        var movimientos = await prueba.Db.MovimientosCaja
            .Where(m => m.IdVenta == venta.Id)
            .ToListAsync();

        var movimiento = Assert.Single(movimientos);
        Assert.Equal(40m, movimiento.Monto);
    }

    [Fact]
    public async Task RegistrarPagoMixtoValidaSumaMetodosYRecibido()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-002");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var sumaIncorrecta = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [
                new LineaPago(MetodoPago.Efectivo, 40m, 40m),
                new LineaPago(MetodoPago.Tarjeta, 50m)
            ]);

        Assert.False(sumaIncorrecta.Correcta);
        Assert.Contains("suma de los pagos", sumaIncorrecta.Mensaje);

        var repetido = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [
                new LineaPago(MetodoPago.Efectivo, 50m, 50m),
                new LineaPago(MetodoPago.Efectivo, 50m, 50m)
            ]);

        Assert.False(repetido.Correcta);
        Assert.Contains("repetir", repetido.Mensaje);

        var recibidoInsuficiente = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [new LineaPago(MetodoPago.Efectivo, 100m, 90m)]);

        Assert.False(recibidoInsuficiente.Correcta);
        Assert.Contains("efectivo recibido", recibidoInsuficiente.Mensaje);
    }

    [Fact]
    public async Task ResumenPagosSeparaUnaVentaMixta()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-003");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [
                new LineaPago(MetodoPago.Efectivo, 40m, 40m),
                new LineaPago(MetodoPago.Tarjeta, 60m)
            ]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var caja = await prueba.CajaService.ObtenerCajaAbiertaAsync();
        var resumen = await prueba.CajaService.ObtenerResumenPagosAsync(caja!.Id);

        Assert.Equal(1, resumen.CantidadVentas);
        Assert.Equal(40m, resumen.Efectivo);
        Assert.Equal(60m, resumen.Tarjeta);
        Assert.Equal(0m, resumen.Transferencia);
        Assert.Equal(100m, resumen.Total);
    }

    [Fact]
    public async Task DevolucionParcialDeVentaMixtaReparteElReembolso()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-004");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 2)],
            [
                new LineaPago(MetodoPago.Efectivo, 80m, 80m),
                new LineaPago(MetodoPago.Tarjeta, 120m)
            ]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var venta = resultado.Valor!;
        var linea = venta.Detalles.First();

        var devolucion = await prueba.VentaService.DevolverAsync(
            venta.Id, [new LineaDevolucion(linea.Id, 1)]);

        Assert.True(devolucion.Correcta, devolucion.Mensaje);

        var ventaDb = await prueba.Db.Ventas
            .Include(v => v.Pagos)
            .SingleAsync(v => v.Id == venta.Id);

        Assert.Equal(EstadoVenta.Completada, ventaDb.Estado);
        Assert.Equal(100m, ventaDb.TotalDevuelto);
        Assert.Equal(
            40m,
            ventaDb.Pagos.Single(p => p.Metodo == MetodoPago.Efectivo).MontoDevuelto);
        Assert.Equal(
            60m,
            ventaDb.Pagos.Single(p => p.Metodo == MetodoPago.Tarjeta).MontoDevuelto);

        var reembolso = await prueba.Db.MovimientosCaja
            .SingleAsync(m => m.IdVenta == venta.Id &&
                              m.Tipo == TipoMovimientoCaja.AnulacionVenta);

        Assert.Equal(40m, reembolso.Monto);
    }

    [Fact]
    public async Task DevolucionTotalDeVentaMixtaUsaElSaldoExacto()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-005");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [
                new LineaPago(MetodoPago.Efectivo, 40m, 40m),
                new LineaPago(MetodoPago.Tarjeta, 60m)
            ]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var venta = resultado.Valor!;

        var devolucion = await prueba.VentaService.DevolverAsync(
            venta.Id, lineas: null);

        Assert.True(devolucion.Correcta, devolucion.Mensaje);

        var ventaDb = await prueba.Db.Ventas
            .Include(v => v.Pagos)
            .SingleAsync(v => v.Id == venta.Id);

        Assert.Equal(EstadoVenta.Anulada, ventaDb.Estado);
        Assert.Equal(100m, ventaDb.TotalDevuelto);
        Assert.Equal(
            40m,
            ventaDb.Pagos.Single(p => p.Metodo == MetodoPago.Efectivo).MontoDevuelto);
        Assert.Equal(
            60m,
            ventaDb.Pagos.Single(p => p.Metodo == MetodoPago.Tarjeta).MontoDevuelto);

        var reembolso = await prueba.Db.MovimientosCaja
            .SingleAsync(m => m.IdVenta == venta.Id &&
                              m.Tipo == TipoMovimientoCaja.AnulacionVenta);

        Assert.Equal(40m, reembolso.Monto);
    }

    [Fact]
    public async Task DevolucionMixtaSinCajaAbiertaBloqueaElEfectivo()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-006");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [
                new LineaPago(MetodoPago.Efectivo, 40m, 40m),
                new LineaPago(MetodoPago.Tarjeta, 60m)
            ]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var stockAntes = producto.Stock;
        await prueba.CerrarCajaAsync(1000m);

        var devolucion = await prueba.VentaService.DevolverAsync(
            resultado.Valor!.Id, lineas: null);

        Assert.False(devolucion.Correcta);
        Assert.Contains("caja abierta", devolucion.Mensaje);
        Assert.Equal(stockAntes, producto.Stock);
    }

    [Fact]
    public async Task DevolucionSoloTarjetaFuncionaSinCajaAbierta()
    {
        await using var prueba = await PruebaVentas.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("MIX-007");
        await prueba.InventarioService.RegistrarEntradaAsync(
            producto.Id, 5, "Carga inicial");
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [new LineaPago(MetodoPago.Tarjeta, 100m)]);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        await prueba.CerrarCajaAsync(1000m);

        var devolucion = await prueba.VentaService.DevolverAsync(
            resultado.Valor!.Id, lineas: null);

        Assert.True(devolucion.Correcta, devolucion.Mensaje);

        var ventaDb = await prueba.Db.Ventas
            .Include(v => v.Pagos)
            .SingleAsync(v => v.Id == resultado.Valor.Id);

        Assert.Equal(100m, ventaDb.TotalDevuelto);
        var reembolsos = await prueba.Db.MovimientosCaja
            .Where(m => m.IdVenta == ventaDb.Id &&
                        m.Tipo == TipoMovimientoCaja.AnulacionVenta)
            .ToListAsync();

        Assert.Empty(reembolsos);
    }
}
