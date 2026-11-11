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

    public CajaViewModel() => ReiniciarPagos();

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

        ReiniciarPagos();
    }

    public ObservableCollection<MovimientoCaja> MovimientosItems { get; } = [];
    public ObservableCollection<Caja> HistorialItems { get; } = [];
    public ObservableCollection<LineaVentaItem> LineasVentaItems { get; } = [];
    public ObservableCollection<PagoVentaItem> PagosItems { get; } = [];

    [ObservableProperty] private Caja? cajaAbierta;
    [ObservableProperty] private decimal montoEsperado;
    [ObservableProperty] private string montoInicialTexto = string.Empty;

    [ObservableProperty] private string descuentoTexto = string.Empty;
    [ObservableProperty] private string busquedaRapida = string.Empty;

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

    public decimal SubtotalVenta => LineasVentaItems.Sum(l => l.Subtotal);

    public string SubtotalVentaTexto => SubtotalVenta.ToString("0.00", Cultura);

    public decimal Descuento =>
        TryParseDecimal(DescuentoTexto, out var descuento) && descuento > 0
            ? descuento
            : 0;

    public string DescuentoTextoMostrar => Descuento.ToString("0.00", Cultura);

    public decimal TotalVenta => Math.Max(0, SubtotalVenta - Descuento);

    public string TotalVentaTexto => TotalVenta.ToString("0.00", Cultura);

    public bool HayLineasVenta => LineasVentaItems.Count > 0;

    public decimal TotalPagado => PagosItems.Sum(p => p.Monto);

    public decimal Faltante => Math.Max(0m, TotalVenta - TotalPagado);

    public decimal Sobrante => Math.Max(0m, TotalPagado - TotalVenta);

    public bool HayFaltante => Faltante > 0;

    public bool HaySobrante => Sobrante > 0;

    public bool HayPagoEfectivo => PagosItems.Any(p => p.EsEfectivo);

    public decimal EfectivoRecibido => PagosItems
        .Where(p => p.EsEfectivo)
        .Sum(p => p.Recibido);

    public decimal Cambio => PagosItems
        .Where(p => p.EsEfectivo)
        .Sum(p => p.Cambio);

    public string TotalPagadoTexto => TotalPagado.ToString("0.00", Cultura);

    public string FaltanteTexto => Faltante.ToString("0.00", Cultura);

    public string SobranteTexto => Sobrante.ToString("0.00", Cultura);

    public string EfectivoRecibidoTexto =>
        EfectivoRecibido.ToString("0.00", Cultura);

    public string CambioTexto => Cambio.ToString("0.00", Cultura);

    public bool PagosValidos =>
        TotalVenta == 0 ||
        (PagosItems.Count > 0 &&
         PagosItems.All(p => p.Monto > 0) &&
         PagosItems.Select(p => p.Metodo).Distinct().Count() == PagosItems.Count &&
         TotalPagado == TotalVenta &&
         PagosItems.All(p => p.RecibidoValido));

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

    partial void OnDescuentoTextoChanged(string value) => NotificarVenta();

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
    {
        var efectivo = PagosItems.FirstOrDefault(p => p.EsEfectivo);

        if (efectivo is not null)
            efectivo.RecibidoTexto = denominacion;
    }

    /// <summary>
    /// Agrega al carrito el producto cuyo SKU coincide con el texto ingresado.
    /// Pensado para lectores de código de barras que escriben y presionan Enter.
    /// </summary>
    [RelayCommand]
    private Task AgregarPorCodigoAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_productoService is null) return;

        var codigo = (BusquedaRapida ?? string.Empty).Trim();

        if (codigo.Length == 0) return;

        if (CajaAbierta is null)
        {
            MensajeOperacion = "No hay una caja abierta para vender.";
            return;
        }

        var producto = await _productoService.ObtenerPorSkuAsync(codigo);

        if (producto is null)
        {
            MensajeOperacion = $"No se encontró un producto con el código {codigo}.";
            return;
        }

        AgregarProducto(producto);
        BusquedaRapida = string.Empty;
    });

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
        DescuentoTexto = string.Empty;
        BusquedaRapida = string.Empty;
        ReiniciarPagos();
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
            MensajeOperacion = Notificar("Agrega al menos un producto.");
            return;
        }

        if (Descuento > SubtotalVenta)
        {
            MensajeOperacion = Notificar("El descuento no puede superar el subtotal.");
            return;
        }

        if (!PagosValidos)
        {
            MensajeOperacion = Notificar(MensajePagosInvalido());
            return;
        }

        var lineas = LineasVentaItems
            .Select(l => new LineaVenta(l.Producto.Id, l.Cantidad))
            .ToList();

        var pagos = new List<LineaPago>();

        if (TotalVenta > 0)
        {
            pagos = PagosItems
                .Select(p => new LineaPago(
                    p.Metodo,
                    p.Monto,
                    p.EsEfectivo ? p.Recibido : null))
                .ToList();
        }

        var resultado = await _ventaService.RegistrarAsync(
            lineas, pagos, Descuento);

        if (!resultado.Correcta)
        {
            MensajeOperacion = Notificar(resultado.Mensaje);
            return;
        }

        MensajeOperacion = resultado.Mensaje;

        LineasVentaItems.Clear();
        DescuentoTexto = string.Empty;
        BusquedaRapida = string.Empty;
        ReiniciarPagos();
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
        if (_ventaService is null || _sesion is null) return null;

        var ventas = await _ventaService.ObtenerPorCajaAsync(caja.Id);
        return new CajaHistorialViewModel(caja, ventas, _ventaService, _sesion);
    }

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        LineasVentaItems.Clear();
        MovimientosItems.Clear();
        HistorialItems.Clear();

        MontoInicialTexto = string.Empty;
        DescuentoTexto = string.Empty;
        BusquedaRapida = string.Empty;

        CajaAbierta = null;
        MontoEsperado = 0;

        ReiniciarPagos();
        NotificarVenta();
    }

    private void NotificarVenta()
    {
        SincronizarPagoUnico();

        OnPropertyChanged(nameof(SubtotalVenta));
        OnPropertyChanged(nameof(SubtotalVentaTexto));
        OnPropertyChanged(nameof(Descuento));
        OnPropertyChanged(nameof(DescuentoTextoMostrar));
        OnPropertyChanged(nameof(TotalVenta));
        OnPropertyChanged(nameof(TotalVentaTexto));
        OnPropertyChanged(nameof(HayLineasVenta));

        NotificarPagos();
    }

    private void ReiniciarPagos()
    {
        PagosItems.Clear();
        PagosItems.Add(new PagoVentaItem(
            MetodoPago.Efectivo, TotalVenta, NotificarPagos, QuitarPago));

        NotificarPagos();
    }

    private void SincronizarPagoUnico()
    {
        if (PagosItems.Count != 1) return;

        PagosItems[0].MontoTexto = TotalVenta.ToString("0.00", Cultura);
    }

    [RelayCommand]
    private void AgregarPago()
    {
        if (PagosItems.Count >= 3)
        {
            MensajeOperacion = Notificar(
                "Ya agregaste todos los métodos de pago disponibles.");
            return;
        }

        var usados = PagosItems.Select(p => p.Metodo).ToHashSet();

        var metodo = new[]
        {
            MetodoPago.Efectivo,
            MetodoPago.Tarjeta,
            MetodoPago.Transferencia
        }.FirstOrDefault(m => !usados.Contains(m));

        if (usados.Contains(metodo))
        {
            MensajeOperacion = Notificar(
                "Ya agregaste todos los métodos de pago disponibles.");
            return;
        }

        PagosItems.Add(new PagoVentaItem(
            metodo, Faltante, NotificarPagos, QuitarPago));

        MensajeOperacion = string.Empty;
        NotificarPagos();
    }

    private void QuitarPago(PagoVentaItem pago)
    {
        if (PagosItems.Count <= 1)
        {
            MensajeOperacion = Notificar(
                "Debe quedar al menos un método de pago.");
            return;
        }

        PagosItems.Remove(pago);
        NotificarPagos();
    }

    private void NotificarPagos()
    {
        OnPropertyChanged(nameof(TotalPagado));
        OnPropertyChanged(nameof(TotalPagadoTexto));
        OnPropertyChanged(nameof(Faltante));
        OnPropertyChanged(nameof(FaltanteTexto));
        OnPropertyChanged(nameof(HayFaltante));
        OnPropertyChanged(nameof(Sobrante));
        OnPropertyChanged(nameof(SobranteTexto));
        OnPropertyChanged(nameof(HaySobrante));
        OnPropertyChanged(nameof(HayPagoEfectivo));
        OnPropertyChanged(nameof(EfectivoRecibido));
        OnPropertyChanged(nameof(EfectivoRecibidoTexto));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(CambioTexto));
        OnPropertyChanged(nameof(PagosValidos));
    }

    private string MensajePagosInvalido()
    {
        if (PagosItems.Count == 0)
            return "Agrega al menos un método de pago.";

        if (PagosItems.Any(p => p.Monto <= 0))
            return "El monto de cada pago debe ser mayor que 0.";

        if (PagosItems.Select(p => p.Metodo).Distinct().Count() != PagosItems.Count)
            return "No se puede repetir el mismo método de pago.";

        if (PagosItems.Any(p => !p.RecibidoValido))
            return "El efectivo recibido es menor al monto en efectivo.";

        if (TotalPagado < TotalVenta)
            return "Los pagos no cubren el total de la venta.";

        if (TotalPagado > TotalVenta)
            return "Los montos aplicados superan el total de la venta.";

        return "Revisa los pagos de la venta.";
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
