using System.Globalization;
using PosInventario.Models;

namespace PosInventario.ViewModels;

public class MovimientoCajaViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public MovimientoCajaViewModel(MovimientoCaja movimiento)
        => Movimiento = movimiento;

    public MovimientoCaja Movimiento { get; }

    public string Tipo => Movimiento.Tipo.ToString();

    public string Fecha => Movimiento.Fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string Usuario => Movimiento.Usuario?.Nombre ?? "—";

    public string CajaTexto => $"Caja #{Movimiento.IdCaja}";

    public string MontoTexto => Movimiento.Monto.ToString("0.00", Cultura);

    public string Descripcion => string.IsNullOrWhiteSpace(Movimiento.Descripcion)
        ? "—"
        : Movimiento.Descripcion;

    public bool EsVenta => Movimiento.Venta is not null;

    public string VentaNumero => Movimiento.Venta is null
        ? "—"
        : $"V-{Movimiento.Venta.Id:000000}";

    public IReadOnlyList<DetalleVenta> Detalles =>
        Movimiento.Venta?.Detalles.ToList() ?? [];

    public string TotalVentaTexto =>
        (Movimiento.Venta?.Total ?? 0m).ToString("0.00", Cultura);

    public string MetodoPagoTexto =>
        Movimiento.Venta?.MetodoPago.ToString() ?? "—";
}
