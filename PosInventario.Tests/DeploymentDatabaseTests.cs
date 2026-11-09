using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Service;

namespace PosInventario.Tests;

public class DeploymentDatabaseTests
{
    [Fact]
    public async Task RespaldoDeUnaBaseEnDiscoEsLegible()
    {
        var carpeta = Path.Combine(
            Path.GetTempPath(),
            "PosInventario-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(carpeta);

        try
        {
            var rutaDb = Path.Combine(carpeta, "prueba.db");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={rutaDb}")
                .Options;

            await using (var db = new AppDbContext(options))
            {
                await db.Database.MigrateAsync();

                var categoriaService = new CategoriaService(db);
                var creada = await categoriaService.AgregarAsync("Sucursal");
                Assert.True(creada.Correcta, creada.Mensaje);

                var respaldoService = new RespaldoService(db);
                var respaldo = await respaldoService.CrearRespaldoAsync(carpeta);

                Assert.True(respaldo.Correcta, respaldo.Mensaje);
                Assert.True(File.Exists(respaldo.Valor));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(carpeta, recursive: true);
        }
    }
}
