using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.ViewModels;

public class LineaCompraItem : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public LineaCompraItem(
        Producto producto, decimal cantidad, decimal costoUnitario)
    {
        Producto = producto;
        Cantidad = cantidad;
        CostoUnitario = costoUnitario;
    }

    public Producto Producto { get; }

    public string Nombre => Producto.Nombre;

    public string Sku => Producto.Sku;

    public decimal Cantidad { get; private set; }

    public decimal CostoUnitario { get; private set; }

    public decimal Subtotal => Math.Round(
        Cantidad * CostoUnitario, 2, MidpointRounding.AwayFromZero);

    public string CantidadTexto =>
        Producto.UnidadMedida.FormatearConUnidad(Cantidad);

    public string CostoUnitarioTexto => Producto.EsPorPeso
        ? $"{CostoUnitario.ToString("0.00", Cultura)}/kg"
        : CostoUnitario.ToString("0.00", Cultura);

    public string SubtotalTexto => Subtotal.ToString("0.00", Cultura);

    /// <summary>
    /// Al repetir un producto se suman las cantidades y se toma el último
    /// costo ingresado.
    /// </summary>
    public void Fusionar(decimal cantidad, decimal costoUnitario)
    {
        Cantidad += cantidad;
        CostoUnitario = costoUnitario;

        OnPropertyChanged(nameof(Cantidad));
        OnPropertyChanged(nameof(CantidadTexto));
        OnPropertyChanged(nameof(CostoUnitario));
        OnPropertyChanged(nameof(CostoUnitarioTexto));
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(SubtotalTexto));
    }
}
