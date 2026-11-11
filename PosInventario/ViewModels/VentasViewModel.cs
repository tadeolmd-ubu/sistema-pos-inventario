using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Historial global de ventas para administradores: permite buscar por rango
/// de fechas, ticket o cajero, y abrir el ticket de cualquier venta.
/// </summary>
public partial class VentasViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly VentaService? _ventaService;
    private readonly List<Venta> _cargadas = [];

    public VentasViewModel() { }

    public VentasViewModel(VentaService ventaService)
        => _ventaService = ventaService;

    public ObservableCollection<Venta> VentasItems { get; } = [];

    [ObservableProperty] private DateTimeOffset? desde =
        new(DateTime.Today.AddDays(-7));

    [ObservableProperty] private DateTimeOffset? hasta =
        new(DateTime.Today);

    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private bool verAnuladas;
    [ObservableProperty] private Venta? selectedVenta;

    public bool HayVentas => VentasItems.Count > 0;

    public bool SinVentas => !HayVentas;

    public int CantidadVentas => VentasItems.Count;

    public string TotalVendidoTexto =>
        VentasItems
            .Where(v => v.Estado == EstadoVenta.Completada)
            .Sum(v => v.TotalNeto)
            .ToString("0.00", Cultura);

    public bool PuedeVerTicket => SelectedVenta is not null;

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
    public Task CargarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_ventaService is null) return;

        var desdeFecha = (Desde ?? DateTimeOffset.Now.AddDays(-7)).Date;
        var hastaFecha = (Hasta ?? DateTimeOffset.Now).Date;

        if (hastaFecha < desdeFecha)
        {
            MensajeOperacion = Notificar(
                "La fecha hasta no puede ser anterior a la fecha desde.");
            return;
        }

        _cargadas.Clear();
        _cargadas.AddRange(
            await _ventaService.ObtenerHistorialAsync(desdeFecha, hastaFecha));

        Filtrar();
    });

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

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnVerAnuladasChanged(bool value) => Filtrar();

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

        _cargadas.Clear();
        VentasItems.Clear();
        Busqueda = string.Empty;
        VerAnuladas = false;
        SelectedVenta = null;
        Desde = new DateTimeOffset(DateTime.Today.AddDays(-7));
        Hasta = new DateTimeOffset(DateTime.Today);

        NotificarResumen();
    }

    private void Filtrar()
    {
        var texto = (Busqueda ?? string.Empty).Trim();

        IEnumerable<Venta> filtradas = _cargadas;

        if (!VerAnuladas)
            filtradas = filtradas.Where(v => v.Estado == EstadoVenta.Completada);

        if (texto.Length > 0)
        {
            filtradas = filtradas.Where(v =>
                v.Id.ToString().Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                $"V-{v.Id:000000}".Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                (v.Usuario?.Nombre ?? string.Empty)
                    .Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                (v.Usuario?.NombreUsuario ?? string.Empty)
                    .Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        VentasItems.Clear();

        foreach (var venta in filtradas)
            VentasItems.Add(venta);

        if (SelectedVenta is not null && !VentasItems.Contains(SelectedVenta))
            SelectedVenta = null;

        NotificarResumen();
    }

    private void NotificarResumen()
    {
        OnPropertyChanged(nameof(HayVentas));
        OnPropertyChanged(nameof(SinVentas));
        OnPropertyChanged(nameof(CantidadVentas));
        OnPropertyChanged(nameof(TotalVendidoTexto));
    }
}
