using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ProductosViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly ProductoService? _productoService;
    private readonly ImportacionDatosService? _importacionService;
    private readonly List<Producto> _cargados = [];
    private bool _limpiando;

    public ProductosViewModel() { }

    public ProductosViewModel(
        ProductoService productoService,
        ImportacionDatosService? importacionService = null)
    {
        _productoService = productoService;
        _importacionService = importacionService;
    }

    public ObservableCollection<Producto> ProductosItems { get; } = [];

    [ObservableProperty] private Producto? selectedProducto;
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private bool verArchivados;
    [ObservableProperty] private string sku = string.Empty;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string descripcion = string.Empty;
    [ObservableProperty] private UnidadMedida unidadMedidaSeleccionada =
        UnidadMedida.Pieza;
    [ObservableProperty] private string precioVentaTexto = string.Empty;
    [ObservableProperty] private string costoTexto = string.Empty;
    [ObservableProperty] private string stockMinimoTexto = string.Empty;

    public IReadOnlyList<UnidadMedida> Unidades { get; } =
        [UnidadMedida.Pieza, UnidadMedida.Kilogramo];

    public bool EsPorPeso =>
        UnidadMedidaSeleccionada == UnidadMedida.Kilogramo;

    public string EtiquetaPrecio =>
        EsPorPeso ? "Precio por kg" : "Precio de venta";

    public string EtiquetaCosto =>
        EsPorPeso ? "Costo por kg" : "Costo";

    public string EtiquetaStockMinimo =>
        EsPorPeso ? "Stock mínimo (kg)" : "Stock mínimo";

    public bool EsModoCreacion => SelectedProducto is null;

    public bool PuedeGuardar =>
        SelectedProducto is null || SelectedProducto.FechaArchivado is null;

    public bool PuedeArchivar =>
        SelectedProducto is not null && SelectedProducto.FechaArchivado is null;

    public bool PuedeRestaurar =>
        SelectedProducto is not null && SelectedProducto.FechaArchivado is not null;

    public string StockActual => SelectedProducto is null
        ? "El stock inicial es 0 y se carga desde Inventario."
        : $"Stock actual: {SelectedProducto.StockTexto}";

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

    /// <summary>
    /// Valida el archivo sin modificar la base y devuelve la ventana de vista
    /// previa. Devuelve null cuando el archivo no se puede leer.
    /// </summary>
    public async Task<ImportarDatosViewModel?> PrepararImportacionAsync(string ruta)
    {
        if (_importacionService is null)
            return null;

        var lectura = await _importacionService.LeerAsync(ruta);

        if (!lectura.Correcta || lectura.Valor is null)
        {
            MensajeOperacion = Notificar(lectura.Mensaje);
            return null;
        }

        return new ImportarDatosViewModel(_importacionService, lectura.Valor);
    }

    public static string GenerarPlantillaImportacion()
        => ImportadorCsv.Plantilla;

    public int CantidadExportable => ProductosItems.Count;

    public string NombreArchivoExportacion =>
        $"productos_{DateTime.Now:yyyy-MM-dd}.csv";

    /// <summary>
    /// El archivo usa las columnas de la plantilla de importación para que se
    /// pueda volver a importar sin editarlo.
    /// </summary>
    public string GenerarCsvProductos() => ExportadorCsv.Productos(ProductosItems);

    public void NotificarPlantillaGuardada(string ruta)
        => MensajeOperacion = $"Plantilla guardada en:\n{ruta}";

    public async Task RecargarTrasImportacionAsync(string mensaje)
    {
        MensajeOperacion = mensaje;
        await CargarDatosAsync();
    }

    partial void OnVerArchivadosChanged(bool value)
    {
        if (!_limpiando)
            _ = CargarAsync();
    }

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnUnidadMedidaSeleccionadaChanged(UnidadMedida value)
    {
        OnPropertyChanged(nameof(EsPorPeso));
        OnPropertyChanged(nameof(EtiquetaPrecio));
        OnPropertyChanged(nameof(EtiquetaCosto));
        OnPropertyChanged(nameof(EtiquetaStockMinimo));
    }

    partial void OnSelectedProductoChanged(Producto? value)
    {
        Sku = value?.Sku ?? string.Empty;
        Nombre = value?.Nombre ?? string.Empty;
        Descripcion = value?.Descripcion ?? string.Empty;
        UnidadMedidaSeleccionada = value?.UnidadMedida ?? UnidadMedida.Pieza;
        PrecioVentaTexto = (value?.PrecioVenta ?? 0).ToString("0.00", Cultura);
        CostoTexto = (value?.Costo ?? 0).ToString("0.00", Cultura);
        StockMinimoTexto = UnidadMedidaSeleccionada.Formatear(
            value?.StockMinimo ?? 0m);

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
        UnidadMedidaSeleccionada = UnidadMedida.Pieza;
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

        if (!TryParseDecimal(StockMinimoTexto, out var stockMinimo))
        {
            MensajeOperacion = Notificar("El stock mínimo no es un número válido.");
            return;
        }

        bool correcta;
        string mensaje;

        if (EsModoCreacion)
        {
            var resultado = await _productoService.AgregarAsync(
                Sku, Nombre, Descripcion, precioVenta, costo, stockMinimo,
                UnidadMedidaSeleccionada);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }
        else
        {
            var resultado = await _productoService.ActualizarAsync(
                SelectedProducto!.Id, Sku, Nombre, Descripcion,
                precioVenta, costo, stockMinimo, UnidadMedidaSeleccionada);

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

    /// <summary>
    /// Procesa un código escaneado en el formulario. Si el SKU existe, carga
    /// el producto; si no, limpia el formulario y deja el código listo para
    /// dar de alta el producto. Se invoca al presionar Enter en el campo SKU.
    /// </summary>
    public Task EscanearCodigoAsync(string? codigo) => EjecutarSeguroAsync(async () =>
    {
        if (_productoService is null) return;

        var normalizado = (codigo ?? string.Empty).Trim();

        if (normalizado.Length == 0) return;

        var producto = await _productoService.ObtenerPorSkuAsync(
            normalizado, incluirArchivados: true);

        if (producto is null)
        {
            LimpiarFormulario();
            Sku = normalizado;
            MensajeOperacion = "Código nuevo: completa los datos y guarda.";
            return;
        }

        // El producto debe estar en el listado para poder seleccionarlo.
        var requiereArchivados = producto.FechaArchivado is not null;

        if (VerArchivados != requiereArchivados)
        {
            _limpiando = true;
            VerArchivados = requiereArchivados;
            _limpiando = false;
            await CargarDatosAsync();
        }

        Busqueda = string.Empty;

        SelectedProducto = ProductosItems.FirstOrDefault(p => p.Id == producto.Id);

        MensajeOperacion = producto.FechaArchivado is null
            ? "Producto cargado."
            : "Producto archivado cargado. Puedes restaurarlo.";
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

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
