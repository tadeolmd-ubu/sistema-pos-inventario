using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Service;

namespace PosInventario.Tests;

public class RespaldoAutomaticoServiceTests
{
    private sealed class Entorno : IAsyncDisposable
    {
        private readonly string _carpeta;

        public string CarpetaRespaldos { get; }
        public AppDbContext Db { get; }
        public ConfiguracionAppService Configuracion { get; }
        public RespaldoAutomaticoService Automatico { get; }

        private Entorno(
            string carpeta,
            AppDbContext db,
            ConfiguracionAppService configuracion)
        {
            _carpeta = carpeta;
            CarpetaRespaldos = Path.Combine(carpeta, "Respaldos");
            Db = db;
            Configuracion = configuracion;
            Automatico = new RespaldoAutomaticoService(
                new RespaldoService(db), configuracion, CarpetaRespaldos);
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

            var configuracion = new ConfiguracionAppService(
                Path.Combine(carpeta, "configuracion.json"));

            return new Entorno(carpeta, db, configuracion);
        }

        public string[] RespaldosAutomaticos()
        {
            if (!Directory.Exists(CarpetaRespaldos))
                return [];

            return Directory.GetFiles(CarpetaRespaldos, "respaldo_auto_*.db");
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();

            SqliteConnection.ClearAllPools();

            try { Directory.Delete(_carpeta, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [Fact]
    public async Task CierreDeCajaCreaRespaldoAutomatico()
    {
        await using var entorno = await Entorno.CrearAsync();

        var resultado = await entorno.Automatico.CrearAlCerrarCajaAsync();

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.NotNull(resultado.Valor);
        Assert.True(File.Exists(resultado.Valor));
        Assert.StartsWith(
            "respaldo_auto_", Path.GetFileName(resultado.Valor));

        var validacion = await new RespaldoService(entorno.Db)
            .ValidarRespaldoAsync(resultado.Valor);

        Assert.True(validacion.Correcta, validacion.Mensaje);
    }

    [Fact]
    public async Task DiarioCreaSoloUnRespaldoPorDia()
    {
        await using var entorno = await Entorno.CrearAsync();

        var primero = await entorno.Automatico.CrearDiarioAsync();
        var segundo = await entorno.Automatico.CrearDiarioAsync();

        Assert.True(primero.Correcta, primero.Mensaje);
        Assert.NotNull(primero.Valor);
        Assert.True(segundo.Correcta, segundo.Mensaje);
        Assert.Null(segundo.Valor);
        Assert.Single(entorno.RespaldosAutomaticos());
    }

    [Fact]
    public async Task DesactivadoNoCreaRespaldos()
    {
        await using var entorno = await Entorno.CrearAsync();

        var guardado = entorno.Configuracion.Guardar(false, 30);
        Assert.True(guardado.Correcta, guardado.Mensaje);

        var diario = await entorno.Automatico.CrearDiarioAsync();
        var cierre = await entorno.Automatico.CrearAlCerrarCajaAsync();

        Assert.True(diario.Correcta, diario.Mensaje);
        Assert.True(cierre.Correcta, cierre.Mensaje);
        Assert.Null(diario.Valor);
        Assert.Null(cierre.Valor);
        Assert.Empty(entorno.RespaldosAutomaticos());
    }

    [Fact]
    public async Task RetencionPorDiasEliminaLosAntiguos()
    {
        await using var entorno = await Entorno.CrearAsync();

        var guardado = entorno.Configuracion.Guardar(true, 2);
        Assert.True(guardado.Correcta, guardado.Mensaje);

        Directory.CreateDirectory(entorno.CarpetaRespaldos);

        var manual = Path.Combine(
            entorno.CarpetaRespaldos, "respaldo_20250101_120000_manual.db");
        await File.WriteAllTextAsync(manual, "respaldo manual");
        File.SetLastWriteTime(manual, DateTime.Today.AddDays(-10).AddHours(12));

        for (var i = 1; i <= 4; i++)
        {
            var ruta = Path.Combine(
                entorno.CarpetaRespaldos,
                $"respaldo_auto_2026010{i}_120000_{i}.db");

            await File.WriteAllTextAsync(ruta, "respaldo anterior");
            File.SetLastWriteTime(
                ruta, DateTime.Today.AddDays(-i).AddHours(12));
        }

        var resultado = await entorno.Automatico.CrearAlCerrarCajaAsync();

        Assert.True(resultado.Correcta, resultado.Mensaje);

        var restantes = entorno.RespaldosAutomaticos();

        Assert.Equal(2, restantes.Length);
        Assert.Contains(resultado.Valor, restantes);
        Assert.True(File.Exists(manual));
    }
}
