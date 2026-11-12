using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class RespaldoServiceTests
{
    private sealed class Entorno : IAsyncDisposable
    {
        private readonly string _carpeta;

        public string RutaDb { get; }
        public string CarpetaRespaldos { get; }
        public AppDbContext Db { get; }
        public RespaldoService Servicio { get; }

        private Entorno(string carpeta, AppDbContext db)
        {
            _carpeta = carpeta;
            RutaDb = Path.Combine(carpeta, "SistemaInventario.db");
            CarpetaRespaldos = Path.Combine(carpeta, "Respaldos");
            Db = db;
            Servicio = new RespaldoService(db);
        }

        public static async Task<Entorno> CrearAsync()
        {
            var carpeta = Path.Combine(
                Path.GetTempPath(),
                "PosInventario-tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(carpeta);

            var rutaDb = Path.Combine(carpeta, "SistemaInventario.db");
            var cadena = new SqliteConnectionStringBuilder
            {
                DataSource = rutaDb,
                Pooling = false
            }.ToString();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(cadena)
                    .Options);

            await db.Database.MigrateAsync();

            return new Entorno(carpeta, db);
        }

        public async Task AgregarUsuarioAsync(string nombreUsuario)
        {
            Db.Usuarios.Add(new Usuario
            {
                NombreUsuario = nombreUsuario,
                Nombre = nombreUsuario,
                ContrasenaHash = "hash",
                Rol = RolUsuario.Cajero,
                Activo = 1,
                FechaCreacion = DateTime.Now
            });

            await Db.SaveChangesAsync();
        }

        public string RutaDeArchivo(string nombre)
            => Path.Combine(_carpeta, nombre);

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();

            SqliteConnection.ClearAllPools();

            try { Directory.Delete(_carpeta, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private sealed class RespaldoServiceConVerificacionFallida : RespaldoService
    {
        public RespaldoServiceConVerificacionFallida(AppDbContext db) : base(db) { }

        protected override Task<Resultado<RespaldoInfo>>
            ValidarRespaldoGeneradoAsync(string ruta)
            => Task.FromResult(new Resultado<RespaldoInfo>(
                false, "La verificación simulada falló.", null));
    }

    [Fact]
    public async Task RespaldoValidoSePuedeRestaurar()
    {
        await using var entorno = await Entorno.CrearAsync();
        await entorno.AgregarUsuarioAsync("admin");

        var respaldo = await entorno.Servicio.CrearRespaldoAsync(
            entorno.CarpetaRespaldos);

        Assert.True(respaldo.Correcta, respaldo.Mensaje);
        Assert.NotNull(respaldo.Valor);

        var validacion = await entorno.Servicio.ValidarRespaldoAsync(respaldo.Valor);

        Assert.True(validacion.Correcta, validacion.Mensaje);
        Assert.NotNull(validacion.Valor);
        Assert.Equal(respaldo.Valor, validacion.Valor.Ruta);
        Assert.True(validacion.Valor.TamanoBytes > 0);
        Assert.True(validacion.Valor.CantidadMigraciones > 0);
        Assert.Empty(Directory.GetFiles(
            entorno.CarpetaRespaldos, ".respaldo_*"));
    }

    [Fact]
    public async Task RespaldoQueNoPasaLaVerificacionNoSeGuarda()
    {
        await using var entorno = await Entorno.CrearAsync();
        var servicio = new RespaldoServiceConVerificacionFallida(entorno.Db);

        var resultado = await servicio.CrearRespaldoAsync(
            entorno.CarpetaRespaldos);

        Assert.False(resultado.Correcta);
        Assert.Contains("verificación", resultado.Mensaje);
        Assert.Empty(Directory.GetFiles(entorno.CarpetaRespaldos));
    }

    [Fact]
    public async Task ArchivoCorruptoNoSeRestaura()
    {
        await using var entorno = await Entorno.CrearAsync();
        await entorno.AgregarUsuarioAsync("actual");

        var rutaTexto = entorno.RutaDeArchivo("corrupto.db");
        await File.WriteAllTextAsync(rutaTexto, "esto no es una base de datos");

        var validacionTexto = await entorno.Servicio.ValidarRespaldoAsync(rutaTexto);

        Assert.False(validacionTexto.Correcta);

        var respaldo = await entorno.Servicio.CrearRespaldoAsync(
            entorno.CarpetaRespaldos);

        Assert.True(respaldo.Correcta, respaldo.Mensaje);
        Assert.NotNull(respaldo.Valor);

        var contenido = await File.ReadAllBytesAsync(respaldo.Valor);
        var rutaTruncada = entorno.RutaDeArchivo("truncado.db");
        await File.WriteAllBytesAsync(
            rutaTruncada, contenido[..(contenido.Length / 2)]);

        var validacionTruncada = await entorno.Servicio.ValidarRespaldoAsync(
            rutaTruncada);

        Assert.False(validacionTruncada.Correcta);

        var restauracion = await entorno.Servicio.RestaurarRespaldoAsync(
            rutaTruncada, entorno.CarpetaRespaldos);

        Assert.False(restauracion.Correcta);

        var usuarios = await entorno.Db.Usuarios
            .AsNoTracking()
            .CountAsync();

        Assert.Equal(1, usuarios);
    }

    [Fact]
    public async Task RestauracionCompletaReemplazaLaBaseActual()
    {
        await using var entorno = await Entorno.CrearAsync();
        await entorno.AgregarUsuarioAsync("antes");

        var respaldo = await entorno.Servicio.CrearRespaldoAsync(
            entorno.CarpetaRespaldos);

        Assert.True(respaldo.Correcta, respaldo.Mensaje);
        Assert.NotNull(respaldo.Valor);

        await entorno.AgregarUsuarioAsync("despues");

        var restauracion = await entorno.Servicio.RestaurarRespaldoAsync(
            respaldo.Valor, entorno.CarpetaRespaldos);

        Assert.True(restauracion.Correcta, restauracion.Mensaje);
        Assert.NotNull(restauracion.Valor);
        Assert.True(File.Exists(restauracion.Valor.RutaRespaldoSeguridad));

        var validacionSeguridad = await entorno.Servicio.ValidarRespaldoAsync(
            restauracion.Valor.RutaRespaldoSeguridad);

        Assert.True(validacionSeguridad.Correcta, validacionSeguridad.Mensaje);

        var cadena = new SqliteConnectionStringBuilder
        {
            DataSource = entorno.RutaDb,
            Pooling = false
        }.ToString();

        await using (var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(cadena)
                .Options))
        {
            var usuarios = await db.Usuarios
                .AsNoTracking()
                .Select(u => u.NombreUsuario)
                .ToListAsync();

            Assert.Contains("antes", usuarios);
            Assert.DoesNotContain("despues", usuarios);
        }

        var cadenaSeguridad = new SqliteConnectionStringBuilder
        {
            DataSource = restauracion.Valor.RutaRespaldoSeguridad,
            Pooling = false
        }.ToString();

        await using (var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(cadenaSeguridad)
                .Options))
        {
            var usuarios = await db.Usuarios
                .AsNoTracking()
                .Select(u => u.NombreUsuario)
                .ToListAsync();

            Assert.Contains("despues", usuarios);
            Assert.Contains("antes", usuarios);
        }
    }

    [Fact]
    public async Task RestauracionRequiereAdministrador()
    {
        await using var entorno = await Entorno.CrearAsync();

        var respaldo = await entorno.Servicio.CrearRespaldoAsync(
            entorno.CarpetaRespaldos);

        Assert.True(respaldo.Correcta, respaldo.Mensaje);
        Assert.NotNull(respaldo.Valor);

        var sesion = new SesionActual();
        sesion.Iniciar(new Usuario
        {
            NombreUsuario = "cajero",
            Nombre = "Cajero",
            Rol = RolUsuario.Cajero
        });

        var servicio = new RespaldoService(entorno.Db, sesion);

        var restauracion = await servicio.RestaurarRespaldoAsync(
            respaldo.Valor, entorno.CarpetaRespaldos);

        Assert.False(restauracion.Correcta);
        Assert.Contains("administrador", restauracion.Mensaje);
    }
}
