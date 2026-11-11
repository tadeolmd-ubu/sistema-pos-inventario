using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

public class VentaService
{
    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public VentaService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<List<Venta>> ObtenerPorCajaAsync(int idCaja)
        => await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Where(v => v.IdCaja == idCaja)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .ToListAsync();

    public async Task<List<Venta>> ObtenerDelDiaAsync()
    {
        var desde = DateTime.Today;
        var hasta = desde.AddDays(1);

        return await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Where(v => v.Estado == EstadoVenta.Completada &&
                        v.Fecha >= desde && v.Fecha < hasta)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .ToListAsync();
    }

    public async Task<List<Venta>> ObtenerRecientesAsync(int limite = 10)
    {
        if (limite <= 0)
            limite = 10;

        return await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .Take(limite)
            .ToListAsync();
    }

    /// <summary>
    /// Historial de ventas entre dos fechas (inclusive), con el cajero y el
    /// detalle necesarios para ver el ticket. Incluye ventas anuladas.
    /// </summary>
    public async Task<List<Venta>> ObtenerHistorialAsync(
        DateTime desde, DateTime hasta, int limite = 500)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        if (fin <= inicio)
            fin = inicio.AddDays(1);

        if (limite <= 0)
            limite = 500;

        return await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Where(v => v.Fecha >= inicio && v.Fecha < fin)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<Resultado<Venta>> RegistrarAsync(
        IReadOnlyList<LineaVenta> lineas,
        MetodoPago metodoPago,
        decimal montoRecibido,
        decimal descuento = 0m)
    {
        if (!_sesion.Iniciada)
            return new Resultado<Venta>(false, "La sesión no está iniciada.", null);

        if (lineas is null || lineas.Count == 0)
            return new Resultado<Venta>(false, "Agrega al menos un producto.", null);

        var caja = await _db.Cajas
            .FirstOrDefaultAsync(c => c.Estado == EstadoCaja.Abierta);

        if (caja is null)
            return new Resultado<Venta>(
                false, "No hay una caja abierta para cobrar.", null);

        if (lineas.Select(l => l.IdProducto).Distinct().Count() != lineas.Count)
            return new Resultado<Venta>(
                false, "Hay productos repetidos en la venta.", null);

        foreach (var linea in lineas)
        {
            if (linea.Cantidad <= 0)
                return new Resultado<Venta>(
                    false, "La cantidad debe ser mayor que 0.", null);
        }

        var idsProductos = lineas.Select(l => l.IdProducto).ToList();

        var productos = await _db.Productos
            .Where(p => idsProductos.Contains(p.Id))
            .ToListAsync();

        if (productos.Count != idsProductos.Count)
            return new Resultado<Venta>(
                false, "Alguno de los productos no existe.", null);

        if (productos.Any(p => p.FechaArchivado is not null))
            return new Resultado<Venta>(
                false, "No se pueden vender productos archivados.", null);

        var subtotal = 0m;

        foreach (var linea in lineas)
        {
            var producto = productos.First(p => p.Id == linea.IdProducto);

            if (producto.Stock < linea.Cantidad)
                return new Resultado<Venta>(
                    false, $"No hay stock suficiente de {producto.Nombre}.", null);

            subtotal += producto.PrecioVenta * linea.Cantidad;
        }

        if (descuento < 0)
            return new Resultado<Venta>(
                false, "El descuento no puede ser negativo.", null);

        if (descuento > subtotal)
            return new Resultado<Venta>(
                false, "El descuento no puede superar el subtotal.", null);

        var total = subtotal - descuento;

        decimal vuelto;

        if (metodoPago == MetodoPago.Efectivo)
        {
            if (montoRecibido < total)
                return new Resultado<Venta>(
                    false, "El monto recibido es menor al total de la venta.", null);

            vuelto = montoRecibido - total;
        }
        else
        {
            montoRecibido = total;
            vuelto = 0m;
        }

        var momento = DateTime.Now;

        var venta = new Venta
        {
            IdUsuario = _sesion.IdUsuario,
            Usuario = _sesion.Usuario!,
            IdCaja = caja.Id,
            Fecha = momento,
            Subtotal = subtotal,
            Descuento = descuento,
            Total = total,
            MetodoPago = metodoPago,
            MontoRecibido = montoRecibido,
            Vuelto = vuelto,
            Estado = EstadoVenta.Completada
        };

        foreach (var linea in lineas)
        {
            var producto = productos.First(p => p.Id == linea.IdProducto);
            var precioUnitario = producto.PrecioVenta;
            var subtotalLinea = precioUnitario * linea.Cantidad;

            var stockAnterior = producto.Stock;
            producto.Stock -= linea.Cantidad;
            producto.FechaModificacion = momento;

            venta.Detalles.Add(new DetalleVenta
            {
                IdProducto = producto.Id,
                Producto = producto,
                Cantidad = linea.Cantidad,
                PrecioUnitario = precioUnitario,
                Subtotal = subtotalLinea
            });

            venta.Movimientos.Add(new MovimientoInventario
            {
                IdProducto = producto.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimiento.Venta,
                Cantidad = linea.Cantidad,
                StockAnterior = stockAnterior,
                StockResultante = producto.Stock,
                Referencia = "Venta",
                Fecha = momento,
                Venta = venta
            });
        }

        // Solo el efectivo entra físicamente a la caja.
        if (metodoPago == MetodoPago.Efectivo && total > 0)
        {
            venta.MovimientosCaja.Add(new MovimientoCaja
            {
                IdCaja = caja.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimientoCaja.Venta,
                Monto = total,
                Descripcion = "Venta",
                Fecha = momento,
                Venta = venta
            });
        }

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();

        var mensaje = "Venta registrada.";

        return new Resultado<Venta>(true, mensaje, venta);
    }

