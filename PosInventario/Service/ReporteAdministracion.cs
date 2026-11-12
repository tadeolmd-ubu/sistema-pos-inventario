using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Forma en la que se agrupan las ventas del reporte. La semana empieza en
/// lunes y el mes usa el nombre en español.
/// </summary>
public enum AgrupacionReporte
{
    Dia,
    Semana,
    Mes
}

/// <summary>
/// Fila de ventas de un día, una semana o un mes. <see cref="Proporcion"/>
/// sirve para dibujar la barra comparativa dentro del reporte.
/// </summary>
public sealed record ReportePeriodo(
    string Etiqueta,
    DateTime Inicio,
    DateTime Fin,
    int CantidadVentas,
    decimal VentasNetas,
    decimal Devoluciones,
    decimal Utilidad,
    decimal Proporcion = 0m);

/// <summary>
/// Ventas cobradas con un método de pago, ya descontadas las devoluciones.
/// </summary>
public sealed record ReporteMetodo(
    MetodoPago Metodo,
    decimal Monto,
    int CantidadVentas,
    decimal Proporcion = 0m)
{
    public string Nombre => Metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transferencia",
        _ => Metodo.ToString()
    };

    public decimal Porcentaje => Proporcion * 100m;
}

/// <summary>
/// Producto dentro del ranking de más vendidos. Las cantidades y montos ya
/// excluyen las unidades devueltas.
/// </summary>
public sealed record ReporteProducto(
    int IdProducto,
    string Sku,
    string Nombre,
    int Cantidad,
    decimal Ventas,
    decimal Costo,
    decimal Utilidad,
    decimal Proporcion = 0m);

/// <summary>
/// Números del período para el dueño: ventas netas, desglose por método,
/// utilidad (venta menos costo) y productos más vendidos.
/// </summary>
public sealed record ReporteAdministracion(
    DateTime Desde,
    DateTime Hasta,
    AgrupacionReporte Agrupacion,
    int CantidadVentas,
    decimal VentasBrutas,
    decimal Devoluciones,
    decimal VentasNetas,
    decimal Costo,
    decimal Utilidad,
    IReadOnlyList<ReportePeriodo> Periodos,
    IReadOnlyList<ReporteMetodo> Metodos,
    IReadOnlyList<ReporteProducto> Productos)
{
    public decimal TicketPromedio =>
        CantidadVentas > 0 ? VentasNetas / CantidadVentas : 0m;

    public decimal Margen =>
        VentasNetas > 0 ? Utilidad / VentasNetas : 0m;
}

