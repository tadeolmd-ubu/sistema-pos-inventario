using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;

namespace PosInventario.Service;

public class CategoriaService
{
    private const int MaxNombre = 100;
    private readonly AppDbContext _db;

    public CategoriaService(AppDbContext db) => _db = db;

    public async Task<List<Categoria>> ObtenerTodosAsync()
        => await _db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();

    public async Task<List<Categoria>> ObtenerActivasAsync()
        => await _db.Categorias
            .AsNoTracking()
            .Where(c => c.Activo == 1)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

    public async Task<Categoria?> ObtenerPorIdAsync(int id)
        => await _db.Categorias
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Resultado<Categoria>> AgregarAsync(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return new Resultado<Categoria>(false, "El nombre es obligatorio.", null);

        if (nombre.Trim().Length > MaxNombre)
            return new Resultado<Categoria>(
                false, $"El nombre no puede superar los {MaxNombre} caracteres.", null);

        var nombreNormalizado = nombre.Trim();

        if (await _db.Categorias.AnyAsync(c => c.Nombre == nombreNormalizado))
            return new Resultado<Categoria>(
                false, "Ya existe una categoría con ese nombre.", null);

        var categoria = new Categoria
        {
            Nombre = nombreNormalizado,
            Activo = 1
        };

        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();

        return new Resultado<Categoria>(true, "Categoría creada.", categoria);
    }

    public async Task<Resultado> ActualizarAsync(int id, string nombre)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null)
            return new Resultado(false, "La categoría no existe.");

        if (string.IsNullOrWhiteSpace(nombre))
            return new Resultado(false, "El nombre es obligatorio.");

        if (nombre.Trim().Length > MaxNombre)
            return new Resultado(
                false, $"El nombre no puede superar los {MaxNombre} caracteres.");

        var nombreNormalizado = nombre.Trim();

        if (await _db.Categorias.AnyAsync(c =>
                c.Id != id && c.Nombre == nombreNormalizado))
            return new Resultado(false, "Ya existe una categoría con ese nombre.");

        categoria.Nombre = nombreNormalizado;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Categoría actualizada.");
    }

    public async Task<Resultado> ActivarAsync(int id)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null)
            return new Resultado(false, "La categoría no existe.");

        categoria.Activo = 1;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Categoría activada.");
    }

    public async Task<Resultado> DesactivarAsync(int id)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id);

        if (categoria is null)
            return new Resultado(false, "La categoría no existe.");

        categoria.Activo = 0;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Categoría desactivada.");
    }
}
