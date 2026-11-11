using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;

namespace PosInventario.Service;

public class ProductoService
{
    private const int MinNombre = 3;
    private const int MaxNombre = 100;
    private const int MaxDescripcion = 500;
    private const int MaxSku = 40;

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public ProductoService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<List<Producto>> ObtenerTodosAsync()
        => await _db.Productos
            .AsNoTracking()
            .Where(p => p.FechaArchivado == null)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

    public async Task<List<Producto>> ObtenerArchivadosAsync()
        => await _db.Productos
            .AsNoTracking()
            .Where(p => p.FechaArchivado != null)
            .OrderByDescending(p => p.FechaArchivado)
            .ToListAsync();

    /// <summary>
    /// Productos vigentes cuyo stock llegó al mínimo configurado.
    /// Un mínimo en 0 significa que el producto no genera alerta.
    /// </summary>
    public async Task<List<Producto>> ObtenerStockBajoAsync()
        => await _db.Productos
            .AsNoTracking()
            .Where(p => p.FechaArchivado == null &&
                        p.StockMinimo > 0 &&
                        p.Stock <= p.StockMinimo)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Nombre)
            .ToListAsync();

    public async Task<Producto?> ObtenerPorIdAsync(int id)
        => await _db.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

    /// <summary>
    /// Busca un producto vigente por su SKU exacto (sin distinguir mayúsculas).
    /// Se usa para agregar productos al POS con un lector de código de barras.
    /// </summary>
    public async Task<Producto?> ObtenerPorSkuAsync(string sku)
    {
        var normalizado = (sku ?? string.Empty).Trim().ToLowerInvariant();

        if (normalizado.Length == 0)
            return null;

        return await _db.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.FechaArchivado == null && p.Sku.ToLower() == normalizado);
    }

    public async Task<Resultado<Producto>> AgregarAsync(
        string sku, string nombre, string? descripcion,
        decimal precioVenta, decimal costo, int stockMinimo)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado<Producto>(false, autorizacion.Mensaje, null);

        var error = ValidarCampos(sku, nombre, descripcion, precioVenta, costo, stockMinimo);
        if (error is not null)
            return new Resultado<Producto>(false, error, null);

        var skuNormalizado = NormalizarSku(sku);

        if (await _db.Productos.AnyAsync(p => p.Sku == skuNormalizado))
            return new Resultado<Producto>(
                false, "Ya existe un producto con ese SKU.", null);

        var producto = new Producto
        {
            Sku = skuNormalizado,
            Nombre = nombre.Trim(),
            Descripcion = NormalizarOpcional(descripcion),
            PrecioVenta = precioVenta,
            Costo = costo,
            Stock = 0,
            StockMinimo = stockMinimo,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        };

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();

        return new Resultado<Producto>(true, "Producto creado.", producto);
    }

    public async Task<Resultado> ActualizarAsync(
        int id, string sku, string nombre, string? descripcion,
        decimal precioVenta, decimal costo, int stockMinimo)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id);

        if (producto is null)
            return new Resultado(false, "El producto no existe.");

        if (producto.FechaArchivado is not null)
            return new Resultado(false, "Restaura el producto antes de editarlo.");

        var error = ValidarCampos(sku, nombre, descripcion, precioVenta, costo, stockMinimo);
        if (error is not null)
            return new Resultado(false, error);

        var skuNormalizado = NormalizarSku(sku);

        if (await _db.Productos.AnyAsync(p =>
                p.Id != id && p.Sku == skuNormalizado))
            return new Resultado(false, "Ya existe un producto con ese SKU.");

        producto.Sku = skuNormalizado;
        producto.Nombre = nombre.Trim();
        producto.Descripcion = NormalizarOpcional(descripcion);
        producto.PrecioVenta = precioVenta;
        producto.Costo = costo;
        producto.StockMinimo = stockMinimo;
        producto.FechaModificacion = DateTime.Now;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Producto actualizado.");
    }

    public async Task<Resultado> ArchivarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id);

        if (producto is null)
            return new Resultado(false, "El producto no existe.");

        if (producto.FechaArchivado is not null)
            return new Resultado(false, "El producto ya está archivado.");

        if (producto.Stock != 0)
            return new Resultado(
                false, "No se puede archivar un producto con stock. Ajusta el inventario a 0 primero.");

        producto.FechaArchivado = DateOnly.FromDateTime(DateTime.Today);
        producto.FechaModificacion = DateTime.Now;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Producto archivado.");
    }

    public async Task<Resultado> RestaurarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id);

        if (producto is null)
            return new Resultado(false, "El producto no existe.");

        if (producto.FechaArchivado is null)
            return new Resultado(false, "El producto no está archivado.");

        producto.FechaArchivado = null;
        producto.FechaModificacion = DateTime.Now;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Producto restaurado.");
    }

    private Resultado? ValidarAdministrador()
        => _sesion.EsAdministrador
            ? null
            : new Resultado(false, "Solo un administrador puede modificar productos.");

    private static string NormalizarSku(string sku) => sku.Trim().ToUpperInvariant();

    private static string? NormalizarOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? ValidarCampos(
        string? sku, string? nombre, string? descripcion,
        decimal precioVenta, decimal costo, int stockMinimo)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return "El SKU es obligatorio.";

        var skuValor = sku.Trim();

        if (skuValor.Length > MaxSku)
            return $"El SKU no puede superar los {MaxSku} caracteres.";

        if (skuValor.Any(char.IsWhiteSpace))
            return "El SKU no puede contener espacios.";

        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre es obligatorio.";

        var nombreValor = nombre.Trim();

        if (nombreValor.Length < MinNombre)
            return $"El nombre debe tener al menos {MinNombre} caracteres.";

        if (nombreValor.Length > MaxNombre)
            return $"El nombre no puede superar los {MaxNombre} caracteres.";

        if (descripcion?.Trim().Length > MaxDescripcion)
            return $"La descripción no puede superar los {MaxDescripcion} caracteres.";

        if (precioVenta < 0)
            return "El precio de venta no puede ser negativo.";

        if (costo < 0)
            return "El costo no puede ser negativo.";

        if (stockMinimo < 0)
            return "El stock mínimo no puede ser negativo.";

        return null;
    }
}
