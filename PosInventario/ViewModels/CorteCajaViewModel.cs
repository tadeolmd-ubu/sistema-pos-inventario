using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Resumen imprimible de una caja cerrada. Muestra las ventas, el desglose por
/// método de pago y la diferencia entre el efectivo esperado y el contado.
/// </summary>
public partial class CorteCajaViewModel : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly IImpresoraTicket _impresora;

    public CorteCajaViewModel(
        Caja caja, ResumenPagosCaja resumen, IImpresoraTicket? impresora = null)
    {
        Caja = caja;
        Resumen = resumen;
        _impresora = impresora ?? new ImpresoraSistema();
        AnchoSeleccionado = AnchosTicket[1];
    }

    public Caja Caja { get; }

    public ResumenPagosCaja Resumen { get; }

    public string Numero => $"Corte de caja #{Caja.Id}";

    public string UsuarioApertura => Caja.UsuarioApertura?.Nombre ?? "—";

    public string FechaApertura =>
        Caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string FechaCierre =>
        Caja.FechaCierre?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—";

    public string CantidadVentasTexto => Resumen.CantidadVentas.ToString(Cultura);

    public string TotalVentasTexto => Resumen.Total.ToString("0.00", Cultura);

    public string TotalEfectivoTexto => Resumen.Efectivo.ToString("0.00", Cultura);

    public string TotalTarjetaTexto => Resumen.Tarjeta.ToString("0.00", Cultura);

    public string TotalTransferenciaTexto =>
        Resumen.Transferencia.ToString("0.00", Cultura);

    public string MontoInicialTexto => Caja.MontoInicial.ToString("0.00", Cultura);

    public decimal MontoEsperado => Caja.MontoEsperado ?? 0m;

    public string MontoEsperadoTexto => MontoEsperado.ToString("0.00", Cultura);

    public decimal MontoContado => Caja.MontoFinal ?? 0m;

    public string MontoContadoTexto => MontoContado.ToString("0.00", Cultura);

    public decimal Diferencia => Caja.Diferencia ?? (MontoContado - MontoEsperado);

    public string DiferenciaTexto =>
        Diferencia.ToString("+0.00;-0.00;0.00", Cultura);

    public bool HayFaltante => Diferencia < 0;

    public bool HaySobrante => Diferencia > 0;

    public bool SinDiferencia => Diferencia == 0;

    public string FaltanteTexto =>
        $"Faltante: {Math.Abs(Diferencia).ToString("0.00", Cultura)}";

    public string SobranteTexto =>
        $"Sobrante: {Diferencia.ToString("0.00", Cultura)}";

    public string Observaciones =>
        string.IsNullOrWhiteSpace(Caja.Observaciones) ? "—" : Caja.Observaciones.Trim();

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
        MensajeImpresion = "Enviando el corte a la impresora...";

        try
        {
            var contenido = GeneradorCorteCajaTexto.Generar(Caja, Resumen, ancho);
            var resultado = await _impresora.ImprimirAsync(contenido, ancho);

            ImpresionCorrecta = resultado.Correcta;
            MensajeImpresion = resultado.Mensaje;
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo imprimir el corte de caja.", ex);

            ImpresionCorrecta = false;
            MensajeImpresion =
                "No se pudo imprimir el corte. Revisa la impresora e inténtalo de nuevo.";
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
