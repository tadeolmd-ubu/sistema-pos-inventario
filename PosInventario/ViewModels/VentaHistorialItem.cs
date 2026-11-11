using System.Globalization;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Venta listada en el detalle de una caja: datos para la lista y el botón
/// de devolución, visible solo para el vendedor original o un administrador
/// cuando la venta aún tiene unidades pendientes.
/// </summary>
public class VentaHistorialItem
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public VentaHistorialItem(Venta venta, SesionActual? sesion)
    {
        Venta = venta;

        PuedeDevolver =
            venta.Estado == EstadoVenta.Completada &&
            venta.Detalles.Any(d => d.CantidadPendiente > 0) &&
            sesion is not null &&
            (sesion.EsAdministrador || venta.IdUsuario == sesion.IdUsuario);
    }

    public Venta Venta { get; }

    public bool PuedeDevolver { get; }

    public string Ticket => $"V-{Venta.Id:000000}";

    public string FechaTexto => Venta.Fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string MetodoPagoTexto => Venta.MetodoPago.ToString();

    public string EstadoTexto => Venta.Estado.ToString();

    public string TotalTexto => Venta.Total.ToString("0.00", Cultura);

    public bool HayDevolucion => Venta.TotalDevuelto > 0;

    public string DevueltoTexto =>
        $"Devuelto: {Venta.TotalDevuelto.ToString("0.00", Cultura)}";
}
