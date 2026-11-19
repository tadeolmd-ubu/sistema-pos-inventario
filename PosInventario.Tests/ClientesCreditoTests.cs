using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class ClientesCreditoTests
{
    private sealed class PruebaCredito : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public InventarioService InventarioService { get; }
        public CajaService CajaService { get; }
        public ClienteService ClienteService { get; }
        public VentaService VentaService { get; }
        public ReporteService ReporteService { get; }

        private PruebaCredito(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
            CajaService = new CajaService(db, Sesion);
            ClienteService = new ClienteService(db, Sesion);
            VentaService = new VentaService(db, Sesion, ClienteService);
            ReporteService = new ReporteService(db, Sesion);
        }

        public static async Task<PruebaCredito> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaCredito(conexion, db);
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
            string sku = "CRE-001", decimal precio = 100m)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, $"Producto {sku}", null, precio, 50m, 0);

            Assert.True(resultado.Correcta, resultado.Mensaje);

            var producto = resultado.Valor!;

            var entrada = await InventarioService.RegistrarEntradaAsync(
                producto.Id, 10m, "Carga inicial");

            Assert.True(entrada.Correcta, entrada.Mensaje);
            return producto;
        }

        public async Task<Cliente> CrearClienteAsync(
            string nombre = "Cliente de prueba", decimal limite = 1000m)
        {
            var resultado = await ClienteService.AgregarAsync(
                nombre, "555-123-4567", null, null, limite);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task AbrirCajaAsync()
        {
            var resultado = await CajaService.AbrirAsync(1000m);
            Assert.True(resultado.Correcta, resultado.Mensaje);
        }

        public async Task<Venta> VentaCreditoAsync(
            Producto producto, Cliente cliente, int cantidad = 1)
        {
            var resultado = await VentaService.RegistrarAsync(
                [new LineaVenta(producto.Id, cantidad)],
                [],
                0m,
                cliente.Id,
                TipoVenta.Credito);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task VentaCreditoQuedaEnLaCuentaDelClienteYNoEnCaja()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        var cliente = await prueba.CrearClienteAsync(limite: 500m);
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VentaCreditoAsync(producto, cliente);

        Assert.True(venta.EsCredito);
        Assert.Equal(MetodoPago.Credito, venta.MetodoPago);
        Assert.Empty(venta.Pagos);
        Assert.Equal(100m, venta.MontoRecibido);
        Assert.Equal(0m, venta.Vuelto);
        Assert.Equal(100m, await prueba.ClienteService.ObtenerSaldoAsync(cliente.Id));

        Assert.Empty(await prueba.Db.MovimientosCaja.ToListAsync());

        var caja = await prueba.Db.Cajas.SingleAsync();
        var resumen = await prueba.CajaService.ObtenerResumenPagosAsync(caja.Id);

        Assert.Equal(100m, resumen.Credito);
        Assert.Equal(100m, resumen.Total);

        var reporte = await prueba.ReporteService.ObtenerAsync(
            DateTime.Today, DateTime.Today, AgrupacionReporte.Dia);

        var metodoCredito = Assert.Single(
            reporte.Valor!.Metodos,
            m => m.Metodo == MetodoPago.Credito);

        Assert.Equal(100m, metodoCredito.Monto);
        Assert.Equal(1, metodoCredito.CantidadVentas);
    }

    [Fact]
    public async Task VentaCreditoSinClienteSeRechaza()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [],
            0m,
            idCliente: null,
            TipoVenta.Credito);

        Assert.False(resultado.Correcta);
        Assert.Contains("cliente", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VentaCreditoNoPuedeSuperarElLimite()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync(precio: 100m);
        var cliente = await prueba.CrearClienteAsync(limite: 50m);
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [],
            0m,
            cliente.Id,
            TipoVenta.Credito);

        Assert.False(resultado.Correcta);
        Assert.Contains("crédito", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VentaContadoConClienteNoGeneraSaldo()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        var cliente = await prueba.CrearClienteAsync();
        await prueba.AbrirCajaAsync();

        var resultado = await prueba.VentaService.RegistrarAsync(
            [new LineaVenta(producto.Id, 1)],
            [new LineaPago(MetodoPago.Efectivo, 100m, 100m)],
            0m,
            cliente.Id);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.False(resultado.Valor!.EsCredito);
        Assert.Equal(cliente.Id, resultado.Valor.IdCliente);
        Assert.Equal(0m, await prueba.ClienteService.ObtenerSaldoAsync(cliente.Id));
    }

    [Fact]
    public async Task AbonoReduceElSaldoYEntraALaCaja()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        var cliente = await prueba.CrearClienteAsync();
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VentaCreditoAsync(producto, cliente);
        var abono = await prueba.ClienteService.RegistrarAbonoAsync(
            venta.Id, 40m, MetodoPago.Efectivo, "Abono inicial");

        Assert.True(abono.Correcta, abono.Mensaje);
        Assert.Equal(60m, await prueba.ClienteService.ObtenerSaldoAsync(cliente.Id));

        var caja = await prueba.Db.Cajas.SingleAsync();
        var movimiento = await prueba.Db.MovimientosCaja.SingleAsync();

        Assert.Equal(TipoMovimientoCaja.Ingreso, movimiento.Tipo);
        Assert.Equal(40m, movimiento.Monto);
        Assert.Equal(1040m, await prueba.CajaService.CalcularMontoEsperadoAsync(caja.Id));
    }

    [Fact]
    public async Task AbonoNoPuedeSuperarElPendiente()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        var cliente = await prueba.CrearClienteAsync();
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VentaCreditoAsync(producto, cliente);

        var abono = await prueba.ClienteService.RegistrarAbonoAsync(
            venta.Id, 150m, MetodoPago.Tarjeta, null);

        Assert.False(abono.Correcta);
        Assert.Contains("superar", abono.Mensaje);
        Assert.Equal(100m, await prueba.ClienteService.ObtenerSaldoAsync(cliente.Id));
    }

    [Fact]
    public async Task DevolucionDeVentaCreditoReduceElSaldoSinMoverLaCaja()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        var cliente = await prueba.CrearClienteAsync();
        await prueba.AbrirCajaAsync();

        var venta = await prueba.VentaCreditoAsync(producto, cliente);
        var devolucion = await prueba.VentaService.DevolverAsync(venta.Id, null);

        Assert.True(devolucion.Correcta, devolucion.Mensaje);
        Assert.Equal(0m, await prueba.ClienteService.ObtenerSaldoAsync(cliente.Id));
        Assert.Empty(await prueba.Db.MovimientosCaja.ToListAsync());
    }

    [Fact]
    public async Task ClientesViewModelCargaElEstadoDeCuenta()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        var cliente = await prueba.CrearClienteAsync();
        await prueba.AbrirCajaAsync();

        await prueba.VentaCreditoAsync(producto, cliente);

        var vm = new ClientesViewModel(prueba.ClienteService);
        await vm.CargarAsync();

        vm.SelectedCliente = Assert.Single(vm.ClientesItems);
        await vm.RecargarAsync();

        Assert.Equal("100.00", vm.SaldoActualTexto);
        Assert.Equal("900.00", vm.CreditoDisponibleTexto);
        Assert.Single(vm.VentasPendientes);
        Assert.True(vm.PuedeAbonar);
    }

    [Fact]
    public async Task NoDesactivaClienteConSaldoPendiente()
    {
        await using var prueba = await PruebaCredito.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();
        var cliente = await prueba.CrearClienteAsync();
        await prueba.AbrirCajaAsync();

        await prueba.VentaCreditoAsync(producto, cliente);

        var desactivar = await prueba.ClienteService.DesactivarAsync(cliente.Id);

        Assert.False(desactivar.Correcta);
        Assert.Contains("saldo", desactivar.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CajaExigeClienteParaCredito()
    {
        var producto = new Producto
        {
            Id = 1,
            Sku = "CRE-UI",
            Nombre = "Producto",
            PrecioVenta = 100m,
            Stock = 10m
        };

        var vm = new CajaViewModel();

        Assert.True(vm.EsPublico);
        Assert.False(vm.EsPagoCredito);

        vm.MetodoPagoSeleccionado = MetodoPago.Credito;
        Assert.True(vm.EsPagoCredito);
        Assert.False(vm.PagosValidos);

        vm.ClienteSeleccionado = new OpcionCliente(1, "Cliente", 500m, 0m);
        Assert.True(vm.PagosValidos);
        Assert.False(vm.EsPublico);
    }
}
