using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

public class CompraService
{
    private const int MaxComprobante = 40;
    private const int LimiteHistorial = 100;

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public CompraService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<List<Compra>> ObtenerRecientesAsync(int limite = LimiteHistorial)
    {
        if (limite <= 0)
            limite = LimiteHistorial;

        return await _db.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Compras entre dos fechas (inclusive), sin límite de filas, para el
    /// historial filtrado y la exportación a CSV.
    /// </summary>
    public async Task<List<Compra>> ObtenerPorRangoAsync(
        DateTime desde, DateTime hasta)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        if (fin <= inicio)
            fin = inicio.AddDays(1);

        return await _db.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .Where(c => c.Fecha >= inicio && c.Fecha < fin)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Compras paginadas entre dos fechas (inclusive). Devuelve solo las filas
    /// de la página solicitada y el total de coincidencias.
    /// </summary>
    public async Task<PaginaResultado<Compra>> ObtenerPorRangoPaginadoAsync(
        DateTime desde, DateTime hasta, int pagina, int tamanoPagina)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        if (fin <= inicio)
            fin = inicio.AddDays(1);

        if (pagina <= 0)
            pagina = 1;

        if (tamanoPagina <= 0)
            tamanoPagina = 10;

        var query = _db.Compras
            .AsNoTracking()
            .Where(c => c.Fecha >= inicio && c.Fecha < fin);

        var totalRegistros = await query.CountAsync();
        var totalPaginas = Math.Max(
            1, (int)Math.Ceiling(totalRegistros / (double)tamanoPagina));

        if (pagina > totalPaginas)
            pagina = totalPaginas;

        var total = await query.SumAsync(c => (decimal?)c.Total) ?? 0m;

        var items = await query
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new PaginaResultado<Compra>(items, totalRegistros, total);
    }

