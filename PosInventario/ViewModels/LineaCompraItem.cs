using CommunityToolkit.Mvvm.ComponentModel;
using PosInventario.Models;

namespace PosInventario.ViewModels;

public class LineaCompraItem : ObservableObject
{
    public LineaCompraItem(Producto producto, int cantidad, decimal costoUnitario)
    {
        Producto = producto;
        Cantidad = cantidad;
        CostoUnitario = costoUnitario;
    }

    public Producto Producto { get; }

    public string Nombre => Producto.Nombre;

    public string Sku => Producto.Sku;

    public int Cantidad { get; private set; }

    public decimal CostoUnitario { get; private set; }

    public decimal Subtotal => Cantidad * CostoUnitario;

    /// <summary>
    /// Al repetir un producto se suman las cantidades y se toma el último
    /// costo ingresado.
    /// </summary>
    public void Fusionar(int cantidad, decimal costoUnitario)
    {
        Cantidad += cantidad;
        CostoUnitario = costoUnitario;

        OnPropertyChanged(nameof(Cantidad));
        OnPropertyChanged(nameof(CostoUnitario));
        OnPropertyChanged(nameof(Subtotal));
    }
}
