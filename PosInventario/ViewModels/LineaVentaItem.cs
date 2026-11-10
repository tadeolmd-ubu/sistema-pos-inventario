using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;

namespace PosInventario.ViewModels;

public class LineaVentaItem : ObservableObject
{
    private readonly Action<LineaVentaItem> _aumentar;
    private readonly Action<LineaVentaItem> _disminuir;
    private readonly Action<LineaVentaItem> _eliminar;

    public LineaVentaItem(
        Producto producto,
        int cantidad,
        Action<LineaVentaItem> aumentar,
        Action<LineaVentaItem> disminuir,
        Action<LineaVentaItem> eliminar)
    {
        Producto = producto;
        Cantidad = cantidad;
        PrecioUnitario = producto.PrecioVenta;
        _aumentar = aumentar;
        _disminuir = disminuir;
        _eliminar = eliminar;

        AumentarCommand = new RelayCommand(() => _aumentar(this));
        DisminuirCommand = new RelayCommand(() => _disminuir(this));
        EliminarCommand = new RelayCommand(() => _eliminar(this));
    }

    public Producto Producto { get; }

    public string Nombre => Producto.Nombre;

    public string Sku => Producto.Sku;

    public int Cantidad { get; private set; }

    public decimal PrecioUnitario { get; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    public IRelayCommand AumentarCommand { get; }

    public IRelayCommand DisminuirCommand { get; }

    public IRelayCommand EliminarCommand { get; }

    public void Aumentar()
    {
        Cantidad++;
        OnPropertyChanged(nameof(Cantidad));
        OnPropertyChanged(nameof(Subtotal));
    }

    public void Disminuir()
    {
        if (Cantidad <= 1) return;

        Cantidad--;
        OnPropertyChanged(nameof(Cantidad));
        OnPropertyChanged(nameof(Subtotal));
    }

    public void Fusionar(int cantidad)
    {
        Cantidad += cantidad;
        OnPropertyChanged(nameof(Cantidad));
        OnPropertyChanged(nameof(Subtotal));
    }
}
