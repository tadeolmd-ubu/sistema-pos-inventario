using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class SeleccionarProductoViewModel : OperacionViewModel
{
    private readonly ProductoService? _productoService;
    private CancellationTokenSource? _busquedaPendiente;
    private int _version;

    public SeleccionarProductoViewModel()
    {
        Paginacion.CambioSolicitado += CargarPaginaActual;
    }

    public SeleccionarProductoViewModel(ProductoService productoService)
        : this()
        => _productoService = productoService;

    public ObservableCollection<Producto> ProductosItems { get; } = [];

    public PaginacionViewModel Paginacion { get; } = new();

    [ObservableProperty] private Producto? selectedProducto;
    [ObservableProperty] private string busqueda = string.Empty;

    /// <summary>
    /// Lo escucha la ventana para cerrarse devolviendo el producto elegido.
    /// </summary>
    public event Action<Producto>? ProductoSeleccionado;

    [RelayCommand]
    public Task CargarAsync()
    {
        _busquedaPendiente?.Cancel();
        return CargarPaginaAsync(reiniciarPagina: true);
    }

    private void CargarPaginaActual() => _ = CargarPaginaAsync(false);

    partial void OnBusquedaChanged(string value)
    {
        if (_productoService is null)
            return;

        // Espera un momento antes de consultar para no lanzar una búsqueda
        // por cada tecla; una búsqueda nueva cancela la anterior.
        _busquedaPendiente?.Cancel();
        _busquedaPendiente?.Dispose();
        _busquedaPendiente = new CancellationTokenSource();

        _ = BuscarConRetrasoAsync(_busquedaPendiente.Token);
    }

    private async Task BuscarConRetrasoAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(200, token);
            await CargarPaginaAsync(reiniciarPagina: true);
        }
        catch (OperationCanceledException)
        {
            // Otra búsqueda reemplazó a esta.
        }
    }

    private async Task CargarPaginaAsync(bool reiniciarPagina)
    {
        if (_productoService is null)
            return;

        if (reiniciarPagina)
            Paginacion.Reiniciar();

        var version = ++_version;
        var idAnterior = SelectedProducto?.Id;

        try
        {
            var pagina = await _productoService.ObtenerConStockPaginadoAsync(
                Busqueda, Paginacion.PaginaActual, Paginacion.TamanoPagina);

            // Si mientras se consultaba se lanzó otra búsqueda, se descarta
            // este resultado para no mostrar datos viejos.
            if (version != _version)
                return;

            ReemplazarItems(ProductosItems, pagina.Items);
            Paginacion.EstablecerTotal(pagina.TotalRegistros);

            SelectedProducto = ProductosItems.FirstOrDefault(p => p.Id == idAnterior)
                ?? ProductosItems.FirstOrDefault();
        }
        catch (Exception ex)
        {
            if (version != _version)
                return;

            MensajeOperacion = $"No se pudieron cargar los productos: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Seleccionar()
    {
        if (SelectedProducto is null)
        {
            MensajeOperacion = "Selecciona un producto.";
            return;
        }

        if (SelectedProducto.Stock <= 0)
        {
            MensajeOperacion = "El producto no tiene stock.";
            return;
        }

        ProductoSeleccionado?.Invoke(SelectedProducto);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
