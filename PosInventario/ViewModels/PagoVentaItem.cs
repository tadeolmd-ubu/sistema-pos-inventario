using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models.Enums;

namespace PosInventario.ViewModels;

/// <summary>
/// Fila de pago del cobro. En efectivo, <see cref="MontoTexto"/> es la parte
/// aplicada a la venta y <see cref="RecibidoTexto"/> lo que entrega el
/// cliente; la diferencia es el cambio.
/// </summary>
public partial class PagoVentaItem : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private static readonly MetodoPago[] Metodos =
        [MetodoPago.Efectivo, MetodoPago.Tarjeta, MetodoPago.Transferencia];

    private readonly Action _cambio;
    private readonly Action<PagoVentaItem> _eliminar;

    public PagoVentaItem(
        MetodoPago metodo,
        decimal monto,
        Action cambio,
        Action<PagoVentaItem> eliminar)
    {
        this.metodo = metodo;
        montoTexto = monto.ToString("0.00", Cultura);

        _cambio = cambio;
        _eliminar = eliminar;

        EliminarCommand = new RelayCommand(() => _eliminar(this));
    }

    public IReadOnlyList<MetodoPago> MetodosPago => Metodos;

    public IRelayCommand EliminarCommand { get; }

    [ObservableProperty] private MetodoPago metodo;
    [ObservableProperty] private string montoTexto = string.Empty;
    [ObservableProperty] private string recibidoTexto = string.Empty;

    public bool EsEfectivo => Metodo == MetodoPago.Efectivo;

    public decimal Monto => Parse(MontoTexto);

    public decimal Recibido => !EsEfectivo
        ? 0m
        : string.IsNullOrWhiteSpace(RecibidoTexto)
            ? Monto
            : Parse(RecibidoTexto);

    public decimal Cambio => EsEfectivo ? Math.Max(0m, Recibido - Monto) : 0m;

    public bool MontoValido => Monto > 0;

    public bool RecibidoValido => !EsEfectivo || Recibido >= Monto;

    partial void OnMetodoChanged(MetodoPago value)
    {
        if (value != MetodoPago.Efectivo)
            RecibidoTexto = string.Empty;

        OnPropertyChanged(nameof(EsEfectivo));
        OnPropertyChanged(nameof(Recibido));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(RecibidoValido));

        _cambio();
    }

    partial void OnMontoTextoChanged(string value)
    {
        OnPropertyChanged(nameof(Monto));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(MontoValido));
        OnPropertyChanged(nameof(RecibidoValido));

        _cambio();
    }

    partial void OnRecibidoTextoChanged(string value)
    {
        OnPropertyChanged(nameof(Recibido));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(RecibidoValido));

        _cambio();
    }

    private static decimal Parse(string? texto)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');

        return decimal.TryParse(
            limpio, NumberStyles.Number, Cultura, out var valor)
            ? valor
            : 0m;
    }
}
