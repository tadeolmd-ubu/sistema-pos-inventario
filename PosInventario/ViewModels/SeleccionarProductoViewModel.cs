using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class SeleccionarProductoViewModel : OperacionViewModel
{
    private readonly ProductoService? _productoService;
    private readonly List<Producto> _cargados = [];

    public SeleccionarProductoViewModel() { }

    public SeleccionarProductoViewModel(ProductoService productoService)
        => _productoService = productoService;

    public ObservableCollection<Producto> ProductosItems { get; } = [];

    [ObservableProperty] private Producto? selectedProducto;
    [ObservableProperty] private string busqueda = string.Empty;

    /// <summary>
    /// Lo escucha la ventana para cerrarse devolviendo el producto elegido.
    /// </summary>
    public event Action<Producto>? ProductoSeleccionado;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_productoService is null) return;

        _cargados.Clear();
        _cargados.AddRange((await _productoService.ObtenerTodosAsync())
            .Where(p => p.Stock > 0));
        Filtrar();
    });

    partial void OnBusquedaChanged(string value) => Filtrar();

    [RelayCommand]
    private void Seleccionar()
    {
        if (SelectedProducto is null)
        {
            MensajeOperacion = "Seleccioná un producto.";
            return;
        }

        if (SelectedProducto.Stock <= 0)
        {
            MensajeOperacion = "El producto no tiene stock.";
            return;
        }

        ProductoSeleccionado?.Invoke(SelectedProducto);
    }

    private void Filtrar()
    {
        var texto = (Busqueda ?? string.Empty).Trim();

        var filtrados = string.IsNullOrWhiteSpace(texto)
            ? _cargados
            : _cargados.Where(p =>
                p.Sku.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase));

        var seleccionado = SelectedProducto?.Id;

        ReemplazarItems(ProductosItems, filtrados);
        SelectedProducto = ProductosItems.FirstOrDefault(p => p.Id == seleccionado);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
