using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class AjustesV1Tests
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

        public async Task<Usuario> CrearAdministradorAsync()
        {
            var resultado = await Servicio.CrearPrimerAdministradorAsync(
                "admin", "Administrador", "claveSegura123");

            Assert.True(resultado.Correcta, resultado.Mensaje);
            Sesion.Iniciar(resultado.Valor!);
            return resultado.Valor!;
        }

        public async Task<Usuario> CrearCajeroAsync()
        {
            var resultado = await Servicio.AgregarAsync(
                "cajero", "Cajero", "claveCajero1", RolUsuario.Cajero);

            Assert.True(resultado.Correcta, resultado.Mensaje);
            return resultado.Valor!;
        }
    }

    [Fact]
    public async Task UsuariosMuestraSoloActivosPorDefecto()
    {
        await using var prueba = await PruebaUsuarios.CrearAsync();
        var admin = await prueba.CrearAdministradorAsync();
        var cajero = await prueba.CrearCajeroAsync();

        var desactivado = await prueba.Servicio.DesactivarAsync(cajero.Id);
        Assert.True(desactivado.Correcta, desactivado.Mensaje);

        var vm = new UsuariosViewModel(prueba.Servicio, prueba.Sesion);
        await vm.CargarAsync();

        Assert.False(vm.VerInactivos);
        Assert.Single(vm.UsuariosItems);
        Assert.Equal(admin.Id, vm.UsuariosItems[0].Id);
        Assert.DoesNotContain(vm.UsuariosItems, u => u.Id == cajero.Id);

        vm.VerInactivos = true;

        Assert.Equal(2, vm.UsuariosItems.Count);
        Assert.Contains(vm.UsuariosItems, u => u.Id == cajero.Id);
    }

    [Fact]
    public void HistorialSeparaVentasCanceladas()
    {
        var caja = new Caja
        {
            Id = 1,
            FechaApertura = DateTime.Now,
            MontoInicial = 1000m,
            Estado = EstadoCaja.Cerrada
        };

        var completada = new Venta
        {
            Id = 1,
            Fecha = DateTime.Now,
            Subtotal = 100m,
            Total = 100m,
            Estado = EstadoVenta.Completada
        };

        var anulada = new Venta
        {
            Id = 2,
            Fecha = DateTime.Now,
            Subtotal = 200m,
            Total = 200m,
            Estado = EstadoVenta.Anulada
        };

        var vm = new CajaHistorialViewModel(caja, [completada, anulada]);

        Assert.True(vm.HayVentasCanceladas);
        Assert.Single(vm.VentasCanceladas);
        Assert.Equal(anulada.Id, vm.VentasCanceladas[0].Id);
        Assert.Equal(2, vm.CantidadVentas);
        Assert.Equal("300.00", vm.TotalVentasTexto);
    }
}
