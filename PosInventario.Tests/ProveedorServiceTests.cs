using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class ProveedorServiceTests
{
    private sealed class PruebaProveedores : IAsyncDisposable
    {
        private readonly SqliteConnection _conexion;

        public AppDbContext Db { get; }
        public SesionActual Sesion { get; } = new();
        public UsuarioService UsuarioService { get; }
        public ProveedorService ProveedorService { get; }

        private PruebaProveedores(SqliteConnection conexion, AppDbContext db)
        {
            _conexion = conexion;
            Db = db;
            UsuarioService = new UsuarioService(db, Sesion);
            ProveedorService = new ProveedorService(db, Sesion);
        }

        public static async Task<PruebaProveedores> CrearAsync()
        {
            var conexion = new SqliteConnection("Data Source=:memory:");
            await conexion.OpenAsync();

            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(conexion)
                    .Options);

            await db.Database.MigrateAsync();

            return new PruebaProveedores(conexion, db);
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

        public async Task<Proveedor> CrearProveedorAsync(
            string nombre = "Distribuidora Central",
            string telefono = "11 5555-1000",
            string? email = "ventas@central.com",
            string? direccion = "Av. Siempre Viva 123")
        {
            var resultado = await ProveedorService.AgregarAsync(
                nombre, telefono, email, direccion);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task AgregarCreaProveedorActivo()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProveedorService.AgregarAsync(
            "Distribuidora Central", "11 5555-1000", "ventas@central.com", null);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Equal(1, resultado.Valor!.Activo);
        Assert.Equal("Distribuidora Central", resultado.Valor.Nombre);
        Assert.Single(await prueba.ProveedorService.ObtenerTodosAsync());
    }

    [Fact]
    public async Task AgregarRechazaTelefonoDuplicado()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        await prueba.CrearProveedorAsync();

        var resultado = await prueba.ProveedorService.AgregarAsync(
            "Otro proveedor", "11 5555-1000", null, null);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaNombreVacio()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProveedorService.AgregarAsync(
            "", "11 5555-1000", null, null);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaTelefonoVacio()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProveedorService.AgregarAsync(
            "Distribuidora Central", "   ", null, null);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AgregarRechazaEmailInvalido()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProveedorService.AgregarAsync(
            "Distribuidora Central", "11 5555-1000", "esto-no-es-un-email", null);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task AgregarNormalizaOpcionalesVaciosANull()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var resultado = await prueba.ProveedorService.AgregarAsync(
            "Distribuidora Central", "11 5555-1000", "   ", "   ");

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Null(resultado.Valor!.Email);
        Assert.Null(resultado.Valor.Direccion);
    }

    [Fact]
    public async Task DesactivarYActivarCambianElEstado()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var proveedor = await prueba.CrearProveedorAsync();

        var desactivado = await prueba.ProveedorService.DesactivarAsync(proveedor.Id);
        Assert.True(desactivado.Correcta, desactivado.Mensaje);

        var enBase = await prueba.Db.Proveedores.FindAsync(proveedor.Id);
        Assert.Equal(0, enBase!.Activo);

        var activado = await prueba.ProveedorService.ActivarAsync(proveedor.Id);
        Assert.True(activado.Correcta, activado.Mensaje);

        enBase = await prueba.Db.Proveedores.FindAsync(proveedor.Id);
        Assert.Equal(1, enBase!.Activo);
    }

    [Fact]
    public async Task ObtenerActivosFiltraInactivos()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var activo = await prueba.CrearProveedorAsync(
            "Activo", "11 5555-1000");
        var inactivo = await prueba.CrearProveedorAsync(
            "Inactivo", "11 5555-2000");

        await prueba.ProveedorService.DesactivarAsync(inactivo.Id);

        var activos = await prueba.ProveedorService.ObtenerActivosAsync();

        Assert.Single(activos);
        Assert.Equal(activo.Id, activos[0].Id);
        Assert.Equal(2, (await prueba.ProveedorService.ObtenerTodosAsync()).Count);
    }

    [Fact]
    public async Task ActualizarNoCambiaActivo()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();
        await prueba.CrearAdministradorEnSesionAsync();

        var proveedor = await prueba.CrearProveedorAsync();
        await prueba.ProveedorService.DesactivarAsync(proveedor.Id);

        var resultado = await prueba.ProveedorService.ActualizarAsync(
            proveedor.Id, "Proveedor actualizado", "11 5555-9999",
            "nuevo@central.com", null);

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var enBase = await prueba.Db.Proveedores.FindAsync(proveedor.Id);
        Assert.Equal(0, enBase!.Activo);
        Assert.Equal("Proveedor actualizado", enBase.Nombre);
    }

    [Fact]
    public async Task NoAdministradorNoPuedeCrearNiEditar()
    {
        await using var prueba = await PruebaProveedores.CrearAsync();

        await prueba.CrearAdministradorEnSesionAsync();
        var proveedor = await prueba.CrearProveedorAsync();

        await prueba.CrearCajeroEnSesionAsync();

        var agregado = await prueba.ProveedorService.AgregarAsync(
            "Otro proveedor", "11 5555-3000", null, null);
        Assert.False(agregado.Correcta);

        var actualizado = await prueba.ProveedorService.ActualizarAsync(
            proveedor.Id, "Cambio", "11 5555-1000", null, null);
        Assert.False(actualizado.Correcta);

        var desactivado = await prueba.ProveedorService.DesactivarAsync(proveedor.Id);
        Assert.False(desactivado.Correcta);
    }
}