/// <summary>
/// Consulta y calcula los reportes de administración. Solo lee la base: no
/// modifica ventas, cajas ni inventario. La utilidad usa el costo guardado en
/// cada línea de venta, por lo que un cambio posterior en el costo del
/// producto no altera los reportes históricos.
/// </summary>
public class ReporteService
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private static readonly CultureInfo CulturaEspanol =
        CultureInfo.GetCultureInfo("es-MX");

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public ReporteService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<Resultado<ReporteAdministracion>> ObtenerAsync(
        DateTime desde,
        DateTime hasta,
        AgrupacionReporte agrupacion,
        int topProductos = 10)
    {
        if (!_sesion.EsAdministrador)
            return new Resultado<ReporteAdministracion>(
                false, "Solo un administrador puede ver los reportes.", null);

        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        if (fin <= inicio)
            fin = inicio.AddDays(1);

        if (topProductos <= 0)
            topProductos = 10;

        // Se excluyen las ventas totalmente anuladas: ya no aportan dinero ni
        // unidades. Las devoluciones parciales sí se descuentan línea por línea.
        var ventas = await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Pagos)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Where(v => v.Estado == EstadoVenta.Completada &&
                        v.Fecha >= inicio && v.Fecha < fin)
            .OrderBy(v => v.Fecha)
            .ThenBy(v => v.Id)
            .ToListAsync();

        var lineas = new List<LineaReporte>();
        var utilidadPorVenta = new Dictionary<int, decimal>();

        foreach (var venta in ventas)
            CalcularLineas(venta, lineas, utilidadPorVenta);

        var ventasBrutas = ventas.Sum(v => v.Total);
        var devoluciones = ventas.Sum(v => v.TotalDevuelto);
        var ventasNetas = ventas.Sum(v => v.Total - v.TotalDevuelto);
        var costo = lineas.Sum(l => l.Costo);
        var utilidad = lineas.Sum(l => l.Utilidad);

        return new Resultado<ReporteAdministracion>(
            true,
            "Reporte generado.",
            new ReporteAdministracion(
                inicio,
                fin.AddDays(-1),
                agrupacion,
                ventas.Count,
                ventasBrutas,
                devoluciones,
                ventasNetas,
                costo,
                utilidad,
                AgruparPeriodos(ventas, utilidadPorVenta, agrupacion, fin),
                CalcularMetodos(ventas, ventasNetas),
                CalcularProductos(lineas, topProductos)));
    }

    /// <summary>
    /// Reparte el neto de la venta entre las unidades que siguen vigentes.
    /// Así el descuento y las devoluciones se prorratean sin perder centavos.
    /// </summary>
    private static void CalcularLineas(
        Venta venta,
        List<LineaReporte> lineas,
        Dictionary<int, decimal> utilidadPorVenta)
    {
        var pendientes = venta.Detalles
            .Where(d => d.CantidadPendiente > 0)
            .ToList();

        var ventaNeta = venta.Total - venta.TotalDevuelto;

        if (pendientes.Count == 0 || ventaNeta <= 0)
        {
            utilidadPorVenta[venta.Id] = 0m;
            return;
        }

        var pesos = pendientes
            .Select(d => d.PrecioUnitario * d.CantidadPendiente)
            .ToList();

        var pesoTotal = pesos.Sum();

        if (pesoTotal <= 0)
        {
            var costo = pendientes.Sum(d => d.CostoUnitario * d.CantidadPendiente);
            utilidadPorVenta[venta.Id] = -costo;
            return;
        }

        var centavosTotales = (int)Math.Round(ventaNeta * 100m);
        var centavosAsignados = 0;
        var utilidadVenta = 0m;

        for (var i = 0; i < pendientes.Count; i++)
        {
            var detalle = pendientes[i];

            var centavos = i == pendientes.Count - 1
                ? centavosTotales - centavosAsignados
                : (int)Math.Round(centavosTotales * (pesos[i] / pesoTotal));

            centavos = Math.Clamp(
                centavos, 0, centavosTotales - centavosAsignados);
            centavosAsignados += centavos;

            var ingreso = centavos / 100m;
            var costo = detalle.CostoUnitario * detalle.CantidadPendiente;
            var utilidad = ingreso - costo;

            utilidadVenta += utilidad;

            lineas.Add(new LineaReporte(
                detalle.IdProducto,
                detalle.Producto?.Sku ?? string.Empty,
                detalle.Producto?.Nombre ?? $"Producto {detalle.IdProducto}",
                detalle.CantidadPendiente,
                ingreso,
                costo,
                utilidad));
        }

        utilidadPorVenta[venta.Id] = utilidadVenta;
    }

    private static IReadOnlyList<ReportePeriodo> AgruparPeriodos(
        IReadOnlyList<Venta> ventas,
        IReadOnlyDictionary<int, decimal> utilidadPorVenta,
        AgrupacionReporte agrupacion,
        DateTime fin)
    {
        var periodos = ventas
            .GroupBy(v => InicioPeriodo(v.Fecha, agrupacion))
            .OrderBy(g => g.Key)
            .Select(g => new ReportePeriodo(
                EtiquetaPeriodo(g.Key, agrupacion, fin),
                g.Key,
                FinPeriodo(g.Key, agrupacion),
                g.Count(),
                g.Sum(v => v.Total - v.TotalDevuelto),
                g.Sum(v => v.TotalDevuelto),
                g.Sum(v => utilidadPorVenta.GetValueOrDefault(v.Id))))
            .ToList();

        var maximo = periodos.Count > 0
            ? periodos.Max(p => p.VentasNetas)
            : 0m;

        return periodos
            .Select(p => p with
            {
                Proporcion = maximo > 0
                    ? Math.Clamp(p.VentasNetas / maximo, 0m, 1m)
                    : 0m
            })
            .ToList();
    }

    private static IReadOnlyList<ReporteMetodo> CalcularMetodos(
        IReadOnlyList<Venta> ventas, decimal ventasNetas)
    {
        var pagos = ventas.SelectMany(PagosNetos).ToList();

        var metodos = new List<ReporteMetodo>();

        foreach (var metodo in new[]
                 {
                     MetodoPago.Efectivo,
                     MetodoPago.Tarjeta,
                     MetodoPago.Transferencia
                 })
        {
            var delMetodo = pagos
                .Where(p => p.Metodo == metodo)
                .ToList();

            var monto = delMetodo.Sum(p => p.Monto);

            metodos.Add(new ReporteMetodo(
                metodo,
                monto,
                delMetodo.Select(p => p.IdVenta).Distinct().Count(),
                ventasNetas > 0
                    ? Math.Clamp(monto / ventasNetas, 0m, 1m)
                    : 0m));
        }

        return metodos;
    }

    /// <summary>
    /// Pagos vigentes de una venta. Las ventas anteriores a la tabla de pagos
    /// se resuelven con su método y total neto original.
    /// </summary>
    private static IEnumerable<PagoNeto> PagosNetos(Venta venta)
    {
        if (venta.Pagos.Count > 0)
        {
            foreach (var pago in venta.Pagos)
            {
                var monto = pago.Monto - pago.MontoDevuelto;

                if (monto > 0 && pago.Metodo != MetodoPago.Mixto)
                    yield return new PagoNeto(venta.Id, pago.Metodo, monto);
            }

            yield break;
        }

        var montoNeto = venta.Total - venta.TotalDevuelto;

        if (montoNeto <= 0)
            yield break;

        var metodo = venta.MetodoPago == MetodoPago.Mixto
            ? MetodoPago.Efectivo
            : venta.MetodoPago;

        yield return new PagoNeto(venta.Id, metodo, montoNeto);
    }

    private static IReadOnlyList<ReporteProducto> CalcularProductos(
        IReadOnlyList<LineaReporte> lineas, int topProductos)
    {
        var productos = lineas
            .GroupBy(l => l.IdProducto)
            .Select(g => new
            {
                IdProducto = g.Key,
                Sku = g.Select(l => l.Sku).FirstOrDefault(s => s.Length > 0)
                      ?? string.Empty,
                Nombre = g.Select(l => l.Nombre)
                          .FirstOrDefault(n => n.Length > 0)
                      ?? $"Producto {g.Key}",
                Cantidad = g.Sum(l => l.Cantidad),
                Ventas = g.Sum(l => l.Ingreso),
                Costo = g.Sum(l => l.Costo),
                Utilidad = g.Sum(l => l.Utilidad)
            })
            .Where(p => p.Cantidad > 0)
            .OrderByDescending(p => p.Cantidad)
            .ThenByDescending(p => p.Ventas)
            .Take(topProductos)
            .ToList();

        var maximo = productos.Count > 0
            ? productos.Max(p => p.Cantidad)
            : 0;

        return productos
            .Select(p => new ReporteProducto(
                p.IdProducto,
                p.Sku,
                p.Nombre,
                p.Cantidad,
                p.Ventas,
                p.Costo,
                p.Utilidad,
                maximo > 0 ? p.Cantidad / (decimal)maximo : 0m))
            .ToList();
    }

    private static DateTime InicioPeriodo(
        DateTime fecha, AgrupacionReporte agrupacion)
        => agrupacion switch
        {
            AgrupacionReporte.Semana =>
                fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7)),
            AgrupacionReporte.Mes =>
                new DateTime(fecha.Year, fecha.Month, 1),
            _ => fecha.Date
        };

    private static DateTime FinPeriodo(
        DateTime inicio, AgrupacionReporte agrupacion)
        => agrupacion switch
        {
            AgrupacionReporte.Semana => inicio.AddDays(7),
            AgrupacionReporte.Mes => inicio.AddMonths(1),
            _ => inicio.AddDays(1)
        };

    private static string EtiquetaPeriodo(
        DateTime inicio, AgrupacionReporte agrupacion, DateTime fin)
    {
        var ultimoDia = FinPeriodo(inicio, agrupacion).AddDays(-1).Date;

        if (ultimoDia > fin.AddDays(-1))
            ultimoDia = fin.AddDays(-1);

        return agrupacion switch
        {
            AgrupacionReporte.Semana => inicio.Year == ultimoDia.Year
                ? $"{inicio.ToString("dd/MM", Cultura)} – " +
                  ultimoDia.ToString("dd/MM/yyyy", Cultura)
                : $"{inicio.ToString("dd/MM/yyyy", Cultura)} – " +
                  ultimoDia.ToString("dd/MM/yyyy", Cultura),
            AgrupacionReporte.Mes => Capitalizar(
                inicio.ToString("MMMM yyyy", CulturaEspanol)),
            _ => inicio.ToString("dd/MM/yyyy", Cultura)
        };
    }

    private static string Capitalizar(string texto)
        => texto.Length == 0
            ? texto
            : char.ToUpper(texto[0], CulturaEspanol) + texto[1..];

    private sealed record LineaReporte(
        int IdProducto,
        string Sku,
        string Nombre,
        int Cantidad,
        decimal Ingreso,
        decimal Costo,
        decimal Utilidad);

    private sealed record PagoNeto(
        int IdVenta, MetodoPago Metodo, decimal Monto);
}
