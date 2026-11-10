using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class CompraServiceTests
{
    private sealed class PruebaCompras : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public ProveedorService ProveedorService { get; }
        public InventarioService InventarioService { get; }
        public CompraService CompraService { get; }

        private PruebaCompras(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            ProveedorService = new ProveedorService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
            CompraService = new CompraService(db, Sesion);
        }

        public static async Task<PruebaCompras> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaCompras(conexion, db);
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
            string sku,
            decimal costo = 50m,
            decimal precioVenta = 100m)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, $"Producto {sku}", null, precioVenta, costo, 0);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task<Proveedor> CrearProveedorAsync(
            string nombre = "Distribuidora Central",
            string telefono = "11 5555-1000")
        {
            var resultado = await ProveedorService.AgregarAsync(
                nombre, telefono, null, null);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async Task<Compra> RegistrarCompraAsync(
            Proveedor proveedor, params LineaCompra[] lineas)
        {
            var resultado = await CompraService.RegistrarAsync(
                proveedor.Id, null, lineas);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task RegistrarCreaCompraConDetallesYMovimientos()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var productoA = await prueba.CrearProductoAsync("ABC-001");
        var productoB = await prueba.CrearProductoAsync("ABC-002");

        var compra = await prueba.RegistrarCompraAsync(
            proveedor,
            new LineaCompra(productoA.Id, 3, 50m),
            new LineaCompra(productoB.Id, 2, 80m));

        Assert.Equal(2, compra.Detalles.Count);
        Assert.Equal(3 * 50m + 2 * 80m, compra.Total);
        Assert.Equal(EstadoCompra.Completada, compra.Estado);

        var movimientos = await prueba.Db.MovimientosInventario
            .Where(m => m.IdCompra == compra.Id)
            .ToListAsync();

        Assert.Equal(2, movimientos.Count);
        Assert.All(movimientos, m => Assert.Equal(TipoMovimiento.Compra, m.Tipo));
    }

    [Fact]
    public async Task RegistrarAumentaStockYActualizaCosto()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001", costo: 50m);

        await prueba.RegistrarCompraAsync(
            proveedor, new LineaCompra(producto.Id, 10, 75m));

        var actualizado = await prueba.Db.Productos
            .AsNoTracking()
            .FirstAsync(p => p.Id == producto.Id);

        Assert.Equal(10, actualizado.Stock);
        Assert.Equal(75m, actualizado.Costo);
    }

    [Fact]
    public async Task TotalEsLaSumaDeLasLineas()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var productoA = await prueba.CrearProductoAsync("ABC-001");
        var productoB = await prueba.CrearProductoAsync("ABC-002");

        var compra = await prueba.RegistrarCompraAsync(
            proveedor,
            new LineaCompra(productoA.Id, 3, 100m),
            new LineaCompra(productoB.Id, 2, 50m));

        Assert.Equal(400m, compra.Total);
    }

    [Fact]
    public async Task RechazaProveedorInactivo()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        await prueba.ProveedorService.DesactivarAsync(proveedor.Id);

        var resultado = await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null, [new LineaCompra(producto.Id, 1, 50m)]);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task RechazaCompraSinLineas()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();

        var resultado = await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null, []);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task RechazaCantidadCeroONegativa()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        var cero = await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null, [new LineaCompra(producto.Id, 0, 50m)]);
        var negativo = await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null, [new LineaCompra(producto.Id, -1, 50m)]);

        Assert.False(cero.Correcta);
        Assert.False(negativo.Correcta);
    }

    [Fact]
    public async Task RechazaProductoArchivado()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        await prueba.ProductoService.ArchivarAsync(producto.Id);

        var resultado = await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null, [new LineaCompra(producto.Id, 1, 50m)]);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task RechazaProductoRepetido()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        var resultado = await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null,
            [
                new LineaCompra(producto.Id, 1, 50m),
                new LineaCompra(producto.Id, 2, 50m)
            ]);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AnularRevierteStockYCreaMovimientos()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        var compra = await prueba.RegistrarCompraAsync(
            proveedor, new LineaCompra(producto.Id, 10, 50m));

        var resultado = await prueba.CompraService.AnularAsync(compra.Id);
        Assert.True(resultado.Correcta, resultado.Mensaje);

        var actualizado = await prueba.Db.Productos
            .AsNoTracking()
            .FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(0, actualizado.Stock);

        var enBase = await prueba.Db.Compras
            .AsNoTracking()
            .FirstAsync(c => c.Id == compra.Id);
        Assert.Equal(EstadoCompra.Anulada, enBase.Estado);
        Assert.NotNull(enBase.FechaAnulacion);

        var movimientos = await prueba.Db.MovimientosInventario
            .Where(m => m.IdCompra == compra.Id)
            .ToListAsync();

        Assert.Equal(2, movimientos.Count);
        Assert.Contains(movimientos, m => m.Tipo == TipoMovimiento.AnulacionCompra);
    }

    [Fact]
    public async Task AnularRechazaSiNoHayStockParaRevertir()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        var compra = await prueba.RegistrarCompraAsync(
            proveedor, new LineaCompra(producto.Id, 10, 50m));

        var salida = await prueba.InventarioService.RegistrarSalidaAsync(
            producto.Id, 4, "Merma");
        Assert.True(salida.Correcta, salida.Mensaje);

        var resultado = await prueba.CompraService.AnularAsync(compra.Id);

        Assert.False(resultado.Correcta);

        var actualizado = await prueba.Db.Productos
            .AsNoTracking()
            .FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(6, actualizado.Stock);
    }

    [Fact]
    public async Task AnularDosVecesEsRechazado()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        var compra = await prueba.RegistrarCompraAsync(
            proveedor, new LineaCompra(producto.Id, 5, 50m));

        var primera = await prueba.CompraService.AnularAsync(compra.Id);
        var segunda = await prueba.CompraService.AnularAsync(compra.Id);

        Assert.True(primera.Correcta, primera.Mensaje);
        Assert.False(segunda.Correcta);
    }

    [Fact]
    public async Task NoAdministradorNoPuedeRegistrarNiAnular()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");
        var compra = await prueba.RegistrarCompraAsync(
            proveedor, new LineaCompra(producto.Id, 5, 50m));

        await prueba.CrearCajeroEnSesionAsync();

        var registrada = await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null, [new LineaCompra(producto.Id, 1, 50m)]);
        var anulada = await prueba.CompraService.AnularAsync(compra.Id);

        Assert.False(registrada.Correcta);
        Assert.False(anulada.Correcta);
    }

    [Fact]
    public async Task RegistroFallidoNoCreaNada()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        await prueba.ProveedorService.DesactivarAsync(proveedor.Id);

        await prueba.CompraService.RegistrarAsync(
            proveedor.Id, null, [new LineaCompra(producto.Id, 5, 50m)]);

        Assert.Equal(0, await prueba.Db.Compras.CountAsync());
        Assert.Equal(0, await prueba.Db.MovimientosInventario.CountAsync());

        var actualizado = await prueba.Db.Productos
            .AsNoTracking()
            .FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(0, actualizado.Stock);
    }

    [Fact]
    public async Task ObtenerRecientesOrdenaDescendente()
    {
        await using var prueba = await PruebaCompras.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();
        var producto = await prueba.CrearProductoAsync("ABC-001");

        var primera = await prueba.RegistrarCompraAsync(
            proveedor, new LineaCompra(producto.Id, 1, 50m));
        var segunda = await prueba.RegistrarCompraAsync(
            proveedor, new LineaCompra(producto.Id, 1, 50m));

        var recientes = await prueba.CompraService.ObtenerRecientesAsync();

        Assert.Equal(2, recientes.Count);
        Assert.Equal(segunda.Id, recientes[0].Id);
        Assert.Equal(primera.Id, recientes[1].Id);
    }
}
