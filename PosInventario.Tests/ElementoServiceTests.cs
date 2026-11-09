using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Service;

namespace PosInventario.Tests;

public class ElementoServiceTests
{
    private static async Task<AppDbContext> CrearContextoAsync(
        SqliteConnection connection)
    {
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options);

        await db.Database.MigrateAsync();
        return db;
    }

    [Fact]
    public async Task AgregarAsyncCreaElementoValido()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var db = await CrearContextoAsync(connection);

        var categoriaService = new CategoriaService(db);
        var elementoService = new ElementoService(db);

        var categoria = await categoriaService.AgregarAsync("Categoría de prueba");
        Assert.True(categoria.Correcta, categoria.Mensaje);

        var resultado = await elementoService.AgregarAsync(
            "Elemento de prueba", "Descripción", categoria.Valor!.Id);

        Assert.True(resultado.Correcta, resultado.Mensaje);
        Assert.Single(await elementoService.ObtenerTodosAsync());
    }

    [Fact]
    public async Task AgregarAsyncRechazaNombreVacio()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var db = await CrearContextoAsync(connection);

        var service = new ElementoService(db);
        var resultado = await service.AgregarAsync("", null, 1);

        Assert.False(resultado.Correcta);
    }

    [Fact]
    public async Task ArchivarAsyncOcultaElElementoDeLosActivos()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var db = await CrearContextoAsync(connection);

        var categoriaService = new CategoriaService(db);
        var elementoService = new ElementoService(db);

        var categoria = await categoriaService.AgregarAsync("Categoría de prueba");
        var creado = await elementoService.AgregarAsync(
            "Elemento", null, categoria.Valor!.Id);

        var archivado =
            await elementoService.ArchivarAsync(creado.Valor!.Id);

        Assert.True(archivado.Correcta, archivado.Mensaje);
        Assert.Empty(await elementoService.ObtenerTodosAsync());
        Assert.Single(await elementoService.ObtenerArchivadosAsync());
    }
}