    public async Task<Compra?> ObtenerPorIdAsync(int id)
        => await _db.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Usuario)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Resultado<Compra>> RegistrarAsync(
        int idProveedor,
        string? numeroComprobante,
        IReadOnlyList<LineaCompra> lineas)
    {
        if (!_sesion.EsAdministrador)
            return new Resultado<Compra>(
                false, "Solo un administrador puede registrar compras.", null);

        if (lineas is null || lineas.Count == 0)
            return new Resultado<Compra>(
                false, "Agrega al menos un producto a la compra.", null);

        var comprobanteNormalizado = NormalizarOpcional(numeroComprobante);

        if (comprobanteNormalizado?.Length > MaxComprobante)
            return new Resultado<Compra>(
                false, $"El comprobante no puede superar los {MaxComprobante} caracteres.", null);

        var proveedor = await _db.Proveedores
            .FirstOrDefaultAsync(p => p.Id == idProveedor && p.Activo == 1);

        if (proveedor is null)
            return new Resultado<Compra>(
                false, "El proveedor no existe o está inactivo.", null);

        if (lineas.Select(l => l.IdProducto).Distinct().Count() != lineas.Count)
            return new Resultado<Compra>(
                false, "Hay productos repetidos en la compra.", null);

        foreach (var linea in lineas)
        {
            if (linea.Cantidad <= 0)
                return new Resultado<Compra>(
                    false, "La cantidad debe ser mayor que 0.", null);

            if (linea.CostoUnitario < 0)
                return new Resultado<Compra>(
                    false, "El costo no puede ser negativo.", null);
        }

        var idsProductos = lineas.Select(l => l.IdProducto).ToList();

        var productos = await _db.Productos
            .Where(p => idsProductos.Contains(p.Id))
            .ToListAsync();

        if (productos.Count != idsProductos.Count)
            return new Resultado<Compra>(
                false, "Alguno de los productos no existe.", null);

        if (productos.Any(p => p.FechaArchivado is not null))
            return new Resultado<Compra>(
                false, "No se pueden comprar productos archivados.", null);

        foreach (var linea in lineas)
        {
            var producto = productos.First(p => p.Id == linea.IdProducto);

            if (Cantidades.TieneDemasiadosDecimales(linea.Cantidad))
                return new Resultado<Compra>(
                    false, "La cantidad no puede tener más de 3 decimales.", null);

            if (producto.UnidadMedida == UnidadMedida.Pieza &&
                !Cantidades.EsEntera(linea.Cantidad))
            {
                return new Resultado<Compra>(
                    false,
                    $"Para {producto.Nombre}, la cantidad debe ser un número entero.",
                    null);
            }
        }

        var compra = new Compra
        {
            IdProveedor = proveedor.Id,
            IdUsuario = _sesion.IdUsuario,
            Fecha = DateTime.Now,
            NumeroComprobante = comprobanteNormalizado,
            Estado = EstadoCompra.Completada
        };

        var total = 0m;
        var referencia = comprobanteNormalizado ?? "Compra";

        foreach (var linea in lineas)
        {
            var producto = productos.First(p => p.Id == linea.IdProducto);
            var cantidad = Cantidades.Normalizar(linea.Cantidad);
            var subtotal = Math.Round(
                cantidad * linea.CostoUnitario,
                2,
                MidpointRounding.AwayFromZero);
            total += subtotal;

            var stockAnterior = producto.Stock;

            producto.Stock += cantidad;
            producto.Costo = linea.CostoUnitario;
            producto.FechaModificacion = compra.Fecha;

            compra.Detalles.Add(new DetalleCompra
            {
                IdProducto = producto.Id,
                Cantidad = cantidad,
                CostoUnitario = linea.CostoUnitario,
                Subtotal = subtotal
            });

            compra.Movimientos.Add(new MovimientoInventario
            {
                IdProducto = producto.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimiento.Compra,
                Cantidad = cantidad,
                StockAnterior = stockAnterior,
                StockResultante = producto.Stock,
                Referencia = referencia,
                Fecha = compra.Fecha,
                Compra = compra
            });
        }

        compra.Total = total;

        _db.Compras.Add(compra);
        await _db.SaveChangesAsync();

        return new Resultado<Compra>(true, "Compra registrada.", compra);
    }

    public async Task<Resultado> AnularAsync(int id)
    {
        if (!_sesion.EsAdministrador)
            return new Resultado(false, "Solo un administrador puede anular compras.");

        var compra = await _db.Compras
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compra is null)
            return new Resultado(false, "La compra no existe.");

        if (compra.Estado == EstadoCompra.Anulada)
            return new Resultado(false, "La compra ya está anulada.");

        if (compra.Detalles.Count == 0)
            return new Resultado(false, "La compra no tiene líneas.");

        foreach (var detalle in compra.Detalles)
        {
            if (detalle.Producto is null)
                return new Resultado(false, "Uno de los productos de la compra no existe.");

            if (detalle.Producto.Stock < detalle.Cantidad)
                return new Resultado(
                    false,
                    $"No hay stock suficiente de {detalle.Producto.Nombre} para anular la compra.");
        }

        var momento = DateTime.Now;

        foreach (var detalle in compra.Detalles)
        {
            var producto = detalle.Producto;
            var stockAnterior = producto.Stock;

            producto.Stock -= detalle.Cantidad;
            producto.FechaModificacion = momento;

            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                IdProducto = producto.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimiento.AnulacionCompra,
                Cantidad = detalle.Cantidad,
                StockAnterior = stockAnterior,
                StockResultante = producto.Stock,
                Referencia = $"Anulación compra C-{compra.Id:000000}",
                IdCompra = compra.Id,
                Fecha = momento
            });
        }

        compra.Estado = EstadoCompra.Anulada;
        compra.FechaAnulacion = momento;
        compra.IdUsuarioAnulacion = _sesion.IdUsuario;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Compra anulada.");
    }

    private static string? NormalizarOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
