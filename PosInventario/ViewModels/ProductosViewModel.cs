using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ProductosViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly ProductoService? _productoService;
    private readonly List<Producto> _cargados = [];
    private bool _limpiando;

    public ProductosViewModel() { }

    public ProductosViewModel(ProductoService productoService)
        => _productoService = productoService;

    public ObservableCollection<Producto> ProductosItems { get; } = [];

    [ObservableProperty] private Producto? selectedProducto;
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private bool verArchivados;
    [ObservableProperty] private string sku = string.Empty;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string descripcion = string.Empty;
    [ObservableProperty] private string precioVentaTexto = string.Empty;
    [ObservableProperty] private string costoTexto = string.Empty;
    [ObservableProperty] private string stockMinimoTexto = string.Empty;

    public bool EsModoCreacion => SelectedProducto is null;

    public bool PuedeGuardar =>
        SelectedProducto is null || SelectedProducto.FechaArchivado is null;

    public bool PuedeArchivar =>
        SelectedProducto is not null && SelectedProducto.FechaArchivado is null;

    public bool PuedeRestaurar =>
        SelectedProducto is not null && SelectedProducto.FechaArchivado is not null;

    public string StockActual => SelectedProducto is null
        ? "El stock inicial es 0 y se carga desde Inventario."
        : $"Stock actual: {SelectedProducto.Stock}";

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(CargarDatosAsync);

    private async Task CargarDatosAsync()
    {
        if (_productoService is null) return;

        _cargados.Clear();
        _cargados.AddRange(VerArchivados
            ? await _productoService.ObtenerArchivadosAsync()
            : await _productoService.ObtenerTodosAsync());

        Filtrar();
    }

    partial void OnVerArchivadosChanged(bool value)
    {
        if (!_limpiando)
            _ = CargarAsync();
    }

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnSelectedProductoChanged(Producto? value)
    {
        Sku = value?.Sku ?? string.Empty;
        Nombre = value?.Nombre ?? string.Empty;
        Descripcion = value?.Descripcion ?? string.Empty;
        PrecioVentaTexto = (value?.PrecioVenta ?? 0).ToString("0.00", Cultura);
        CostoTexto = (value?.Costo ?? 0).ToString("0.00", Cultura);
        StockMinimoTexto = (value?.StockMinimo ?? 0).ToString(Cultura);

        OnPropertyChanged(nameof(EsModoCreacion));
        OnPropertyChanged(nameof(PuedeGuardar));
        OnPropertyChanged(nameof(PuedeArchivar));
        OnPropertyChanged(nameof(PuedeRestaurar));
        OnPropertyChanged(nameof(StockActual));
    }

    [RelayCommand]
    private void LimpiarFormulario()
    {
        SelectedProducto = null;
        Sku = string.Empty;
        Nombre = string.Empty;
        Descripcion = string.Empty;
        PrecioVentaTexto = string.Empty;
        CostoTexto = string.Empty;
        StockMinimoTexto = string.Empty;
    }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_productoService is null) return;

        if (!TryParseDecimal(PrecioVentaTexto, out var precioVenta))
        {
            MensajeOperacion = Notificar("El precio de venta no es un número válido.");
            return;
        }

        if (!TryParseDecimal(CostoTexto, out var costo))
        {
            MensajeOperacion = Notificar("El costo no es un número válido.");
            return;
        }

        if (!TryParseEntero(StockMinimoTexto, out var stockMinimo))
        {
            MensajeOperacion = Notificar("El stock mínimo debe ser un número entero.");
            return;
        }

        bool correcta;
        string mensaje;

        if (EsModoCreacion)
        {
            var resultado = await _productoService.AgregarAsync(
                Sku, Nombre, Descripcion, precioVenta, costo, stockMinimo);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }
        else
        {
            var resultado = await _productoService.ActualizarAsync(
                SelectedProducto!.Id, Sku, Nombre, Descripcion,
                precioVenta, costo, stockMinimo);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }

        MensajeOperacion = Notificar(mensaje, correcta);

        if (correcta)
        {
            await CargarDatosAsync();
            LimpiarFormulario();
        }
    });

    [RelayCommand]
    private Task ArchivarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_productoService is null || SelectedProducto is null) return;

        var resultado = await _productoService.ArchivarAsync(SelectedProducto.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
        {
            await CargarDatosAsync();
            LimpiarFormulario();
        }
    });

    [RelayCommand]
    private Task RestaurarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_productoService is null || SelectedProducto is null) return;

        var resultado = await _productoService.RestaurarAsync(SelectedProducto.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
        {
            await CargarDatosAsync();
            LimpiarFormulario();
        }
    });

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        _limpiando = true;
        _cargados.Clear();
        ProductosItems.Clear();
        LimpiarFormulario();
        Busqueda = string.Empty;
        VerArchivados = false;
        _limpiando = false;
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

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, Cultura, out valor);
    }

    private static bool TryParseEntero(string? texto, out int valor)
    {
        var limpio = (texto ?? string.Empty).Trim();
        return int.TryParse(limpio, NumberStyles.Integer, Cultura, out valor);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
