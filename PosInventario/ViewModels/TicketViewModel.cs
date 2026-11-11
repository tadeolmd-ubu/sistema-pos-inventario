using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public class TicketViewModel
{
    private static readonly System.Globalization.CultureInfo Cultura =
        System.Globalization.CultureInfo.InvariantCulture;

    public TicketViewModel(Venta venta)
    {
        Venta = venta;
    }

    public Venta Venta { get; }

    public string Numero => $"V-{Venta.Id:000000}";

    public string Fecha => Venta.Fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string Cajero => Venta.Usuario?.Nombre ?? "—";

    public IReadOnlyList<DetalleVenta> Detalles => Venta.Detalles.ToList();

    public string SubtotalTexto => Venta.Subtotal.ToString("0.00", Cultura);

    public bool HayDescuento => Venta.Descuento > 0;

    public string DescuentoTexto => Venta.Descuento.ToString("0.00", Cultura);

    public string TotalTexto => Venta.Total.ToString("0.00", Cultura);

    public string MetodoPagoTexto => Venta.MetodoPago.ToString();

    public bool EsEfectivo => Venta.MetodoPago == MetodoPago.Efectivo;

    public string EstadoTexto => Venta.Estado.ToString();

    public bool EsAnulada => Venta.Estado == EstadoVenta.Anulada;

    public string MontoRecibidoTexto => Venta.MontoRecibido.ToString("0.00", Cultura);

    public string VueltoTexto => Venta.Vuelto.ToString("0.00", Cultura);
}
