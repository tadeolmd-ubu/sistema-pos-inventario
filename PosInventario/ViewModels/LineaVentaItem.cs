using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public class LineaVentaItem : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly Action<LineaVentaItem> _aumentar;
    private readonly Action<LineaVentaItem> _disminuir;
    private readonly Action<LineaVentaItem> _eliminar;
    private readonly Func<LineaVentaItem, Task> _editar;

    public LineaVentaItem(
        Producto producto,
        decimal cantidad,
        Action<LineaVentaItem> aumentar,
        Action<LineaVentaItem> disminuir,
        Action<LineaVentaItem> eliminar,
        Func<LineaVentaItem, Task> editar)
    {
        Producto = producto;
        Cantidad = cantidad;
        PrecioUnitario = producto.PrecioVenta;
        _aumentar = aumentar;
        _disminuir = disminuir;
        _eliminar = eliminar;
        _editar = editar;

        AumentarCommand = new RelayCommand(() => _aumentar(this));
        DisminuirCommand = new RelayCommand(() => _disminuir(this));
        EliminarCommand = new RelayCommand(() => _eliminar(this));
        EditarCommand = new AsyncRelayCommand(() => _editar(this));
    }

    public Producto Producto { get; }

    public string Nombre => Producto.Nombre;

    public string Sku => Producto.Sku;

    public decimal Cantidad { get; private set; }

    public decimal PrecioUnitario { get; }

    public decimal Subtotal => Math.Round(
        Cantidad * PrecioUnitario, 2, MidpointRounding.AwayFromZero);

    public bool EsPorPeso => Producto.EsPorPeso;

    public bool EsPorPieza => !EsPorPeso;

    public string UnidadTexto => Producto.UnidadTexto;

    public string CantidadTexto => Producto.UnidadMedida.Formatear(Cantidad);

    public string CantidadConUnidadTexto =>
        Producto.UnidadMedida.FormatearConUnidad(Cantidad);

    public string PrecioUnitarioTexto => EsPorPeso
        ? $"{PrecioUnitario.ToString("0.00", Cultura)}/kg"
        : PrecioUnitario.ToString("0.00", Cultura);

    public string SubtotalTexto => Subtotal.ToString("0.00", Cultura);

    public IRelayCommand AumentarCommand { get; }

    public IRelayCommand DisminuirCommand { get; }

    public IRelayCommand EliminarCommand { get; }

    public IAsyncRelayCommand EditarCommand { get; }

    public void Aumentar()
    {
        if (EsPorPeso) return;

        Cantidad++;
        NotificarCantidad();
    }

    public void Disminuir()
    {
        if (EsPorPeso || Cantidad <= 1) return;

        Cantidad--;
        NotificarCantidad();
    }

    public void Fusionar(decimal cantidad)
    {
        Cantidad = Cantidades.Normalizar(Cantidad + cantidad);
        NotificarCantidad();
    }

    public void EstablecerCantidad(decimal cantidad)
    {
        Cantidad = Cantidades.Normalizar(cantidad);
        NotificarCantidad();
    }

    private void NotificarCantidad()
    {
        OnPropertyChanged(nameof(Cantidad));
        OnPropertyChanged(nameof(CantidadTexto));
        OnPropertyChanged(nameof(CantidadConUnidadTexto));
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(SubtotalTexto));
    }
}
