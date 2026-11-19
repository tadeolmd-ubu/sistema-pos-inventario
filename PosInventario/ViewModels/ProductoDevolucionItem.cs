using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.ViewModels;

/// <summary>
/// Línea de producto del checklist de devolución. Los productos por pieza se
/// marcan unidad por unidad; los productos por peso usan una cantidad decimal.
/// </summary>
public partial class ProductoDevolucionItem : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly Action _cambio;

    public ProductoDevolucionItem(DetalleVenta detalle, Action cambio)
    {
        Detalle = detalle;
        _cambio = cambio;

        var nombre = Nombre;

        Unidades = EsPorPieza
            ? Enumerable
                .Range(1, (int)detalle.CantidadPendiente)
                .Select(_ => new UnidadDevolucionItem(nombre, cambio))
                .ToList()
            : [];
    }

    public DetalleVenta Detalle { get; }

    public IReadOnlyList<UnidadDevolucionItem> Unidades { get; }

    public int IdDetalleVenta => Detalle.Id;

    public string Nombre => Detalle.Producto?.Nombre ?? "Producto";

    public bool EsPorPeso => Detalle.Producto?.EsPorPeso == true;

    public bool EsPorPieza => !EsPorPeso;

    public bool EsUnidadUnica => EsPorPieza && Unidades.Count == 1;

    public bool EsGrupo => EsPorPieza && Unidades.Count > 1;

    public string TituloGrupo => $"{Nombre} - x{Unidades.Count}";

    public string CantidadPendienteTexto => Detalle.CantidadPendienteTexto;

    public string UnidadTexto => Detalle.Unidad.Abreviatura();

    /// <summary>
    /// Única unidad de la línea cuando solo queda una pendiente.
    /// </summary>
    public UnidadDevolucionItem Unica => Unidades[0];

    [ObservableProperty] private bool expandido;
    [ObservableProperty] private string cantidadTexto = string.Empty;

    public decimal CantidadSeleccionada
    {
        get
        {
            if (!EsPorPeso)
                return Unidades.Count(u => u.Seleccionada);

            if (!TryParseDecimal(CantidadTexto, out var cantidad))
                return 0m;

            return Math.Clamp(
                cantidad,
                0m,
                Detalle.CantidadPendiente);
        }
    }

    public string CantidadSeleccionadaTexto =>
        Detalle.Unidad.FormatearConUnidad(CantidadSeleccionada);

    public bool TodasSeleccionadas => EsPorPeso
        ? CantidadSeleccionada == Detalle.CantidadPendiente
        : Unidades.All(u => u.Seleccionada);

    partial void OnCantidadTextoChanged(string value)
    {
        OnPropertyChanged(nameof(CantidadSeleccionada));
        OnPropertyChanged(nameof(CantidadSeleccionadaTexto));
        OnPropertyChanged(nameof(TodasSeleccionadas));
        _cambio();
    }

    [RelayCommand]
    private void DevolverTodo()
        => CantidadTexto = Detalle.Unidad.Formatear(Detalle.CantidadPendiente);

    public void DefinirSeleccion(bool seleccionada, bool expandir)
    {
        if (EsPorPeso)
        {
            CantidadTexto = seleccionada
                ? Detalle.Unidad.Formatear(Detalle.CantidadPendiente)
                : string.Empty;
            return;
        }

        if (expandir)
            Expandido = true;

        foreach (var unidad in Unidades)
            unidad.Definir(seleccionada);
    }

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(
            limpio, NumberStyles.Number, Cultura, out valor);
    }
}
