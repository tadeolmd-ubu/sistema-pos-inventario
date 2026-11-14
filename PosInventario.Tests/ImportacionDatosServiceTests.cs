using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class ImportacionDatosServiceTests
{
    private sealed class PruebaImportacion : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;
        private readonly string _carpeta;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProductoService ProductoService { get; }
        public ProveedorService ProveedorService { get; }
        public InventarioService InventarioService { get; }
        public ImportacionDatosService Service { get; }

        private PruebaImportacion(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProductoService = new ProductoService(db, Sesion);
            ProveedorService = new ProveedorService(db, Sesion);
            InventarioService = new InventarioService(db, Sesion);
            Service = new ImportacionDatosService(db, Sesion);

            _carpeta = Path.Combine(
                Path.GetTempPath(), $"pos-importacion-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_carpeta);
        }

        public static async Task<PruebaImportacion> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaImportacion(conexion, db);
        }

        public async Task CrearAdministradorEnSesionAsync()
        {
            var resultado = await UsuarioService.CrearPrimerAdministradorAsync(
                "admin", "Administrador", "claveSegura123");

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);
        }

        public async Task<string> ArchivoAsync(
            string contenido, string extension = ".csv")
        {
            var ruta = Path.Combine(_carpeta, $"datos{extension}");
            await File.WriteAllTextAsync(ruta, contenido, new UTF8Encoding(true));
            return ruta;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _conexion.DisposeAsync();

            if (Directory.Exists(_carpeta))
                Directory.Delete(_carpeta, recursive: true);
        }
    }

    [Fact]
    public async Task ImportaProductosProveedoresYStockInicial()
    {
        await using var prueba = await PruebaImportacion.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var ruta = await prueba.ArchivoAsync(
            """
            sku,nombre,descripcion,precio_venta,costo,stock_minimo,stock_inicial,proveedor,proveedor_telefono,proveedor_email,proveedor_direccion
            ABC-1,Producto uno,Descripción,25.50,10,3,10,Proveedor Uno,555-1234,prove@example.com,Calle 1
            ABC-2,Producto dos,,15,7.25,1,4,,,,
            """);

        var lectura = await prueba.Service.LeerAsync(ruta);

        Assert.True(lectura.Correcta, lectura.Mensaje);
        var vista = lectura.Valor!;
        Assert.Empty(vista.Errores);
        Assert.True(vista.PuedeImportar);
        Assert.Equal(2, vista.ProductosNuevos);
        Assert.Equal(0, vista.ProductosActualizados);
        Assert.Equal(1, vista.ProveedoresNuevos);
        Assert.Equal(0, vista.ProveedoresReutilizados);
        Assert.Equal(14, vista.UnidadesStock);

        var importacion = await prueba.Service.ImportarAsync(vista);

        Assert.True(importacion.Correcta, importacion.Mensaje);
        Assert.Equal(2, importacion.Valor!.ProductosNuevos);
        Assert.Equal(14, importacion.Valor.UnidadesStock);

        var productos = await prueba.Db.Productos
            .OrderBy(p => p.Sku)
            .ToListAsync();

        Assert.Equal(2, productos.Count);

        var primero = productos[0];
        Assert.Equal("ABC-1", primero.Sku);
        Assert.Equal("Producto uno", primero.Nombre);
        Assert.Equal("Descripción", primero.Descripcion);
        Assert.Equal(25.50m, primero.PrecioVenta);
        Assert.Equal(10m, primero.Costo);
        Assert.Equal(3, primero.StockMinimo);
        Assert.Equal(10, primero.Stock);

        Assert.Equal(4, productos[1].Stock);
        Assert.Equal(1, await prueba.Db.Proveedores.CountAsync());

        var movimientos = await prueba.Db.MovimientosInventario.ToListAsync();

        Assert.Equal(2, movimientos.Count);
        Assert.All(movimientos, m =>
        {
            Assert.Equal(TipoMovimiento.Entrada, m.Tipo);
            Assert.Equal("Importación inicial", m.Referencia);
            Assert.Equal(0, m.StockAnterior);
        });
    }

    [Fact]
    public async Task ActualizaProductoExistenteSinTocarSuStock()
    {
        await using var prueba = await PruebaImportacion.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var creado = await prueba.ProductoService.AgregarAsync(
            "ABC-1", "Producto viejo", null, 10, 5, 1);

        Assert.True(creado.Correcta, creado.Mensaje);

        var entrada = await prueba.InventarioService.RegistrarEntradaAsync(
            creado.Valor!.Id, 5, "Carga inicial");

        Assert.True(entrada.Correcta, entrada.Mensaje);

        var ruta = await prueba.ArchivoAsync(
            """
            sku,nombre,descripcion,precio_venta,costo,stock_minimo,stock_inicial
            ABC-1,Producto nuevo,Descripción nueva,20,8,2,99
            """);

        var lectura = await prueba.Service.LeerAsync(ruta);
        var vista = lectura.Valor!;

        Assert.True(lectura.Correcta, lectura.Mensaje);
        Assert.Empty(vista.Errores);
        Assert.Equal(0, vista.ProductosNuevos);
        Assert.Equal(1, vista.ProductosActualizados);
        Assert.Equal(0, vista.UnidadesStock);

        var importacion = await prueba.Service.ImportarAsync(vista);
        Assert.True(importacion.Correcta, importacion.Mensaje);

        var producto = await prueba.Db.Productos.SingleAsync(p => p.Sku == "ABC-1");

        Assert.Equal("Producto nuevo", producto.Nombre);
        Assert.Equal("Descripción nueva", producto.Descripcion);
        Assert.Equal(20m, producto.PrecioVenta);
        Assert.Equal(8m, producto.Costo);
        Assert.Equal(2, producto.StockMinimo);
        Assert.Equal(5, producto.Stock);
        Assert.Equal(1, await prueba.Db.Productos.CountAsync());
        Assert.Equal(1, await prueba.Db.MovimientosInventario.CountAsync());
    }

    [Fact]
    public async Task ReutilizaProveedorExistentePorTelefono()
    {
        await using var prueba = await PruebaImportacion.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var proveedor = await prueba.ProveedorService.AgregarAsync(
            "Proveedor Uno", "555-1234", null, null);

        Assert.True(proveedor.Correcta, proveedor.Mensaje);

        var ruta = await prueba.ArchivoAsync(
            """
            sku,nombre,precio_venta,costo,stock_minimo,stock_inicial,proveedor,proveedor_telefono
            ABC-1,Producto uno,10,5,1,2,Proveedor Uno,555-1234
            """);

        var vista = (await prueba.Service.LeerAsync(ruta)).Valor!;

        Assert.Equal(0, vista.ProveedoresNuevos);
        Assert.Equal(1, vista.ProveedoresReutilizados);

        var importacion = await prueba.Service.ImportarAsync(vista);

        Assert.True(importacion.Correcta, importacion.Mensaje);
        Assert.Equal(1, await prueba.Db.Proveedores.CountAsync());
        Assert.Equal(1, await prueba.Db.Productos.CountAsync());
    }

    [Fact]
    public async Task RechazaElArchivoSiHayFilasInvalidas()
    {
        await using var prueba = await PruebaImportacion.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var ruta = await prueba.ArchivoAsync(
            """
            sku,nombre,precio_venta,costo,stock_minimo,stock_inicial
            ABC-1,Producto bueno,10,5,1,2
            ABC-2,Producto malo,abc,5,1,0
            """);

        var lectura = await prueba.Service.LeerAsync(ruta);
        var vista = lectura.Valor!;

        Assert.True(lectura.Correcta, lectura.Mensaje);
        Assert.NotEmpty(vista.Errores);
        Assert.False(vista.PuedeImportar);
        Assert.Contains(vista.Errores, e => e.Mensaje.Contains("precio"));

        var importacion = await prueba.Service.ImportarAsync(vista);

        Assert.False(importacion.Correcta);
        Assert.Equal(0, await prueba.Db.Productos.CountAsync());
    }

    [Fact]
    public async Task DetectaSkuRepetidoEnElArchivo()
    {
        await using var prueba = await PruebaImportacion.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var ruta = await prueba.ArchivoAsync(
            """
            sku,nombre,precio_venta,costo,stock_minimo,stock_inicial
            ABC-1,Producto uno,10,5,1,0
            ABC-1,Producto repetido,12,6,1,0
            """);

        var vista = (await prueba.Service.LeerAsync(ruta)).Valor!;

        Assert.False(vista.PuedeImportar);
        Assert.Contains(vista.Errores, e => e.Mensaje.Contains("repetido"));
    }

    [Fact]
    public async Task AceptaPuntoYComaYComillasConSaltosDeLinea()
    {
        await using var prueba = await PruebaImportacion.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var ruta = await prueba.ArchivoAsync(
            "sku;nombre;descripcion;precio_venta;costo;stock_minimo;stock_inicial\n" +
            "A-1;Producto;\"Línea 1\nLínea 2\";10;5;1;2\n");

        var vista = (await prueba.Service.LeerAsync(ruta)).Valor!;

        Assert.Empty(vista.Errores);
        Assert.Single(vista.Filas);

        var importacion = await prueba.Service.ImportarAsync(vista);
        Assert.True(importacion.Correcta, importacion.Mensaje);

        var producto = await prueba.Db.Productos.SingleAsync();

        Assert.Equal("Línea 1\nLínea 2", producto.Descripcion);
        Assert.Equal(2, producto.Stock);
    }

    [Fact]
    public async Task ExigeSesionDeAdministrador()
    {
        await using var prueba = await PruebaImportacion.CrearAsync();

        var ruta = await prueba.ArchivoAsync(
            """
            sku,nombre,precio_venta,costo,stock_minimo,stock_inicial
            ABC-1,Producto uno,10,5,1,0
            """);

        var lectura = await prueba.Service.LeerAsync(ruta);

        Assert.False(lectura.Correcta);
        Assert.Contains(
            "administrador", lectura.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LaPlantillaTieneLasColumnasObligatorias()
    {
        var registros = ImportadorCsv.Leer(ImportadorCsv.Plantilla);

        Assert.Single(registros);

        var encabezados = registros[0].Campos
            .Select(ImportadorCsv.NormalizarEncabezado)
            .ToList();

        Assert.Contains("sku", encabezados);
        Assert.Contains("nombre", encabezados);
        Assert.Contains("precio_venta", encabezados);
        Assert.Contains("stock_inicial", encabezados);
        Assert.Contains("proveedor_telefono", encabezados);
    }

    [Theory]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1234,56", 1234.56)]
    [InlineData("1,234", 1234)]
    [InlineData("12,50", 12.50)]
    [InlineData("$ 12.50", 12.50)]
    [InlineData("", 0)]
    public void InterpretaImportesDeExcel(string texto, double esperado)
    {
        var correcto = ImportadorCsv.TryParseDecimal(texto, out var valor);

        Assert.True(correcto);
        Assert.Equal((decimal)esperado, valor);
    }
}