    /// <summary>
    /// Anula la venta devolviendo todas las unidades pendientes: es el caso
    /// especial de una devolución total.
    /// </summary>
    public async Task<Resultado> AnularAsync(int idVenta)
    {
        var resultado = await DevolverAsync(idVenta, lineas: null);

        return resultado.Correcta
            ? new Resultado(true, "Venta anulada.")
            : resultado;
    }

    /// <summary>
    /// Registra una devolución total o parcial de una venta: regresa el stock
    /// de las líneas devueltas y acumula lo devuelto por línea y por venta.
    /// El reembolso en efectivo sale de la caja original si sigue abierta o,
    /// si ya se cerró, de la caja abierta actual sin tocar los montos del
    /// cierre anterior. Sin líneas se devuelven todas las unidades pendientes.
    /// </summary>
    public async Task<Resultado> DevolverAsync(
        int idVenta, IReadOnlyList<LineaDevolucion>? lineas)
    {
        if (!_sesion.Iniciada)
            return new Resultado(false, "La sesión no está iniciada.");

        var venta = await _db.Ventas
            .Include(v => v.Caja)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == idVenta);

        if (venta is null)
            return new Resultado(false, "La venta no existe.");

        if (venta.Estado == EstadoVenta.Anulada)
            return new Resultado(false, "La venta ya está anulada.");

        if (!_sesion.EsAdministrador && venta.IdUsuario != _sesion.IdUsuario)
            return new Resultado(
                false, "Solo quien realizó la venta o un administrador puede hacer la devolución.");

        foreach (var detalle in venta.Detalles)
        {
            if (detalle.Producto is null)
                return new Resultado(false, "Uno de los productos de la venta no existe.");
        }

        // Sin líneas se devuelven todas las unidades pendientes (anulación).
        Dictionary<int, int> cantidades;

        if (lineas is null)
        {
            cantidades = venta.Detalles
                .Where(d => d.CantidadPendiente > 0)
                .ToDictionary(d => d.Id, d => d.CantidadPendiente);

            if (cantidades.Count == 0)
                return new Resultado(false, "La venta ya está completamente devuelta.");
        }
        else
        {
            if (lineas.Count == 0)
                return new Resultado(false, "Selecciona al menos un producto.");

            if (lineas.Select(l => l.IdDetalleVenta).Distinct().Count() != lineas.Count)
                return new Resultado(false, "Hay productos repetidos en la devolución.");

            cantidades = [];

            foreach (var linea in lineas)
            {
                var detalle = venta.Detalles
                    .FirstOrDefault(d => d.Id == linea.IdDetalleVenta);

                if (detalle is null)
                    return new Resultado(
                        false, "Uno de los productos no pertenece a la venta.");

                if (linea.Cantidad <= 0)
                    return new Resultado(
                        false, "La cantidad a devolver debe ser mayor que 0.");

                if (linea.Cantidad > detalle.CantidadPendiente)
                    return new Resultado(false,
                        $"No se pueden devolver más unidades de {detalle.Producto!.Nombre} de las vendidas.");

                cantidades[detalle.Id] = linea.Cantidad;
            }
        }

