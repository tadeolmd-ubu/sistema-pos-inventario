using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class CajaViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly CajaService? _cajaService;
    private readonly VentaService? _ventaService;
    private readonly ProductoService? _productoService;
    private readonly SesionActual? _sesion;

    public CajaViewModel() { }

    public CajaViewModel(
        CajaService cajaService,
        VentaService ventaService,
        ProductoService productoService,
        SesionActual sesion)
    {
        _cajaService = cajaService;
        _ventaService = ventaService;
        _productoService = productoService;
        _sesion = sesion;
    }

    public ObservableCollection<MovimientoCaja> MovimientosItems { get; } = [];
    public ObservableCollection<Caja> HistorialItems { get; } = [];
    public ObservableCollection<LineaVentaItem> LineasVentaItems { get; } = [];

    public IReadOnlyList<MetodoPago> MetodosPago { get; } =
        [MetodoPago.Efectivo, MetodoPago.Tarjeta, MetodoPago.Transferencia];

    [ObservableProperty] private Caja? cajaAbierta;
    [ObservableProperty] private decimal montoEsperado;
    [ObservableProperty] private string montoInicialTexto = string.Empty;

    [ObservableProperty] private MetodoPago metodoPagoSeleccionado = MetodoPago.Efectivo;
    [ObservableProperty] private string montoRecibidoTexto = string.Empty;

    /// <summary>
    /// La vista abre la ventana de selección de productos.
    /// </summary>
    public event Action<SeleccionarProductoViewModel>? SeleccionarProductoSolicitado;

    /// <summary>
    /// La vista pide confirmación antes de quitar una línea.
    /// </summary>
    public event Action<LineaVentaItem>? EliminarLineaSolicitada;

    /// <summary>
    /// La vista abre el ticket de la venta registrada.
    /// </summary>
    public event Action<Venta>? TicketSolicitado;

    /// <summary>
    /// La vista abre la ventana para registrar un ingreso o retiro.
    /// </summary>
    public event Action<RegistrarMovimientoCajaViewModel>? RegistrarMovimientoSolicitado;

    /// <summary>
    /// La vista abre la ventana de cierre de caja.
    /// </summary>
    public event Action<CerrarCajaViewModel>? CerrarCajaSolicitado;

    public bool HayCajaAbierta => CajaAbierta is not null;

    public bool SinCajaAbierta => CajaAbierta is null;

    public string UsuarioAperturaTexto =>
        CajaAbierta?.UsuarioApertura?.Nombre ?? "—";

    public string FechaAperturaTexto =>
        CajaAbierta?.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—";

    public string MontoInicialMostrar =>
        (CajaAbierta?.MontoInicial ?? 0).ToString("0.00", Cultura);

    public string MontoEsperadoTexto =>
        MontoEsperado.ToString("0.00", Cultura);

    public decimal TotalVenta => LineasVentaItems.Sum(l => l.Subtotal);

    public string TotalVentaTexto => TotalVenta.ToString("0.00", Cultura);

    public bool HayLineasVenta => LineasVentaItems.Count > 0;

    public bool EsPagoEfectivo =>
        MetodoPagoSeleccionado == MetodoPago.Efectivo;

    public event Action<CancelarVentaViewModel>? CancelarVentaSolicitado;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(CargarDatosAsync);

    private async Task CargarDatosAsync()
    {
        if (_cajaService is null) return;

        var caja = await _cajaService.ObtenerCajaAbiertaAsync();
        CajaAbierta = caja;

        if (caja is null)
        {
            MovimientosItems.Clear();
            MontoEsperado = 0;

            if (LineasVentaItems.Count > 0)
                LimpiarVenta();
        }
        else
        {
            var movimientos = await _cajaService.ObtenerMovimientosAsync(caja.Id);
            ReemplazarItems(MovimientosItems, movimientos);

            MontoEsperado = await _cajaService.CalcularMontoEsperadoAsync(caja.Id)
                ?? caja.MontoInicial;
        }

        var historial = await _cajaService.ObtenerHistorialAsync();
        ReemplazarItems(HistorialItems, historial);
    }

    partial void OnCajaAbiertaChanged(Caja? value)
    {
        OnPropertyChanged(nameof(HayCajaAbierta));
        OnPropertyChanged(nameof(SinCajaAbierta));
        OnPropertyChanged(nameof(UsuarioAperturaTexto));
        OnPropertyChanged(nameof(FechaAperturaTexto));
        OnPropertyChanged(nameof(MontoInicialMostrar));
    }

    partial void OnMontoEsperadoChanged(decimal value)
        => OnPropertyChanged(nameof(MontoEsperadoTexto));

    partial void OnMetodoPagoSeleccionadoChanged(MetodoPago value)
        => OnPropertyChanged(nameof(EsPagoEfectivo));

    [RelayCommand]
    private Task AbrirAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_cajaService is null) return;

        if (!TryParseDecimal(MontoInicialTexto, out var montoInicial) || montoInicial < 0)
        {
            MensajeOperacion = Notificar("El monto inicial debe ser un número mayor o igual a 0.");
            return;
        }

        var resultado = await _cajaService.AbrirAsync(montoInicial);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (!resultado.Correcta) return;

        MontoInicialTexto = string.Empty;
        await CargarDatosAsync();
    });

    [RelayCommand]
    private void SeleccionarProductos()
    {
        if (_productoService is null) return;

        SeleccionarProductoSolicitado?.Invoke(
            new SeleccionarProductoViewModel(_productoService));
    }

    [RelayCommand]
    private void SeleccionarDenominacion(string denominacion)
        => MontoRecibidoTexto = denominacion;

    /// <summary>
    /// La llama la vista cuando la ventana de selección devuelve un producto.
    /// </summary>
    public void AgregarProducto(Producto producto)
    {
        if (producto.Stock <= 0)
        {
            MensajeOperacion = Notificar("El producto no tiene stock.");
            return;
        }

        var existente = LineasVentaItems
            .FirstOrDefault(l => l.Producto.Id == producto.Id);

        if (existente is not null)
        {
            if (existente.Cantidad >= producto.Stock)
            {
                MensajeOperacion = Notificar("No hay más stock disponible de ese producto.");
                return;
            }

            existente.Fusionar(1);
        }
        else
        {
            LineasVentaItems.Add(new LineaVentaItem(
                producto, 1, AumentarLinea, DisminuirLinea, SolicitarEliminarLinea));
        }

        NotificarVenta();
        MensajeOperacion = string.Empty;
    }

    /// <summary>
    /// La llama la vista después de confirmar la eliminación.
    /// </summary>
    public void EliminarLinea(LineaVentaItem linea)
    {
        LineasVentaItems.Remove(linea);
        NotificarVenta();
    }

    private void AumentarLinea(LineaVentaItem linea)
    {
        if (linea.Cantidad >= linea.Producto.Stock)
        {
            MensajeOperacion = Notificar("No hay más stock disponible de ese producto.");
            return;
        }

        linea.Aumentar();
        NotificarVenta();
    }

    private void DisminuirLinea(LineaVentaItem linea)
    {
        if (linea.Cantidad <= 1)
        {
            SolicitarEliminarLinea(linea);
            return;
        }

        linea.Disminuir();
        NotificarVenta();
    }

    private void SolicitarEliminarLinea(LineaVentaItem linea)
        => EliminarLineaSolicitada?.Invoke(linea);

    [RelayCommand]
    private void LimpiarVenta()
    {
        LineasVentaItems.Clear();
        MontoRecibidoTexto = string.Empty;
        MetodoPagoSeleccionado = MetodoPago.Efectivo;
        NotificarVenta();
    }

    [RelayCommand]
    private Task CobrarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_ventaService is null) return;

        if (CajaAbierta is null)
        {
            MensajeOperacion = Notificar("No hay una caja abierta para cobrar.");
            return;
        }

        if (LineasVentaItems.Count == 0)
        {
            MensajeOperacion = Notificar("Agregá al menos un producto.");
            return;
        }

        var montoRecibido = TotalVenta;

        if (EsPagoEfectivo)
        {
            if (!TryParseDecimal(MontoRecibidoTexto, out montoRecibido))
            {
                MensajeOperacion = Notificar("El monto recibido no es un número válido.");
                return;
            }

            if (montoRecibido < TotalVenta)
            {
                MensajeOperacion = Notificar("El monto recibido es menor al total de la venta.");
                return;
            }
        }

        var lineas = LineasVentaItems
            .Select(l => new LineaVenta(l.Producto.Id, l.Cantidad))
            .ToList();

        var resultado = await _ventaService.RegistrarAsync(
            lineas, MetodoPagoSeleccionado, montoRecibido);

        if (!resultado.Correcta)
        {
            MensajeOperacion = Notificar(resultado.Mensaje);
            return;
        }

        MensajeOperacion = resultado.Mensaje;

        LineasVentaItems.Clear();
        MontoRecibidoTexto = string.Empty;
        MetodoPagoSeleccionado = MetodoPago.Efectivo;
        NotificarVenta();

        await CargarDatosAsync();

        if (resultado.Valor is not null)
            TicketSolicitado?.Invoke(resultado.Valor);
    });

    [RelayCommand]
    private void SolicitarRegistrarMovimiento()
    {
        if (_cajaService is null || CajaAbierta is null) return;

        RegistrarMovimientoSolicitado?.Invoke(
            new RegistrarMovimientoCajaViewModel(_cajaService, MontoEsperado));
    }

    [RelayCommand]
    private void SolicitarCancelarVenta()
    {
        if (_ventaService is null || _sesion is null || CajaAbierta is null) return;

        CancelarVentaSolicitado?.Invoke(
            new CancelarVentaViewModel(_ventaService, _sesion, CajaAbierta.Id));
    }

    [RelayCommand]
    private Task SolicitarCerrarCajaAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_cajaService is null || CajaAbierta is null) return;

        var resumen = await _cajaService.ObtenerResumenPagosAsync(CajaAbierta.Id);

        CerrarCajaSolicitado?.Invoke(
            new CerrarCajaViewModel(_cajaService, CajaAbierta, MontoEsperado, resumen));
    });

    /// <summary>
    /// La vista lo usa al seleccionar una caja del historial.
    /// </summary>
    public async Task<CajaHistorialViewModel?> CrearHistorialViewModelAsync(Caja caja)
    {
        if (_ventaService is null) return null;

        var ventas = await _ventaService.ObtenerPorCajaAsync(caja.Id);
        return new CajaHistorialViewModel(caja, ventas);
    }

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        LineasVentaItems.Clear();
        MovimientosItems.Clear();
        HistorialItems.Clear();

        MontoInicialTexto = string.Empty;
        MontoRecibidoTexto = string.Empty;
        MetodoPagoSeleccionado = MetodoPago.Efectivo;

        CajaAbierta = null;
        MontoEsperado = 0;

        OnPropertyChanged(nameof(TotalVenta));
        OnPropertyChanged(nameof(TotalVentaTexto));
        OnPropertyChanged(nameof(HayLineasVenta));
    }

    private void NotificarVenta()
    {
        OnPropertyChanged(nameof(TotalVenta));
        OnPropertyChanged(nameof(TotalVentaTexto));
        OnPropertyChanged(nameof(HayLineasVenta));
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
