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
            .Include(v => v.Pagos)
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
            .Include(v => v.Pagos)
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
            .Include(v => v.Pagos)
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
            .Include(v => v.Pagos)
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
        IReadOnlyList<LineaPago> pagos,
        decimal descuento = 0m)
    {
        if (!_sesion.Iniciada)
            return new Resultado<Venta>(false, "La sesión no está iniciada.", null);

        if (lineas is null || lineas.Count == 0)
            return new Resultado<Venta>(false, "Agrega al menos un producto.", null);

        if (pagos is null)
            return new Resultado<Venta>(false, "Agrega al menos un método de pago.", null);

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

        var pagosNormalizados = NormalizarPagos(pagos, total, out var errorPagos);

        if (errorPagos is not null)
            return new Resultado<Venta>(false, errorPagos, null);

        var efectivoAplicado = pagosNormalizados
            .Where(p => p.Metodo == MetodoPago.Efectivo)
            .Sum(p => p.Monto);

        var efectivoRecibido = pagosNormalizados
            .Where(p => p.Metodo == MetodoPago.Efectivo)
            .Sum(p => p.MontoRecibido ?? 0m);

        var vuelto = efectivoRecibido - efectivoAplicado;
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
            MetodoPago = pagosNormalizados.Count switch
            {
                0 => MetodoPago.Efectivo,
                1 => pagosNormalizados[0].Metodo,
                _ => MetodoPago.Mixto
            },
            MontoRecibido = total + vuelto,
            Vuelto = vuelto,
            Estado = EstadoVenta.Completada
        };

        foreach (var pago in pagosNormalizados)
        {
            // El efectivo con monto 0 se usa solo para calcular el cambio
            // cuando tarjeta/transferencia ya cubrieron el total.
            if (pago.Monto <= 0)
                continue;

            venta.Pagos.Add(new PagoVenta
            {
                Metodo = pago.Metodo,
                Monto = pago.Monto,
                MontoDevuelto = 0m
            });
        }

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
        if (efectivoAplicado > 0)
        {
            venta.MovimientosCaja.Add(new MovimientoCaja
            {
                IdCaja = caja.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimientoCaja.Venta,
                Monto = efectivoAplicado,
                Descripcion = "Venta",
                Fecha = momento,
                Venta = venta
            });
        }

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();

        return new Resultado<Venta>(true, "Venta registrada.", venta);
    }

    /// <summary>
    /// Valida y completa los pagos: una fila por método, montos positivos,
    /// suma exacta al total y efectivo recibido suficiente. Una venta sin
    /// costo (descuento del 100%) no necesita filas de pago.
    /// </summary>
    private static List<LineaPago> NormalizarPagos(
        IReadOnlyList<LineaPago> pagos, decimal total, out string? error)
    {
        error = null;
        var normalizados = new List<LineaPago>();

        if (total == 0)
            return normalizados;

        if (pagos.Count == 0)
        {
            error = "Agrega al menos un método de pago.";
            return normalizados;
        }

        if (pagos.Any(p => p.Metodo == MetodoPago.Mixto))
        {
            error = "El método Mixto no es un pago válido.";
            return normalizados;
        }

        if (pagos.Select(p => p.Metodo).Distinct().Count() != pagos.Count)
        {
            error = "No se puede repetir el mismo método de pago.";
            return normalizados;
        }

        foreach (var pago in pagos)
        {
            if (pago.Monto < 0)
            {
                error = "El monto de cada pago no puede ser negativo.";
                return normalizados;
            }

            if (pago.Metodo == MetodoPago.Efectivo)
            {
                var recibido = pago.MontoRecibido ?? pago.Monto;

                if (pago.Monto == 0 && recibido <= 0)
                {
                    error = "El monto de cada pago debe ser mayor que 0.";
                    return normalizados;
                }

                if (recibido < pago.Monto)
                {
                    error = "El efectivo recibido es menor al monto en efectivo.";
                    return normalizados;
                }

                normalizados.Add(pago with { MontoRecibido = recibido });
            }
            else
            {
                if (pago.Monto == 0)
                {
                    error = "El monto de cada pago debe ser mayor que 0.";
                    return normalizados;
                }

                normalizados.Add(pago with { MontoRecibido = null });
            }
        }

        if (normalizados.Sum(p => p.Monto) != total)
        {
            error = "La suma de los pagos debe ser igual al total de la venta.";
            return normalizados;
        }

        return normalizados;
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
            .Include(v => v.Pagos)
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

        // Compatibilidad: una venta anterior a la migración de pagos podría
        // no tener filas. Se crea una con su método y saldo original.
        if (venta.Pagos.Count == 0 && venta.Total > 0)
        {
            var metodo = venta.MetodoPago == MetodoPago.Mixto
                ? MetodoPago.Efectivo
                : venta.MetodoPago;

            venta.Pagos.Add(new PagoVenta
            {
                Metodo = metodo,
                Monto = venta.Total,
                MontoDevuelto = venta.TotalDevuelto
            });
        }

        var reembolso = CalcularReembolso(venta, lineas);
        var reembolsos = RepartirReembolso(venta.Pagos.ToList(), reembolso);

        if (reembolsos.Sum(r => r.Monto) != reembolso)
            return new Resultado(false,
                "No se pudo distribuir el reembolso entre los métodos de pago.");

        var reembolsoEfectivo = reembolsos
            .Where(r => r.Pago.Metodo == MetodoPago.Efectivo)
            .Sum(r => r.Monto);

        // El efectivo solo puede salir de una caja abierta: la original si
        // aún está abierta o, si ya se cerró, la abierta actual. Si la venta
        // fue 100% electrónica, no se necesita caja.
        Caja? cajaReembolso = null;

        if (reembolsoEfectivo > 0)
        {
            if (venta.Caja is not null && venta.Caja.Estado == EstadoCaja.Abierta)
            {
                cajaReembolso = venta.Caja;
            }
            else
            {
                cajaReembolso = await _db.Cajas
                    .FirstOrDefaultAsync(c => c.Estado == EstadoCaja.Abierta);

                if (cajaReembolso is null)
                    return new Resultado(false,
                        "No hay una caja abierta para reembolsar el efectivo. " +
                        "Abre la caja e inténtalo de nuevo.");
            }
        }

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

        foreach (var reembolsoPago in reembolsos)
            reembolsoPago.Pago.MontoDevuelto += reembolsoPago.Monto;

        venta.TotalDevuelto += reembolso;

        var quedanPendientes = venta.Detalles.Any(d => d.CantidadPendiente > 0);

        if (!quedanPendientes)
        {
            venta.Estado = EstadoVenta.Anulada;
            venta.FechaAnulacion = momento;
            venta.IdUsuarioAnulacion = _sesion.IdUsuario;
        }

        // Solo la parte en efectivo sale físicamente de la caja.
        if (reembolsoEfectivo > 0 && cajaReembolso is not null)
        {
            _db.MovimientosCaja.Add(new MovimientoCaja
            {
                IdCaja = cajaReembolso.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimientoCaja.AnulacionVenta,
                Monto = reembolsoEfectivo,
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

    /// <summary>
    /// Reparte el reembolso entre los pagos originales de la venta. Se usan
    /// los saldos pendientes de cada pago para que las devoluciones parciales
    /// sucesivas no se desfasen por redondeo.
    /// </summary>
    public IReadOnlyList<ReembolsoPago> CalcularReembolsoPorPago(
        Venta venta, IReadOnlyList<LineaDevolucion>? lineas)
    {
        var reembolso = CalcularReembolso(venta, lineas);

        return RepartirReembolso(PagosParaReembolso(venta), reembolso);
    }

    private static IReadOnlyList<PagoVenta> PagosParaReembolso(Venta venta)
    {
        if (venta.Pagos.Count > 0)
            return venta.Pagos.ToList();

        if (venta.Total <= 0)
            return [];

        var metodo = venta.MetodoPago == MetodoPago.Mixto
            ? MetodoPago.Efectivo
            : venta.MetodoPago;

        return
        [
            new PagoVenta
            {
                Metodo = metodo,
                Monto = venta.Total,
                MontoDevuelto = venta.TotalDevuelto
            }
        ];
    }

    private static List<ReembolsoPago> RepartirReembolso(
        IReadOnlyList<PagoVenta> pagos, decimal reembolso)
    {
        var resultado = new List<ReembolsoPago>();

        if (reembolso <= 0)
            return resultado;

        var pendientes = pagos
            .Select(p => new { Pago = p, Pendiente = p.Monto - p.MontoDevuelto })
            .Where(x => x.Pendiente > 0)
            .ToList();

        var totalPendiente = pendientes.Sum(x => x.Pendiente);

        if (totalPendiente <= 0)
            return resultado;

        // Al devolver todo lo pendiente se usan los saldos exactos.
        if (reembolso >= totalPendiente)
        {
            foreach (var pendiente in pendientes)
                resultado.Add(new ReembolsoPago(pendiente.Pago, pendiente.Pendiente));

            return resultado;
        }

        var asignado = 0m;

        for (var i = 0; i < pendientes.Count; i++)
        {
            var pendiente = pendientes[i];
            var monto = i == pendientes.Count - 1
                ? reembolso - asignado
                : Math.Round(reembolso * pendiente.Pendiente / totalPendiente, 2);

            monto = Math.Clamp(monto, 0m, pendiente.Pendiente);

            if (monto <= 0)
                continue;

            resultado.Add(new ReembolsoPago(pendiente.Pago, monto));
            asignado += monto;
        }

        // Los centavos que deje el redondeo los absorbe el último pago.
        var diferencia = reembolso - resultado.Sum(r => r.Monto);

        for (var i = resultado.Count - 1; i >= 0 && diferencia != 0; i--)
        {
            var actual = resultado[i];
            var maximo = actual.Pago.Monto - actual.Pago.MontoDevuelto;
            var ajuste = Math.Clamp(
                diferencia, -actual.Monto, maximo - actual.Monto);

            if (ajuste == 0)
                continue;

            resultado[i] = actual with { Monto = actual.Monto + ajuste };
            diferencia -= ajuste;
        }

        return resultado;
    }
}
