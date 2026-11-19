using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Pide el peso de un producto por kilogramo antes de agregarlo a la venta.
/// </summary>
public partial class CantidadPesoViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly Producto _producto = null!;

    public CantidadPesoViewModel() { }

    public CantidadPesoViewModel(Producto producto, decimal cantidadInicial)
    {
        _producto = producto;

        var inicial = producto.Stock > 0m
            ? Math.Min(cantidadInicial, producto.Stock)
            : 0m;

        CantidadTexto = producto.UnidadMedida.Formatear(inicial);
    }

    public string Titulo => $"Cantidad de {_producto.Nombre}";

    public string PrecioTexto => _producto.PrecioVentaTexto;

    public string StockTexto => _producto.StockTexto;

    public string UnidadTexto => _producto.UnidadTexto;

    [ObservableProperty] private string cantidadTexto = string.Empty;

    public decimal CantidadActual =>
        TryParseDecimal(CantidadTexto, out var cantidad) && cantidad > 0m
            ? cantidad
            : 0m;

    public string SubtotalTexto => Math.Round(
        CantidadActual * _producto.PrecioVenta,
        2,
        MidpointRounding.AwayFromZero).ToString("0.00", Cultura);

    public bool PuedeAgregar =>
        CantidadActual > 0m &&
        CantidadActual <= _producto.Stock &&
        !Cantidades.TieneDemasiadosDecimales(CantidadActual);

    public event Action<decimal>? Confirmado;

    partial void OnCantidadTextoChanged(string value)
    {
        OnPropertyChanged(nameof(CantidadActual));
        OnPropertyChanged(nameof(SubtotalTexto));
        OnPropertyChanged(nameof(PuedeAgregar));
    }

    [RelayCommand]
    private void UsarCantidadRapida(string? texto)
    {
        if (TryParseDecimal(texto, out var cantidad))
            CantidadTexto = _producto.UnidadMedida.Formatear(cantidad);
    }

    [RelayCommand]
    private void Confirmar()
    {
        if (!PuedeAgregar)
        {
            MensajeOperacion =
                "El peso debe ser mayor que 0, tener máximo 3 decimales " +
                "y no superar el stock.";
            return;
        }

        Confirmado?.Invoke(Cantidades.Normalizar(CantidadActual));
    }

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(
            limpio, NumberStyles.Number, Cultura, out valor);
    }
}
