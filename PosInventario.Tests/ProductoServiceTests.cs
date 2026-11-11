using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class ProductoServiceTests
{
    private sealed class PruebaProductos : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }

        private PruebaProductos(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
        }

        public static async Task<PruebaProductos> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaProductos(conexion, db);
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
            string sku = "ABC-001",
            string nombre = "Producto de prueba",
            decimal precioVenta = 100m,
            decimal costo = 50m,
            int stockMinimo = 0)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, nombre, null, precioVenta, costo, stockMinimo);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task AgregarCreaProductoConStockCero()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProductoService.AgregarAsync(
            "ABC-001", "Producto de prueba", "Descripción", 1500m, 900m, 5);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Equal(0, resultado.Valor!.Stock);
        Assert.Equal("ABC-001", resultado.Valor.Sku);
        Assert.Single(await prueba.ProductoService.ObtenerTodosAsync());
    }

    [Fact]
    public async Task AgregarNormalizaSkuYRechazaDuplicado()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync("abc-001");
        Assert.Equal("ABC-001", producto.Sku);

        var duplicado = await prueba.ProductoService.AgregarAsync(
            "ABC-001", "Otro producto", null, 10m, 5m, 0);

        Assert.False(duplicado.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaPrecioNegativo()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProductoService.AgregarAsync(
            "ABC-001", "Producto de prueba", null, -1m, 0m, 0);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaNombreCorto()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProductoService.AgregarAsync(
            "ABC-001", "AB", null, 10m, 5m, 0);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task ObtenerTodosSoloDevuelveVigentes()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var vigente = await prueba.CrearProductoAsync("ABC-001", "Vigente");
        var archivado = await prueba.CrearProductoAsync("ABC-002", "Archivado");

        var resultado = await prueba.ProductoService.ArchivarAsync(archivado.Id);
        Assert.True(resultado.Correcta, resultado.Mensaje);

        var todos = await prueba.ProductoService.ObtenerTodosAsync();
        var archivados = await prueba.ProductoService.ObtenerArchivadosAsync();

        Assert.Single(todos);
        Assert.Equal(vigente.Id, todos[0].Id);
        Assert.Single(archivados);
        Assert.Equal(archivado.Id, archivados[0].Id);
    }

    [Fact]
    public async Task ArchivarRechazaProductoConStock()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync();

        var enBase = await prueba.Db.Productos.FindAsync(producto.Id);
        enBase!.Stock = 3;
        await prueba.Db.SaveChangesAsync();

        var resultado = await prueba.ProductoService.ArchivarAsync(producto.Id);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task ArchivarMarcaFechaArchivado()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync();

        var resultado = await prueba.ProductoService.ArchivarAsync(producto.Id);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var enBase = await prueba.Db.Productos.FindAsync(producto.Id);
        Assert.NotNull(enBase!.FechaArchivado);
    }

    [Fact]
    public async Task RestaurarProductoArchivado()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync();
        await prueba.ProductoService.ArchivarAsync(producto.Id);

        var resultado = await prueba.ProductoService.RestaurarAsync(producto.Id);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var enBase = await prueba.Db.Productos.FindAsync(producto.Id);
        Assert.Null(enBase!.FechaArchivado);
    }

    [Fact]
    public async Task ObtenerStockBajoDevuelveSoloLosQueCorresponden()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var bajo = await prueba.CrearProductoAsync("ABC-001", "Bajo", stockMinimo: 5);
        var normal = await prueba.CrearProductoAsync("ABC-002", "Normal", stockMinimo: 5);
        var sinAlerta = await prueba.CrearProductoAsync("ABC-003", "Sin alerta", stockMinimo: 0);

        await CambiarStockAsync(prueba.Db, bajo.Id, 3);
        await CambiarStockAsync(prueba.Db, normal.Id, 10);

        var resultado = await prueba.ProductoService.ObtenerStockBajoAsync();

        Assert.Single(resultado);
        Assert.Equal(bajo.Id, resultado[0].Id);
        Assert.DoesNotContain(resultado, p => p.Id == sinAlerta.Id);
    }

    [Fact]
    public async Task ActualizarNoModificaStock()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var producto = await prueba.CrearProductoAsync();
        await CambiarStockAsync(prueba.Db, producto.Id, 7);

        var resultado = await prueba.ProductoService.ActualizarAsync(
            producto.Id, "ABC-001", "Producto actualizado", null, 200m, 100m, 2);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var enBase = await prueba.Db.Productos.FindAsync(producto.Id);
        Assert.Equal(7, enBase!.Stock);
        Assert.Equal(200m, enBase.PrecioVenta);
    }

    [Fact]
    public async Task NoAdministradorNoPuedeCrearProductos()
    {
        await using var prueba = await PruebaProductos.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();
        await prueba.CrearCajeroEnSesionAsync();

        var resultado = await prueba.ProductoService.AgregarAsync(
            "ABC-001", "Producto de prueba", null, 10m, 5m, 0);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task NoAdministradorNoPuedeArchivarProductos()
    {
        await using var prueba = await PruebaProductos.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync();

        await prueba.CrearCajeroEnSesionAsync();

        var resultado = await prueba.ProductoService.ArchivarAsync(producto.Id);

        Assert.False(resultado.Correcta);
    }

    private static async Task CambiarStockAsync(
        AppDbContext db, int idProducto, int stock)
    {
        var producto = await db.Productos.FindAsync(idProducto);
        producto!.Stock = stock;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ObtenerPorSkuIgnoraMayusculasYExcluyeArchivados()
    {
        await using var prueba = await PruebaProductos.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();
        var producto = await prueba.CrearProductoAsync("SKU-ABC");

        var encontrado = await prueba.ProductoService.ObtenerPorSkuAsync(" sku-abc ");

        Assert.NotNull(encontrado);
        Assert.Equal(producto.Id, encontrado!.Id);

        Assert.Null(await prueba.ProductoService.ObtenerPorSkuAsync("NO-EXISTE"));

        var archivado = await prueba.ProductoService.ArchivarAsync(producto.Id);
        Assert.True(archivado.Correcta, archivado.Mensaje);

        Assert.Null(await prueba.ProductoService.ObtenerPorSkuAsync("SKU-ABC"));
    }
}