        // El efectivo solo puede salir de una caja abierta: la original si
        // aún está abierta o, si ya se cerró, la abierta actual.
        Caja? cajaReembolso;

        if (venta.Caja is not null && venta.Caja.Estado == EstadoCaja.Abierta)
        {
            cajaReembolso = venta.Caja;
        }
        else
        {
            cajaReembolso = await _db.Cajas
                .FirstOrDefaultAsync(c => c.Estado == EstadoCaja.Abierta);

            if (cajaReembolso is null && venta.MetodoPago == MetodoPago.Efectivo)
                return new Resultado(false,
                    "No hay una caja abierta para reembolsar el efectivo. " +
                    "Abre la caja e inténtalo de nuevo.");
        }

        var reembolso = CalcularReembolso(venta, lineas);
        var momento = DateTime.Now;

        foreach (var (idDetalle, cantidad) in cantidades)
        {
            var detalle = venta.Detalles.First(d => d.Id == idDetalle);
            var producto = detalle.Producto!;
            var stockAnterior = producto.Stock;

            producto.Stock += cantidad;
            producto.FechaModificacion = momento;

            detalle.CantidadDevuelta += cantidad;

            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                IdProducto = producto.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimiento.AnulacionVenta,
                Cantidad = cantidad,
                StockAnterior = stockAnterior,
                StockResultante = producto.Stock,
                Referencia = $"Devolución venta V-{venta.Id:000000}",
                IdVenta = venta.Id,
                Fecha = momento
            });
        }

        venta.TotalDevuelto += reembolso;

        var quedanPendientes = venta.Detalles.Any(d => d.CantidadPendiente > 0);

        if (!quedanPendientes)
        {
            venta.Estado = EstadoVenta.Anulada;
            venta.FechaAnulacion = momento;
            venta.IdUsuarioAnulacion = _sesion.IdUsuario;
        }

        // Solo el efectivo sale físicamente de la caja.
        if (venta.MetodoPago == MetodoPago.Efectivo && reembolso > 0 && cajaReembolso is not null)
        {
            _db.MovimientosCaja.Add(new MovimientoCaja
            {
                IdCaja = cajaReembolso.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimientoCaja.AnulacionVenta,
                Monto = reembolso,
                Descripcion = $"Devolución venta V-{venta.Id:000000}",
                IdVenta = venta.Id,
                Fecha = momento
            });
        }

        await _db.SaveChangesAsync();

        return new Resultado(true, quedanPendientes
            ? "Devolución registrada."
            : "Devolución registrada. La venta ya no tiene nada pendiente.");
    }

    /// <summary>
    /// Cuánto dinero se reembolsa al devolver las líneas indicadas. El
    /// descuento se prorratea (cada unidad devuelve su parte); si se devuelven
    /// todas las unidades pendientes se reembolsa el saldo exacto para no
    /// arrastrar diferencias de redondeo.
    /// </summary>
    public decimal CalcularReembolso(
        Venta venta, IReadOnlyList<LineaDevolucion>? lineas)
    {
        var totalPendiente = venta.Total - venta.TotalDevuelto;

        if (totalPendiente <= 0)
            return 0;

        var cantidadPendiente = venta.Detalles.Sum(d => d.CantidadPendiente);

        if (cantidadPendiente <= 0)
            return 0;

        var unidadesADevolver = lineas?.Sum(l => l.Cantidad) ?? cantidadPendiente;

        if (unidadesADevolver == cantidadPendiente)
            return totalPendiente;

        var factor = venta.Subtotal > 0 ? venta.Total / venta.Subtotal : 1m;
        var reembolso = 0m;

        foreach (var linea in lineas ?? [])
        {
            var detalle = venta.Detalles
                .FirstOrDefault(d => d.Id == linea.IdDetalleVenta);

            if (detalle is null)
                continue;

            reembolso += Math.Round(
                detalle.PrecioUnitario * linea.Cantidad * factor, 2);
        }

        return Math.Min(reembolso, totalPendiente);
    }
}
