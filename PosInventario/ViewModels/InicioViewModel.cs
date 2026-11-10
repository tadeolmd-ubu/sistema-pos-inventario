using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class InicioViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly VentaService? _ventaService;
    private readonly ProductoService? _productoService;
    private readonly CajaService? _cajaService;
    private readonly CompraService? _compraService;

    public InicioViewModel() { }

    public InicioViewModel(
        VentaService ventaService,
        ProductoService productoService,
        CajaService cajaService,
        CompraService compraService,
        SesionActual sesion)
    {
        _ventaService = ventaService;
        _productoService = productoService;
        _cajaService = cajaService;
        _compraService = compraService;
        EsAdministrador = sesion.EsAdministrador;
    }

    public ObservableCollection<Venta> UltimasVentas { get; } = [];
    public ObservableCollection<Producto> StockBajo { get; } = [];
    public ObservableCollection<Compra> UltimasCompras { get; } = [];

    [ObservableProperty] private int ventasDelDia;
    [ObservableProperty] private decimal totalDelDia;
    [ObservableProperty] private decimal ticketPromedio;
    [ObservableProperty] private decimal totalEfectivo;
    [ObservableProperty] private decimal totalTarjeta;
    [ObservableProperty] private decimal totalTransferencia;

    [ObservableProperty] private bool cajaAbierta;
    [ObservableProperty] private string estadoCajaTexto = "Cerrada";
    [ObservableProperty] private string usuarioCajaTexto = "—";
    [ObservableProperty] private string fechaAperturaCajaTexto = "—";
    [ObservableProperty] private decimal efectivoEsperado;

    [ObservableProperty] private int productosActivos;
    [ObservableProperty] private int productosStockBajo;
    [ObservableProperty] private bool esAdministrador;

    public string TotalDelDiaTexto => TotalDelDia.ToString("0.00", Cultura);

    public string TicketPromedioTexto => TicketPromedio.ToString("0.00", Cultura);

    public string TotalEfectivoTexto => TotalEfectivo.ToString("0.00", Cultura);

    public string TotalTarjetaTexto => TotalTarjeta.ToString("0.00", Cultura);

    public string TotalTransferenciaTexto => TotalTransferencia.ToString("0.00", Cultura);

    public string EfectivoEsperadoTexto => EfectivoEsperado.ToString("0.00", Cultura);

    public bool HayUltimasVentas => UltimasVentas.Count > 0;

    public bool HayStockBajo => StockBajo.Count > 0;

    public bool HayCompras => UltimasCompras.Count > 0;

    partial void OnTotalDelDiaChanged(decimal value)
        => OnPropertyChanged(nameof(TotalDelDiaTexto));

    partial void OnTicketPromedioChanged(decimal value)
        => OnPropertyChanged(nameof(TicketPromedioTexto));

    partial void OnTotalEfectivoChanged(decimal value)
        => OnPropertyChanged(nameof(TotalEfectivoTexto));

    partial void OnTotalTarjetaChanged(decimal value)
        => OnPropertyChanged(nameof(TotalTarjetaTexto));

    partial void OnTotalTransferenciaChanged(decimal value)
        => OnPropertyChanged(nameof(TotalTransferenciaTexto));

    partial void OnEfectivoEsperadoChanged(decimal value)
        => OnPropertyChanged(nameof(EfectivoEsperadoTexto));

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(CargarDatosAsync);

    private async Task CargarDatosAsync()
    {
        if (_ventaService is null || _productoService is null ||
            _cajaService is null || _compraService is null)
            return;

        var ventasHoy = await _ventaService.ObtenerDelDiaAsync();

        VentasDelDia = ventasHoy.Count;
        TotalDelDia = ventasHoy.Sum(v => v.Total);
        TotalEfectivo = ventasHoy
            .Where(v => v.MetodoPago == MetodoPago.Efectivo)
            .Sum(v => v.Total);
        TotalTarjeta = ventasHoy
            .Where(v => v.MetodoPago == MetodoPago.Tarjeta)
            .Sum(v => v.Total);
        TotalTransferencia = ventasHoy
            .Where(v => v.MetodoPago == MetodoPago.Transferencia)
            .Sum(v => v.Total);

        TicketPromedio = VentasDelDia > 0
            ? TotalDelDia / VentasDelDia
            : 0m;

        var recientes = await _ventaService.ObtenerRecientesAsync(10);
        ReemplazarItems(UltimasVentas, recientes);

        var caja = await _cajaService.ObtenerCajaAbiertaAsync();

        CajaAbierta = caja is not null;
        EstadoCajaTexto = caja is null ? "Cerrada" : "Abierta";
        UsuarioCajaTexto = caja?.UsuarioApertura?.Nombre ?? "—";
        FechaAperturaCajaTexto = caja is null
            ? "—"
            : caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura);

        EfectivoEsperado = caja is null
            ? 0m
            : await _cajaService.CalcularMontoEsperadoAsync(caja.Id) ?? caja.MontoInicial;

        if (EsAdministrador)
        {
            var productos = await _productoService.ObtenerTodosAsync();
            ProductosActivos = productos.Count;

            var stockBajo = await _productoService.ObtenerStockBajoAsync();
            ProductosStockBajo = stockBajo.Count;
            ReemplazarItems(StockBajo, stockBajo);

            var compras = await _compraService.ObtenerRecientesAsync(5);
            ReemplazarItems(UltimasCompras, compras);
        }
        else
        {
            ProductosActivos = 0;
            ProductosStockBajo = 0;
            StockBajo.Clear();
            UltimasCompras.Clear();
        }

        OnPropertyChanged(nameof(HayUltimasVentas));
        OnPropertyChanged(nameof(HayStockBajo));
        OnPropertyChanged(nameof(HayCompras));
    }

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        UltimasVentas.Clear();
        StockBajo.Clear();
        UltimasCompras.Clear();

        VentasDelDia = 0;
        TotalDelDia = 0;
        TicketPromedio = 0;
        TotalEfectivo = 0;
        TotalTarjeta = 0;
        TotalTransferencia = 0;

        CajaAbierta = false;
        EstadoCajaTexto = "Cerrada";
        UsuarioCajaTexto = "—";
        FechaAperturaCajaTexto = "—";
        EfectivoEsperado = 0;

        ProductosActivos = 0;
        ProductosStockBajo = 0;

        OnPropertyChanged(nameof(HayUltimasVentas));
        OnPropertyChanged(nameof(HayStockBajo));
        OnPropertyChanged(nameof(HayCompras));
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
