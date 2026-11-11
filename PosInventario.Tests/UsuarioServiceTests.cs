using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class UsuarioServiceTests
{
    private sealed class PruebaUsuarios : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService Servicio { get; }

        private PruebaUsuarios(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            Servicio = new UsuarioService(db, Sesion);
        }

        public static async Task<PruebaUsuarios> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaUsuarios(conexion, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _conexion.DisposeAsync();
        }

        /// <summary>
        /// Crea el primer administrador y lo deja como usuario de la sesión.
        /// </summary>
        public async Task<Usuario> CrearAdministradorEnSesionAsync(
            string nombreUsuario = "admin",
            string nombre = "Administrador",
            string contrasena = "claveSegura123")
        {
            var resultado = await Servicio.CrearPrimerAdministradorAsync(
                nombreUsuario, nombre, contrasena);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);

            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task CrearPrimerAdministradorCreaUnAdminActivo()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        var resultado = await prueba.Servicio.CrearPrimerAdministradorAsync(
            "admin", "Administrador", "claveSegura123");

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Equal(RolUsuario.Administrador, resultado.Valor!.Rol);
        Assert.Equal(1, resultado.Valor.Activo);
        Assert.StartsWith("pbkdf2-sha256$", resultado.Valor.ContrasenaHash);
        Assert.Single(await prueba.Servicio.ObtenerTodosAsync());
    }

    [Fact]
    public async Task CrearPrimerAdministradorFallaSiYaHayUsuarios()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.Servicio.CrearPrimerAdministradorAsync(
            "otro", "Otro", "claveSegura123");

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaNombreDeUsuarioDuplicadoSinImportarMayusculas()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var primera = await prueba.Servicio.AgregarAsync(
            "laura", "Laura Gómez", "claveDeLaura1", RolUsuario.Cajero);
        Assert.True(primera.Correcta, primera.Mensaje);

        var duplicada = await prueba.Servicio.AgregarAsync(
            "LAURA", "Laura Gómez", "claveDeLaura1", RolUsuario.Cajero);

        Assert.False(duplicada.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaContrasenaCorta()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.Servicio.AgregarAsync(
            "laura", "Laura Gómez", "corta", RolUsuario.Cajero);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaCuandoLaSesionNoEsAdministrador()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var cajero = await prueba.Servicio.AgregarAsync(
            "laura", "Laura Gómez", "claveDeLaura1", RolUsuario.Cajero);
        Assert.True(cajero.Correcta, cajero.Mensaje);

        prueba.Sesion.Iniciar(cajero.Valor!);

        var resultado = await prueba.Servicio.AgregarAsync(
            "pedro", "Pedro", "claveDePedro1", RolUsuario.Cajero);

        Assert.False(resultado.Correcta);
        Assert.False(prueba.Sesion.EsAdministrador);
    }

    [Fact]
    public async Task ValidarCredencialesDevuelveUsuarioYActualizaUltimoAcceso()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.Servicio.ValidarCredencialesAsync(
            "admin", "claveSegura123");

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.NotNull(resultado.Valor!.FechaUltimoAcceso);
    }

    [Fact]
    public async Task ValidarCredencialesRechazaContrasenaIncorrecta()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.Servicio.ValidarCredencialesAsync(
            "admin", "claveIncorrecta");

        Assert.False(resultado.Correcta);
        Assert.Null(resultado.Valor);
    }

    [Fact]
    public async Task ValidarCredencialesRechazaUsuarioInactivo()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var cajero = await prueba.Servicio.AgregarAsync(
            "laura", "Laura Gómez", "claveDeLaura1", RolUsuario.Cajero);
        Assert.True(cajero.Correcta, cajero.Mensaje);

        var desactivado = await prueba.Servicio.DesactivarAsync(cajero.Valor!.Id);
        Assert.True(desactivado.Correcta, desactivado.Mensaje);

        var resultado = await prueba.Servicio.ValidarCredencialesAsync(
            "laura", "claveDeLaura1");

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task DesactivarRechazaDesactivarseASiMismo()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        var admin = await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.Servicio.DesactivarAsync(admin.Id);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task ActualizarRechazaDegradarAlUltimoAdministradorActivo()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        var admin = await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.Servicio.ActualizarAsync(
            admin.Id, "admin", "Administrador", RolUsuario.Cajero);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task RestablecerContrasenaPermiteIngresarConLaNueva()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var cajero = await prueba.Servicio.AgregarAsync(
            "laura", "Laura Gómez", "claveDeLaura1", RolUsuario.Cajero);
        Assert.True(cajero.Correcta, cajero.Mensaje);

        var restablecida = await prueba.Servicio.RestablecerContrasenaAsync(
            cajero.Valor!.Id, "nuevaClave99");
        Assert.True(restablecida.Correcta, restablecida.Mensaje);

        var conLaVieja = await prueba.Servicio.ValidarCredencialesAsync(
            "laura", "claveDeLaura1");
        Assert.False(conLaVieja.Correcta);

        var conLaNueva = await prueba.Servicio.ValidarCredencialesAsync(
            "laura", "nuevaClave99");
        Assert.True(conLaNueva.Correcta, conLaNueva.Mensaje);
    }

    [Fact]
    public async Task ActivarPermiteVolverAIngresar()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();

        var cajero = await prueba.Servicio.AgregarAsync(
            "laura", "Laura Gómez", "claveDeLaura1", RolUsuario.Cajero);

        await prueba.Servicio.DesactivarAsync(cajero.Valor!.Id);

        var activado = await prueba.Servicio.ActivarAsync(cajero.Valor!.Id);
        Assert.True(activado.Correcta, activado.Mensaje);

        var resultado = await prueba.Servicio.ValidarCredencialesAsync(
            "laura", "claveDeLaura1");

        Assert.True(resultado.Correcta, resultado.Mensaje);
    }

    [Fact]
    public async Task ElHashGuardadoNoContieneLaContrasenaEnTextoPlano()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();

        var resultado = await prueba.Servicio.CrearPrimerAdministradorAsync(
            "admin", "Administrador", "claveSegura123");

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.DoesNotContain("claveSegura123", resultado.Valor!.ContrasenaHash);
        Assert.StartsWith("pbkdf2-sha256$100000$", resultado.Valor.ContrasenaHash);
    }
}
