using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Único punto autorizado para modificar el stock de un producto.
/// Cada cambio deja un MovimientoInventario inmutable.
/// </summary>
public class InventarioService
{
    private const int MaxReferencia = 100;
    private const int LimiteMovimientos = 100;

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public InventarioService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<List<MovimientoInventario>> ObtenerMovimientosAsync(
        int? idProducto = null, int limite = LimiteMovimientos)
    {
        if (limite <= 0)
            limite = LimiteMovimientos;

        var query = _db.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Producto)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (idProducto is not null)
            query = query.Where(m => m.IdProducto == idProducto);

        return await query
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Movimientos entre dos fechas (inclusive), sin límite de filas, para el
    /// historial filtrado y la exportación a CSV.
    /// </summary>
    public async Task<List<MovimientoInventario>> ObtenerMovimientosPorRangoAsync(
        DateTime desde, DateTime hasta, int? idProducto = null)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        if (fin <= inicio)
            fin = inicio.AddDays(1);

        var query = _db.MovimientosInventario
            .AsNoTracking()
            .Include(m => m.Producto)
            .Include(m => m.Usuario)
            .Where(m => m.Fecha >= inicio && m.Fecha < fin);

        if (idProducto is not null)
            query = query.Where(m => m.IdProducto == idProducto);

        return await query
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Movimientos paginados entre dos fechas (inclusive), opcionalmente de un
    /// producto. Devuelve solo las filas de la página y el total de
    /// coincidencias.
    /// </summary>
    public async Task<PaginaResultado<MovimientoInventario>>
        ObtenerMovimientosPorRangoPaginadosAsync(
            DateTime desde,
            DateTime hasta,
            int? idProducto,
            int pagina,
            int tamanoPagina)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        if (fin <= inicio)
            fin = inicio.AddDays(1);

        if (pagina <= 0)
            pagina = 1;

        if (tamanoPagina <= 0)
            tamanoPagina = 10;

        var query = _db.MovimientosInventario
            .AsNoTracking()
            .Where(m => m.Fecha >= inicio && m.Fecha < fin);

        if (idProducto is not null)
            query = query.Where(m => m.IdProducto == idProducto);

        var totalRegistros = await query.CountAsync();
        var totalPaginas = Math.Max(
            1, (int)Math.Ceiling(totalRegistros / (double)tamanoPagina));

        if (pagina > totalPaginas)
            pagina = totalPaginas;

        var items = await query
            .Include(m => m.Producto)
            .Include(m => m.Usuario)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new PaginaResultado<MovimientoInventario>(
            items, totalRegistros);
    }

    public Task<Resultado<MovimientoInventario>> RegistrarEntradaAsync(
        int idProducto, int cantidad, string referencia)
        => RegistrarManualAsync(
            idProducto, TipoMovimiento.Entrada, cantidad, referencia, esAjuste: false);

    public Task<Resultado<MovimientoInventario>> RegistrarSalidaAsync(
        int idProducto, int cantidad, string referencia)
        => RegistrarManualAsync(
            idProducto, TipoMovimiento.Salida, cantidad, referencia, esAjuste: false);

    public Task<Resultado<MovimientoInventario>> RegistrarAjusteAsync(
        int idProducto, int stockContado, string referencia)
        => RegistrarManualAsync(
            idProducto, TipoMovimiento.Ajuste, stockContado, referencia, esAjuste: true);

    private async Task<Resultado<MovimientoInventario>> RegistrarManualAsync(
        int idProducto,
        TipoMovimiento tipo,
        int valor,
        string referencia,
        bool esAjuste)
    {
        if (!_sesion.EsAdministrador)
            return new Resultado<MovimientoInventario>(
                false, "Solo un administrador puede mover el inventario.", null);

        var referenciaNormalizada = (referencia ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(referenciaNormalizada))
            return new Resultado<MovimientoInventario>(
                false, "Selecciona un motivo.", null);

        if (referenciaNormalizada.Length > MaxReferencia)
            return new Resultado<MovimientoInventario>(
                false, $"El motivo no puede superar los {MaxReferencia} caracteres.", null);

        if (!MotivosInventario.Para(tipo).Contains(referenciaNormalizada))
            return new Resultado<MovimientoInventario>(
                false, "Selecciona un motivo válido.", null);

        if (!esAjuste && valor <= 0)
            return new Resultado<MovimientoInventario>(
                false, "La cantidad debe ser mayor que 0.", null);

        if (esAjuste && valor < 0)
            return new Resultado<MovimientoInventario>(
                false, "El stock contado no puede ser negativo.", null);

        var producto = await _db.Productos
            .FirstOrDefaultAsync(p => p.Id == idProducto);

        if (producto is null)
            return new Resultado<MovimientoInventario>(
                false, "El producto no existe.", null);

        if (producto.FechaArchivado is not null)
            return new Resultado<MovimientoInventario>(
                false, "El producto está archivado.", null);

        var stockAnterior = producto.Stock;
        int stockResultante;
        int cantidadMovimiento;

        switch (tipo)
        {
            case TipoMovimiento.Entrada:
                stockResultante = stockAnterior + valor;
                cantidadMovimiento = valor;
                break;

            case TipoMovimiento.Salida:
                if (valor > stockAnterior)
                    return new Resultado<MovimientoInventario>(
                        false, "No hay stock suficiente para esa salida.", null);

                stockResultante = stockAnterior - valor;
                cantidadMovimiento = valor;
                break;

            case TipoMovimiento.Ajuste:
                if (valor == stockAnterior)
                    return new Resultado<MovimientoInventario>(
                        false, "El stock contado coincide con el actual.", null);

                stockResultante = valor;
                cantidadMovimiento = Math.Abs(valor - stockAnterior);
                break;

            default:
                return new Resultado<MovimientoInventario>(
                    false, "Tipo de movimiento no permitido.", null);
        }

        var momento = DateTime.Now;

        producto.Stock = stockResultante;
        producto.FechaModificacion = momento;

        var movimiento = new MovimientoInventario
        {
            IdProducto = producto.Id,
            IdUsuario = _sesion.IdUsuario,
            Tipo = tipo,
            Cantidad = cantidadMovimiento,
            StockAnterior = stockAnterior,
            StockResultante = stockResultante,
            Referencia = referenciaNormalizada,
            Fecha = momento
        };

        _db.MovimientosInventario.Add(movimiento);
        await _db.SaveChangesAsync();

        var mensaje = tipo switch
        {
            TipoMovimiento.Entrada => "Entrada registrada.",
            TipoMovimiento.Salida => "Salida registrada.",
            _ => "Ajuste registrado."
        };

        return new Resultado<MovimientoInventario>(true, mensaje, movimiento);
    }
}
