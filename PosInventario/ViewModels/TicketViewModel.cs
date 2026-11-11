using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class TicketViewModel : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly IImpresoraTicket _impresora;

    public TicketViewModel(Venta venta, IImpresoraTicket? impresora = null)
    {
        Venta = venta;
        _impresora = impresora ?? new ImpresoraSistema();
        AnchoSeleccionado = AnchosTicket[1];
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

    public bool HayDevolucion => Venta.HayDevolucion;

    public string TotalDevueltoTexto =>
        Venta.TotalDevuelto.ToString("0.00", Cultura);

    public string TotalNetoTexto => Venta.TotalNeto.ToString("0.00", Cultura);

    public IReadOnlyList<PagoVenta> Pagos => Venta.Pagos.ToList();

    public bool HayPagos => Pagos.Count > 0;

    public bool HayPagoEfectivo =>
        Venta.Pagos.Count > 0
            ? Venta.Pagos.Any(p => p.Metodo == MetodoPago.Efectivo)
            : Venta.MetodoPago == MetodoPago.Efectivo;

    public decimal EfectivoRecibido =>
        Venta.Pagos.Count > 0
            ? Venta.Pagos
                .Where(p => p.Metodo == MetodoPago.Efectivo)
                .Sum(p => p.Monto) + Venta.Vuelto
            : Venta.MontoRecibido;

    public string MontoRecibidoTexto =>
        EfectivoRecibido.ToString("0.00", Cultura);

    public string EstadoTexto => Venta.Estado.ToString();

    public bool EsAnulada => Venta.Estado == EstadoVenta.Anulada;

    public string VueltoTexto => Venta.Vuelto.ToString("0.00", Cultura);

    public IReadOnlyList<AnchoTicketOpcion> AnchosTicket { get; } =
    [
        new(AnchoTicket.Mm58),
        new(AnchoTicket.Mm80)
    ];

    [ObservableProperty] private AnchoTicketOpcion anchoSeleccionado = null!;

    [ObservableProperty] private string mensajeImpresion = string.Empty;
    [ObservableProperty] private bool impresionCorrecta;
    [ObservableProperty] private bool imprimiendo;

    public bool HayMensajeImpresion => !string.IsNullOrWhiteSpace(MensajeImpresion);

    public bool EsMensajeExito => HayMensajeImpresion && ImpresionCorrecta;

    public bool EsMensajeError => HayMensajeImpresion && !ImpresionCorrecta;

    partial void OnMensajeImpresionChanged(string value) => NotificarMensaje();

    partial void OnImpresionCorrectaChanged(bool value) => NotificarMensaje();

    [RelayCommand]
    private async Task ImprimirAsync()
    {
        if (Imprimiendo) return;

        var ancho = AnchoSeleccionado?.Ancho ?? AnchoTicket.Mm80;

        Imprimiendo = true;
        ImpresionCorrecta = true;
        MensajeImpresion = "Enviando el ticket a la impresora...";

        try
        {
            var contenido = GeneradorTicketTexto.Generar(Venta, ancho);
            var resultado = await _impresora.ImprimirAsync(contenido, ancho);

            ImpresionCorrecta = resultado.Correcta;
            MensajeImpresion = resultado.Mensaje;
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo imprimir el ticket.", ex);

            ImpresionCorrecta = false;
            MensajeImpresion =
                "No se pudo imprimir el ticket. Revisa la impresora e inténtalo de nuevo.";
        }
        finally
        {
            Imprimiendo = false;
        }
    }

    private void NotificarMensaje()
    {
        OnPropertyChanged(nameof(HayMensajeImpresion));
        OnPropertyChanged(nameof(EsMensajeExito));
        OnPropertyChanged(nameof(EsMensajeError));
    }
}
