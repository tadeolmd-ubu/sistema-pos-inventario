using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;

namespace PosInventario.Service;

public class ElementoService
{
    private const int MaxNombre = 100;
    private const int MaxDescripcion = 500;
    private readonly AppDbContext _db;

    public ElementoService(AppDbContext db) => _db = db;

    public async Task<List<Elemento>> ObtenerTodosAsync()
        => await _db.Elementos
            .AsNoTracking()
            .Include(e => e.Categoria)
            .Where(e => e.FechaArchivado == null)
            .OrderBy(e => e.Nombre)
            .ToListAsync();

    public async Task<List<Elemento>> ObtenerArchivadosAsync()
        => await _db.Elementos
            .AsNoTracking()
            .Include(e => e.Categoria)
            .Where(e => e.FechaArchivado != null)
            .OrderByDescending(e => e.FechaArchivado)
            .ToListAsync();

    public async Task<Elemento?> ObtenerPorIdAsync(int id)
        => await _db.Elementos
            .AsNoTracking()
            .Include(e => e.Categoria)
            .FirstOrDefaultAsync(e => e.Id == id && e.FechaArchivado == null);

    public async Task<Resultado<Elemento>> AgregarAsync(
        string nombre, string? descripcion, int idCategoria)
    {
        var error = ValidarCampos(nombre, descripcion);
        if (error is not null)
            return new Resultado<Elemento>(false, error, null);

        var categoria = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Id == idCategoria && c.Activo == 1);

        if (categoria is null)
            return new Resultado<Elemento>(
                false, "La categoría no existe o está inactiva.", null);

        var nombreNormalizado = nombre.Trim();

        if (await _db.Elementos.AnyAsync(e =>
                e.Nombre == nombreNormalizado && e.FechaArchivado == null))
            return new Resultado<Elemento>(
                false, "Ya existe un elemento con ese nombre.", null);

        var elemento = new Elemento
        {
            Nombre = nombreNormalizado,
            Descripcion = NormalizarOpcional(descripcion),
            IdCategoria = idCategoria,
            Activo = 1,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        };

        _db.Elementos.Add(elemento);
        await _db.SaveChangesAsync();

        return new Resultado<Elemento>(true, "Elemento creado.", elemento);
    }

    public async Task<Resultado> ActualizarAsync(
        int id, string nombre, string? descripcion, int idCategoria)
    {
        var elemento = await _db.Elementos
            .FirstOrDefaultAsync(e => e.Id == id && e.FechaArchivado == null);

        if (elemento is null)
            return new Resultado(false, "El elemento no existe.");

        var error = ValidarCampos(nombre, descripcion);
        if (error is not null)
            return new Resultado(false, error);

        var categoria = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Id == idCategoria && c.Activo == 1);

        if (categoria is null)
            return new Resultado(false, "La categoría no existe o está inactiva.");

        var nombreNormalizado = nombre.Trim();

        if (await _db.Elementos.AnyAsync(e =>
                e.Id != id && e.Nombre == nombreNormalizado &&
                e.FechaArchivado == null))
            return new Resultado(false, "Ya existe un elemento con ese nombre.");

        elemento.Nombre = nombreNormalizado;
        elemento.Descripcion = NormalizarOpcional(descripcion);
        elemento.IdCategoria = idCategoria;
        elemento.FechaModificacion = DateTime.Now;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Elemento actualizado.");
    }

    public async Task<Resultado> ArchivarAsync(int id)
    {
        var elemento = await _db.Elementos
            .FirstOrDefaultAsync(e => e.Id == id && e.FechaArchivado == null);

        if (elemento is null)
            return new Resultado(false, "El elemento no existe.");

        elemento.FechaArchivado = DateOnly.FromDateTime(DateTime.Today);
        elemento.FechaModificacion = DateTime.Now;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Elemento archivado.");
    }

    public async Task<Resultado> RestaurarAsync(int id)
    {
        var elemento = await _db.Elementos
            .FirstOrDefaultAsync(e => e.Id == id && e.FechaArchivado != null);

        if (elemento is null)
            return new Resultado(false, "El elemento no está archivado.");

        elemento.FechaArchivado = null;
        elemento.FechaModificacion = DateTime.Now;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Elemento restaurado.");
    }

    private static string? ValidarCampos(string nombre, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre es obligatorio.";

        if (nombre.Trim().Length > MaxNombre)
            return $"El nombre no puede superar los {MaxNombre} caracteres.";

        if (descripcion?.Trim().Length > MaxDescripcion)
            return $"La descripción no puede superar los {MaxDescripcion} caracteres.";

        return null;
    }

    private static string? NormalizarOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
