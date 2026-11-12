using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Opción del filtro de cajero. <see cref="Id"/> nulo representa "todos".
/// </summary>
public sealed record OpcionCajero(int? Id, string Nombre);

/// <summary>
/// Historial global de ventas para administradores: permite buscar por rango
/// de fechas, ticket, cajero y método de pago, y abrir el ticket de cualquier
/// venta.
/// </summary>
public partial class VentasViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private static readonly OpcionCajero TodosLosCajeros =
        new(null, "Todos los cajeros");

    private readonly VentaService? _ventaService;
    private decimal _totalVendido;

    public VentasViewModel()
    {
        Paginacion.CambioSolicitado += CargarPaginaActual;
        Cajeros.Add(TodosLosCajeros);
        CajeroSeleccionado = TodosLosCajeros;
    }

    public VentasViewModel(VentaService ventaService)
        : this()
        => _ventaService = ventaService;

    public ObservableCollection<Venta> VentasItems { get; } = [];
    public ObservableCollection<OpcionCajero> Cajeros { get; } = [];

    public PaginacionViewModel Paginacion { get; } = new();

    public IReadOnlyList<string> OpcionesMetodo { get; } =
        ["Todos", "Efectivo", "Tarjeta", "Transferencia", "Mixto"];

    [ObservableProperty] private DateTimeOffset? desde =
        new(DateTime.Today.AddDays(-7));

    [ObservableProperty] private DateTimeOffset? hasta =
        new(DateTime.Today);

    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private string metodoSeleccionado = "Todos";
    [ObservableProperty] private OpcionCajero? cajeroSeleccionado;
    [ObservableProperty] private bool verAnuladas;
    [ObservableProperty] private Venta? selectedVenta;

    public bool HayVentas => VentasItems.Count > 0;

    public bool SinVentas => !HayVentas;

    public int CantidadVentas => Paginacion.TotalRegistros;

    public string TotalVendidoTexto => _totalVendido.ToString("0.00", Cultura);

    public bool PuedeVerTicket => SelectedVenta is not null;

    public int CantidadExportable => VentasItems.Count;

    public string NombreArchivoExportacion =>
        ExportadorCsv.NombreArchivo(
            "ventas", DesdeFecha, HastaFecha, Paginacion.PaginaActual);

    public string GenerarCsv() => ExportadorCsv.Ventas(VentasItems);

    private DateTime DesdeFecha =>
        (Desde ?? DateTimeOffset.Now.AddDays(-7)).Date;

    private DateTime HastaFecha =>
        (Hasta ?? DateTimeOffset.Now).Date;

    private MetodoPago? MetodoFiltro => MetodoSeleccionado switch
    {
        "Efectivo" => MetodoPago.Efectivo,
        "Tarjeta" => MetodoPago.Tarjeta,
        "Transferencia" => MetodoPago.Transferencia,
        "Mixto" => MetodoPago.Mixto,
        _ => null
    };

    /// <summary>
    /// Solo se puede devolver una venta completada con unidades pendientes.
    /// El servicio valida que el usuario sea el vendedor o un administrador.
    /// </summary>
    public bool PuedeDevolverSeleccionada =>
        SelectedVenta is not null &&
        SelectedVenta.Estado == EstadoVenta.Completada &&
        SelectedVenta.Detalles.Any(d => d.CantidadPendiente > 0);

    /// <summary>
    /// La vista abre el ticket de la venta seleccionada.
    /// </summary>
    public event Action<Venta>? TicketSolicitado;

    /// <summary>
    /// La vista abre la ventana de devolución de la venta seleccionada.
    /// </summary>
    public event Action<Venta>? DevolucionSolicitada;

    public DevolverVentaViewModel? CrearDevolucionViewModel(Venta venta)
        => _ventaService is null
            ? null
            : new DevolverVentaViewModel(_ventaService, venta);

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(
        () => CargarPaginaAsync(reiniciarPagina: true));

    private void CargarPaginaActual()
        => _ = EjecutarSeguroAsync(() => CargarPaginaAsync(reiniciarPagina: false));

    private async Task CargarPaginaAsync(bool reiniciarPagina)
    {
        if (_ventaService is null) return;

        var desdeFecha = DesdeFecha;
        var hastaFecha = HastaFecha;

        if (hastaFecha < desdeFecha)
        {
            MensajeOperacion = Notificar(
                "La fecha hasta no puede ser anterior a la fecha desde.");
            return;
        }

        if (reiniciarPagina)
        {
            Paginacion.Reiniciar();
            await CargarCajerosAsync();
        }

        var pagina = await _ventaService.ObtenerHistorialPaginadoAsync(
            desdeFecha,
            hastaFecha,
            Busqueda,
            VerAnuladas,
            Paginacion.PaginaActual,
            Paginacion.TamanoPagina,
            MetodoFiltro,
            CajeroSeleccionado?.Id);

        ReemplazarItems(VentasItems, pagina.Items);
        _totalVendido = pagina.Total;
        Paginacion.EstablecerTotal(pagina.TotalRegistros);

        if (SelectedVenta is not null && !VentasItems.Contains(SelectedVenta))
            SelectedVenta = null;

        NotificarResumen();
    }

    /// <summary>
    /// Refresca la lista de cajeros con ventas y conserva la selección actual
    /// cuando el usuario ya había elegido uno.
    /// </summary>
    private async Task CargarCajerosAsync()
    {
        if (_ventaService is null) return;

        var seleccionado = CajeroSeleccionado?.Id;
        var usuarios = await _ventaService.ObtenerCajerosConVentasAsync();

        Cajeros.Clear();
        Cajeros.Add(TodosLosCajeros);

        foreach (var usuario in usuarios)
            Cajeros.Add(new OpcionCajero(usuario.Id, usuario.Nombre));

        CajeroSeleccionado = Cajeros
            .FirstOrDefault(c => c.Id == seleccionado) ?? TodosLosCajeros;
    }

    [RelayCommand(CanExecute = nameof(PuedeVerTicket))]
    private void VerTicket()
    {
        if (SelectedVenta is not null)
            TicketSolicitado?.Invoke(SelectedVenta);
    }

    [RelayCommand(CanExecute = nameof(PuedeDevolverSeleccionada))]
    private void Devolver()
    {
        if (SelectedVenta is not null)
            DevolucionSolicitada?.Invoke(SelectedVenta);
    }

    partial void OnSelectedVentaChanged(Venta? value)
    {
        OnPropertyChanged(nameof(PuedeVerTicket));
        VerTicketCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PuedeDevolverSeleccionada));
        DevolverCommand.NotifyCanExecuteChanged();
    }

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        Paginacion.Reiniciar();
        VentasItems.Clear();
        Cajeros.Clear();
        Busqueda = string.Empty;
        MetodoSeleccionado = "Todos";
        CajeroSeleccionado = null;
        VerAnuladas = false;
        SelectedVenta = null;
        Desde = new DateTimeOffset(DateTime.Today.AddDays(-7));
        Hasta = new DateTimeOffset(DateTime.Today);
        _totalVendido = 0m;

        NotificarResumen();
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();

        foreach (var item in origen)
            destino.Add(item);
    }

    private void NotificarResumen()
    {
        OnPropertyChanged(nameof(HayVentas));
        OnPropertyChanged(nameof(SinVentas));
        OnPropertyChanged(nameof(CantidadVentas));
        OnPropertyChanged(nameof(TotalVendidoTexto));
    }
}
