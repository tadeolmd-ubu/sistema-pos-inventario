using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class InventarioViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly ProductoService? _productoService;
    private readonly InventarioService? _inventarioService;
    private readonly List<Producto> _cargados = [];
    private bool _cargando;
    private bool _limpiando;

    public InventarioViewModel()
    {
        MotivoSeleccionado = MotivosInventario.Entrada[0];
        Paginacion.CambioSolicitado += CargarPaginaActual;
    }

    public InventarioViewModel(
        ProductoService productoService, InventarioService inventarioService)
        : this()
    {
        _productoService = productoService;
        _inventarioService = inventarioService;
    }

    public ObservableCollection<Producto> ProductosItems { get; } = [];
    public ObservableCollection<MovimientoInventario> MovimientosItems { get; } = [];

    public PaginacionViewModel Paginacion { get; } = new();

    public IReadOnlyList<TipoMovimiento> Tipos { get; } =
        [TipoMovimiento.Entrada, TipoMovimiento.Salida, TipoMovimiento.Ajuste];

    [ObservableProperty] private Producto? selectedProducto;
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private TipoMovimiento tipoSeleccionado = TipoMovimiento.Entrada;
    [ObservableProperty] private string motivoSeleccionado = string.Empty;
    [ObservableProperty] private string cantidadTexto = string.Empty;
    [ObservableProperty] private string stockContadoTexto = string.Empty;
    [ObservableProperty] private bool verTodosLosMovimientos;

    [ObservableProperty] private DateTimeOffset? desde =
        new(DateTime.Today.AddDays(-7));

    [ObservableProperty] private DateTimeOffset? hasta =
        new(DateTime.Today);

    public IReadOnlyList<string> MotivosDisponibles =>
        MotivosInventario.Para(TipoSeleccionado);

    public bool EsAjuste => TipoSeleccionado == TipoMovimiento.Ajuste;

    public bool EsPorPeso => SelectedProducto?.EsPorPeso == true;

    public string EtiquetaCantidad =>
        EsPorPeso ? "Cantidad (kg)" : "Cantidad";

    public string EtiquetaStockContado =>
        EsPorPeso ? "Stock contado (kg)" : "Stock contado";

    public string StockActualTexto => SelectedProducto is null
        ? "Selecciona un producto para mover el stock."
        : $"Stock actual: {SelectedProducto.StockTexto} · " +
          $"Mínimo: {SelectedProducto.StockMinimoTexto}";

    public string TituloMovimientos =>
        VerTodosLosMovimientos || SelectedProducto is null
            ? "Últimos movimientos"
            : $"Movimientos de {SelectedProducto.Nombre}";

    public bool SinMovimientos => Paginacion.TotalRegistros == 0;

    public int CantidadExportable => MovimientosItems.Count;

    public string NombreArchivoExportacion =>
        ExportadorCsv.NombreArchivo(
            "inventario", DesdeFecha, HastaFecha, Paginacion.PaginaActual);

    public string GenerarCsv() => ExportadorCsv.Inventario(MovimientosItems);

    private DateTime DesdeFecha =>
        (Desde ?? DateTimeOffset.Now.AddDays(-7)).Date;

    private DateTime HastaFecha =>
        (Hasta ?? DateTimeOffset.Now).Date;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(CargarDatosAsync);

    [RelayCommand]
    private Task BuscarMovimientosAsync() => CargarMovimientosAsync();

    private void CargarPaginaActual()
        => _ = CargarMovimientosAsync(reiniciarPagina: false);

    private async Task CargarDatosAsync()
    {
        if (_productoService is null) return;

        _cargando = true;

        try
        {
            _cargados.Clear();
            _cargados.AddRange(await _productoService.ObtenerTodosAsync());
            Filtrar();
        }
        finally
        {
            _cargando = false;
        }

        await CargarMovimientosAsync();
    }

    partial void OnVerTodosLosMovimientosChanged(bool value)
    {
        OnPropertyChanged(nameof(TituloMovimientos));
        _ = CargarMovimientosAsync();
    }

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnTipoSeleccionadoChanged(TipoMovimiento value)
    {
        MotivoSeleccionado = MotivosDisponibles.FirstOrDefault() ?? string.Empty;
        OnPropertyChanged(nameof(MotivosDisponibles));
        OnPropertyChanged(nameof(EsAjuste));
    }

    partial void OnSelectedProductoChanged(Producto? value)
    {
        OnPropertyChanged(nameof(StockActualTexto));
        OnPropertyChanged(nameof(EsPorPeso));
        OnPropertyChanged(nameof(EtiquetaCantidad));
        OnPropertyChanged(nameof(EtiquetaStockContado));
        OnPropertyChanged(nameof(TituloMovimientos));

        if (!_cargando && !_limpiando)
            _ = CargarMovimientosAsync();
    }

    [RelayCommand]
    private Task RegistrarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_inventarioService is null) return;

        if (SelectedProducto is null)
        {
            MensajeOperacion = Notificar("Selecciona un producto.");
            return;
        }

        if (string.IsNullOrWhiteSpace(MotivoSeleccionado))
        {
            MensajeOperacion = Notificar("Selecciona un motivo.");
            return;
        }

        bool correcta;
        string mensaje;

        if (EsAjuste)
        {
            if (!TryParseDecimal(StockContadoTexto, out var stockContado))
            {
                MensajeOperacion = Notificar("El stock contado no es un número válido.");
                return;
            }

            var resultado = await _inventarioService.RegistrarAjusteAsync(
                SelectedProducto.Id, stockContado, MotivoSeleccionado);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }
        else
        {
            if (!TryParseDecimal(CantidadTexto, out var cantidad))
            {
                MensajeOperacion = Notificar("La cantidad no es un número válido.");
                return;
            }

            var resultado = TipoSeleccionado == TipoMovimiento.Entrada
                ? await _inventarioService.RegistrarEntradaAsync(
                    SelectedProducto.Id, cantidad, MotivoSeleccionado)
                : await _inventarioService.RegistrarSalidaAsync(
                    SelectedProducto.Id, cantidad, MotivoSeleccionado);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }

        MensajeOperacion = Notificar(mensaje, correcta);

        if (!correcta) return;

        await CargarDatosAsync();
        LimpiarFormulario();
    });

    [RelayCommand]
    private void LimpiarFormulario()
    {
        SelectedProducto = null;
        TipoSeleccionado = TipoMovimiento.Entrada;
        MotivoSeleccionado = MotivosInventario.Entrada[0];
        CantidadTexto = string.Empty;
        StockContadoTexto = string.Empty;
    }

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        _limpiando = true;
        _cargados.Clear();
        ProductosItems.Clear();
        MovimientosItems.Clear();
        Paginacion.Reiniciar();
        OnPropertyChanged(nameof(SinMovimientos));
        OnPropertyChanged(nameof(CantidadExportable));
        Busqueda = string.Empty;
        Desde = new DateTimeOffset(DateTime.Today.AddDays(-7));
        Hasta = new DateTimeOffset(DateTime.Today);
        LimpiarFormulario();
        _limpiando = false;
    }

    private async Task CargarMovimientosAsync(bool reiniciarPagina = true)
    {
        if (_inventarioService is null) return;

        var desdeFecha = DesdeFecha;
        var hastaFecha = HastaFecha;

        if (hastaFecha < desdeFecha)
        {
            MensajeOperacion =
                "La fecha hasta no puede ser anterior a la fecha desde.";
            return;
        }

        if (reiniciarPagina)
            Paginacion.Reiniciar();

        try
        {
            var idProducto = VerTodosLosMovimientos ? null : SelectedProducto?.Id;

            var pagina = await _inventarioService
                .ObtenerMovimientosPorRangoPaginadosAsync(
                    desdeFecha,
                    hastaFecha,
                    idProducto,
                    Paginacion.PaginaActual,
                    Paginacion.TamanoPagina);

            ReemplazarItems(MovimientosItems, pagina.Items);
            Paginacion.EstablecerTotal(pagina.TotalRegistros);

            OnPropertyChanged(nameof(SinMovimientos));
            OnPropertyChanged(nameof(CantidadExportable));
        }
        catch (Exception ex)
        {
            MensajeOperacion = $"No se pudieron cargar los movimientos: {ex.Message}";
        }
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
