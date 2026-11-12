using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Reportes de administración: ventas por período, por método, productos más
/// vendidos y utilidad neta de devoluciones. Solo se muestra a administradores.
/// </summary>
public partial class ReportesViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly ReporteService? _reporteService;
    private bool _suprimirRecarga;

    public ReportesViewModel() { }

    public ReportesViewModel(ReporteService reporteService)
        => _reporteService = reporteService;

    public ObservableCollection<ReportePeriodo> Periodos { get; } = [];
    public ObservableCollection<ReporteMetodo> Metodos { get; } = [];
    public ObservableCollection<ReporteProducto> Productos { get; } = [];

    public IReadOnlyList<string> OpcionesAgrupacion { get; } =
        ["Día", "Semana", "Mes"];

    [ObservableProperty] private DateTimeOffset? desde =
        new(DateTime.Today.AddDays(-6));

    [ObservableProperty] private DateTimeOffset? hasta =
        new(DateTime.Today);

    [ObservableProperty] private string agrupacionSeleccionada = "Día";

    [ObservableProperty] private int cantidadVentas;
    [ObservableProperty] private decimal ventasBrutas;
    [ObservableProperty] private decimal devoluciones;
    [ObservableProperty] private decimal ventasNetas;
    [ObservableProperty] private decimal costo;
    [ObservableProperty] private decimal utilidad;
    [ObservableProperty] private decimal margen;
    [ObservableProperty] private decimal ticketPromedio;

    public string PeriodoTexto =>
        $"{DesdeFecha.ToString("dd/MM/yyyy", Cultura)} – " +
        HastaFecha.ToString("dd/MM/yyyy", Cultura);

    public string VentasNetasTexto =>
        VentasNetas.ToString("0.00", Cultura);

    public string VentasBrutasTexto =>
        VentasBrutas.ToString("0.00", Cultura);

    public string DevolucionesTexto =>
        Devoluciones.ToString("0.00", Cultura);

    public string CostoTexto => Costo.ToString("0.00", Cultura);

    public string UtilidadTexto => Utilidad.ToString("0.00", Cultura);

    public string MargenTexto =>
        $"{(Margen * 100m).ToString("0.0", Cultura)}%";

    public string TicketPromedioTexto =>
        TicketPromedio.ToString("0.00", Cultura);

    public bool HayDatos => Periodos.Count > 0;
    public bool SinDatos => !HayDatos;

    private DateTime DesdeFecha =>
        (Desde ?? DateTimeOffset.Now.AddDays(-6)).Date;

    private DateTime HastaFecha =>
        (Hasta ?? DateTimeOffset.Now).Date;

    private AgrupacionReporte AgrupacionActual =>
        AgrupacionSeleccionada switch
        {
            "Semana" => AgrupacionReporte.Semana,
            "Mes" => AgrupacionReporte.Mes,
            _ => AgrupacionReporte.Dia
        };

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(CargarReporteAsync);

    private async Task CargarReporteAsync()
    {
        var desde = DesdeFecha;
        var hasta = HastaFecha;

        if (hasta < desde)
        {
            MensajeOperacion = Notificar(
                "La fecha hasta no puede ser anterior a la fecha desde.");
            return;
        }

        if (_reporteService is null)
            return;

        var resultado = await _reporteService.ObtenerAsync(
            desde, hasta, AgrupacionActual);

        if (!resultado.Correcta || resultado.Valor is null)
        {
            MensajeOperacion = Notificar(resultado.Mensaje);
            return;
        }

        Aplicar(resultado.Valor);
    }

    private void Aplicar(ReporteAdministracion reporte)
    {
        ReemplazarItems(Periodos, reporte.Periodos);
        ReemplazarItems(Metodos, reporte.Metodos);
        ReemplazarItems(Productos, reporte.Productos);

        CantidadVentas = reporte.CantidadVentas;
        VentasBrutas = reporte.VentasBrutas;
        Devoluciones = reporte.Devoluciones;
        VentasNetas = reporte.VentasNetas;
        Costo = reporte.Costo;
        Utilidad = reporte.Utilidad;
        Margen = reporte.Margen;
        TicketPromedio = reporte.TicketPromedio;

        MensajeOperacion = string.Empty;
        NotificarResumen();
    }

    [RelayCommand]
    private Task VerHoyAsync()
        => AplicarRangoAsync(DateTime.Today, DateTime.Today);

    [RelayCommand]
    private Task VerUltimos7DiasAsync()
        => AplicarRangoAsync(DateTime.Today.AddDays(-6), DateTime.Today);

    [RelayCommand]
    private Task VerEsteMesAsync()
    {
        var hoy = DateTime.Today;
        return AplicarRangoAsync(new DateTime(hoy.Year, hoy.Month, 1), hoy);
    }

    [RelayCommand]
    private Task VerMesAnteriorAsync()
    {
        var primerDia = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        return AplicarRangoAsync(primerDia.AddMonths(-1), primerDia.AddDays(-1));
    }

    private async Task AplicarRangoAsync(DateTime desde, DateTime hasta)
    {
        Desde = new DateTimeOffset(desde);
        Hasta = new DateTimeOffset(hasta);

        await CargarAsync();
    }

    partial void OnDesdeChanged(DateTimeOffset? value)
        => OnPropertyChanged(nameof(PeriodoTexto));

    partial void OnHastaChanged(DateTimeOffset? value)
        => OnPropertyChanged(nameof(PeriodoTexto));

    partial void OnAgrupacionSeleccionadaChanged(string value)
    {
        if (_suprimirRecarga || _reporteService is null)
            return;

        _ = CargarAsync();
    }

    partial void OnCantidadVentasChanged(int value)
        => NotificarResumen();

    partial void OnVentasNetasChanged(decimal value)
        => OnPropertyChanged(nameof(VentasNetasTexto));

    partial void OnVentasBrutasChanged(decimal value)
        => OnPropertyChanged(nameof(VentasBrutasTexto));

    partial void OnDevolucionesChanged(decimal value)
        => OnPropertyChanged(nameof(DevolucionesTexto));

    partial void OnCostoChanged(decimal value)
        => OnPropertyChanged(nameof(CostoTexto));

    partial void OnUtilidadChanged(decimal value)
        => OnPropertyChanged(nameof(UtilidadTexto));

    partial void OnMargenChanged(decimal value)
        => OnPropertyChanged(nameof(MargenTexto));

    partial void OnTicketPromedioChanged(decimal value)
        => OnPropertyChanged(nameof(TicketPromedioTexto));

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        _suprimirRecarga = true;

        Periodos.Clear();
        Metodos.Clear();
        Productos.Clear();

        Desde = new DateTimeOffset(DateTime.Today.AddDays(-6));
        Hasta = new DateTimeOffset(DateTime.Today);
        AgrupacionSeleccionada = "Día";

        _suprimirRecarga = false;

        CantidadVentas = 0;
        VentasBrutas = 0;
        Devoluciones = 0;
        VentasNetas = 0;
        Costo = 0;
        Utilidad = 0;
        Margen = 0;
        TicketPromedio = 0;

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
        OnPropertyChanged(nameof(HayDatos));
        OnPropertyChanged(nameof(SinDatos));
        OnPropertyChanged(nameof(PeriodoTexto));
    }
}
