using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class ProductosViewModelTests
{
    private sealed class Contexto : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public ProductoService ProductoService { get; }
        public ProductosViewModel ViewModel { get; }

        private Contexto(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            ProductoService = new ProductoService(db, Sesion);
            ViewModel = new ProductosViewModel(ProductoService);
        }

        public static async Task<Contexto> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new Contexto(conexion, db);
        }

        public async Task CrearAdministradorAsync()
        {
            var usuarioService = new UsuarioService(Db, Sesion);
            var resultado = await usuarioService.CrearPrimerAdministradorAsync(
                "admin", "Administrador", "claveSegura123");

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);
        }

        public async Task<Producto> CrearProductoAsync(
            string sku, string nombre = "Producto", decimal precio = 10m)
        {
            var resultado = await ProductoService.AgregarAsync(
                sku, nombre, null, precio, 5m, 1);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _conexion.DisposeAsync();
        }
    }

    [Fact]
    public async Task EscanearCodigoExistenteCargaElProducto()
    {
        await using var contexto = await Contexto.CrearAsync();
        await contexto.CrearAdministradorAsync();
        await contexto.CrearProductoAsync("ABC-001", "Café");
        await contexto.ViewModel.CargarAsync();

        await contexto.ViewModel.EscanearCodigoAsync("abc-001");

        Assert.NotNull(contexto.ViewModel.SelectedProducto);
        Assert.Equal("ABC-001", contexto.ViewModel.Sku);
        Assert.Equal("Café", contexto.ViewModel.Nombre);
        Assert.False(contexto.ViewModel.EsModoCreacion);
    }

    [Fact]
    public async Task EscanearCodigoNuevoPreparaElAlta()
    {
        await using var contexto = await Contexto.CrearAsync();
        await contexto.CrearAdministradorAsync();
        await contexto.ViewModel.CargarAsync();

        await contexto.ViewModel.EscanearCodigoAsync("NUEVO-99");

        Assert.Null(contexto.ViewModel.SelectedProducto);
        Assert.True(contexto.ViewModel.EsModoCreacion);
        Assert.Equal("NUEVO-99", contexto.ViewModel.Sku);
        Assert.Equal(string.Empty, contexto.ViewModel.Nombre);
    }

    [Fact]
    public async Task EscanearCodigoArchivadoLoCargaParaRestaurar()
    {
        await using var contexto = await Contexto.CrearAsync();
        await contexto.CrearAdministradorAsync();
        var producto = await contexto.CrearProductoAsync("VIEJO-01");

        var archivado = await contexto.ProductoService.ArchivarAsync(producto.Id);
        Assert.True(archivado.Correcta, archivado.Mensaje);

        await contexto.ViewModel.CargarAsync();
        await contexto.ViewModel.EscanearCodigoAsync("VIEJO-01");

        Assert.True(contexto.ViewModel.VerArchivados);
        Assert.NotNull(contexto.ViewModel.SelectedProducto);
        Assert.True(contexto.ViewModel.PuedeRestaurar);
    }

    [Fact]
    public async Task EscanearCodigoVacioNoCambiaNada()
    {
        await using var contexto = await Contexto.CrearAsync();
        await contexto.CrearAdministradorAsync();
        await contexto.ViewModel.CargarAsync();

        await contexto.ViewModel.EscanearCodigoAsync("   ");

        Assert.Null(contexto.ViewModel.SelectedProducto);
        Assert.Equal(string.Empty, contexto.ViewModel.Sku);
    }

    [Fact]
    public void CambiarLaUnidadActualizaLasEtiquetas()
    {
        var vm = new ProductosViewModel();

        Assert.False(vm.EsPorPeso);
        Assert.Equal("Precio de venta", vm.EtiquetaPrecio);
        Assert.Equal("Stock mínimo", vm.EtiquetaStockMinimo);

        vm.UnidadMedidaSeleccionada = UnidadMedida.Kilogramo;

        Assert.True(vm.EsPorPeso);
        Assert.Equal("Precio por kg", vm.EtiquetaPrecio);
        Assert.Equal("Costo por kg", vm.EtiquetaCosto);
        Assert.Equal("Stock mínimo (kg)", vm.EtiquetaStockMinimo);
    }
}
