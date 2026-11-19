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
    private readonly ClienteService? _clienteService;
    private readonly SesionActual? _sesion;
    private readonly RespaldoAutomaticoService? _respaldoAutomatico;

    public CajaViewModel() { }

    public CajaViewModel(
        CajaService cajaService,
        VentaService ventaService,
        ProductoService productoService,
        SesionActual sesion,
        RespaldoAutomaticoService? respaldoAutomatico = null,
        ClienteService? clienteService = null)
    {
        _cajaService = cajaService;
        _ventaService = ventaService;
        _productoService = productoService;
        _sesion = sesion;
        _respaldoAutomatico = respaldoAutomatico;
        _clienteService = clienteService;
    }

    public ObservableCollection<MovimientoCaja> MovimientosItems { get; } = [];
    public ObservableCollection<Caja> HistorialItems { get; } = [];
    public ObservableCollection<LineaVentaItem> LineasVentaItems { get; } = [];
    public ObservableCollection<OpcionCliente> ClientesItems { get; } = [];

    public IReadOnlyList<MetodoPago> MetodosPago { get; } =
    [
        MetodoPago.Efectivo,
        MetodoPago.Tarjeta,
        MetodoPago.Transferencia,
        MetodoPago.Credito,
        MetodoPago.Mixto
    ];

    [ObservableProperty] private Caja? cajaAbierta;
    [ObservableProperty] private decimal montoEsperado;
    [ObservableProperty] private string montoInicialTexto = string.Empty;

    [ObservableProperty] private MetodoPago metodoPagoSeleccionado = MetodoPago.Efectivo;
    [ObservableProperty] private string montoEfectivoTexto = string.Empty;
    [ObservableProperty] private string montoTarjetaTexto = string.Empty;
    [ObservableProperty] private string montoTransferenciaTexto = string.Empty;
    [ObservableProperty] private string descuentoTexto = string.Empty;
    [ObservableProperty] private string busquedaRapida = string.Empty;
    [ObservableProperty] private OpcionCliente? clienteSeleccionado;

    /// <summary>
    /// La vista pregunta el peso cuando se agrega o edita un producto por peso.
    /// </summary>
    public delegate Task<decimal?> PedirCantidadPesoHandler(
        Producto producto, decimal cantidadInicial);

    public event PedirCantidadPesoHandler? CantidadPesoPreguntada;

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

    public bool EsPagoEfectivo => MetodoPagoSeleccionado == MetodoPago.Efectivo;

    public bool EsPagoMixto => MetodoPagoSeleccionado == MetodoPago.Mixto;

    public bool EsPagoCredito => MetodoPagoSeleccionado == MetodoPago.Credito;

    public bool MuestraEfectivo => EsPagoEfectivo || EsPagoMixto;

    public bool EsPublico => ClienteSeleccionado?.Id is null;

    public string ClienteSeleccionadoTexto =>
        ClienteSeleccionado?.Nombre ?? "Público";

    public string CreditoDisponibleTexto =>
        ClienteSeleccionado?.DisponibleTexto ?? "0.00";

    public decimal MontoEfectivo => Parse(MontoEfectivoTexto);

    public decimal MontoTarjeta => Parse(MontoTarjetaTexto);

    public decimal MontoTransferencia => Parse(MontoTransferenciaTexto);

    /// <summary>
    /// Parte del efectivo que se aplica a la venta. En mixto es el total menos
    /// lo pagado con tarjeta y transferencia; el resto del efectivo es cambio.
    /// </summary>
    public decimal EfectivoAplicado =>
        TotalVenta == 0
            ? 0
            : EsPagoMixto
                ? Math.Max(0, TotalVenta - MontoTarjeta - MontoTransferencia)
                : EsPagoEfectivo
                    ? TotalVenta
                    : 0;

    public decimal EfectivoRecibido => MuestraEfectivo ? MontoEfectivo : 0;

    public decimal TotalRecibido => EsPagoCredito
        ? 0m
        : EsPagoMixto
            ? MontoEfectivo + MontoTarjeta + MontoTransferencia
            : EsPagoEfectivo
                ? MontoEfectivo
                : TotalVenta;

    public decimal TotalPagado => EsPagoCredito
        ? 0m
        : EsPagoMixto
            ? EfectivoAplicado + MontoTarjeta + MontoTransferencia
            : TotalVenta;

    public decimal Faltante => EsPagoCredito
        ? 0m
        : Math.Max(0, TotalVenta - TotalRecibido);

    public decimal Cambio => MuestraEfectivo
        ? Math.Max(0, EfectivoRecibido - EfectivoAplicado)
        : 0;

    public bool HayFaltante => Faltante > 0;

    public bool HayCambio => Cambio > 0;

    public string TotalRecibidoTexto => TotalRecibido.ToString("0.00", Cultura);

    public string TotalPagadoTexto => TotalPagado.ToString("0.00", Cultura);

    public string FaltanteTexto => Faltante.ToString("0.00", Cultura);

    public string CambioTexto => Cambio.ToString("0.00", Cultura);

    public bool PagosValidos => EsPagoCredito
        ? ClienteSeleccionado?.Id is not null &&
          TotalVenta <= (ClienteSeleccionado?.Disponible ?? 0m)
        : TotalVenta == 0 ||
          (EsPagoEfectivo
              ? MontoEfectivo >= TotalVenta
              : EsPagoMixto
                  ? MontoTarjeta >= 0 &&
                    MontoTransferencia >= 0 &&
                    MontoEfectivo >= 0 &&
                    MontoTarjeta + MontoTransferencia <= TotalVenta &&
                    TotalRecibido >= TotalVenta
                  : true);

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

        await CargarClientesAsync();
    }

    private async Task CargarClientesAsync()
    {
        if (_clienteService is null)
            return;

        var idAnterior = ClienteSeleccionado?.Id;
        var clientes = await _clienteService.ObtenerActivosAsync();

        ClientesItems.Clear();
        ClientesItems.Add(new OpcionCliente(null, "Público general"));

        foreach (var cliente in clientes)
        {
            ClientesItems.Add(new OpcionCliente(
                cliente.Id,
                cliente.Nombre,
                cliente.LimiteCredito,
                cliente.SaldoActual));
        }

        ClienteSeleccionado = ClientesItems
            .FirstOrDefault(c => c.Id == idAnterior)
            ?? ClientesItems[0];
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

    partial void OnMetodoPagoSeleccionadoChanged(MetodoPago value)
        => NotificarPagos();

    partial void OnClienteSeleccionadoChanged(OpcionCliente? value)
        => NotificarPagos();

    partial void OnMontoEfectivoTextoChanged(string value) => NotificarPagos();

    partial void OnMontoTarjetaTextoChanged(string value) => NotificarPagos();

    partial void OnMontoTransferenciaTextoChanged(string value) => NotificarPagos();

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
        => MontoEfectivoTexto = denominacion;

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

        await AgregarProductoAsync(producto);
        BusquedaRapida = string.Empty;
    });

    /// <summary>
    /// La llama la vista cuando la ventana de selección devuelve un producto.
    /// Los productos por peso piden la cantidad antes de agregarse.
    /// </summary>
    public async Task AgregarProductoAsync(Producto producto)
    {
        if (producto.Stock <= 0m)
        {
            MensajeOperacion = Notificar("El producto no tiene stock.");
            return;
        }

        if (producto.EsPorPeso)
        {
            var cantidad = await PedirCantidadPesoAsync(producto, 1m);

            if (cantidad is null)
                return;

            AgregarProductoConCantidad(producto, cantidad.Value);
            return;
        }

        AgregarProductoConCantidad(producto, 1m);
    }

    private async Task<decimal?> PedirCantidadPesoAsync(
        Producto producto, decimal cantidadInicial)
    {
        var handler = CantidadPesoPreguntada;

        return handler is null
            ? null
            : await handler(producto, cantidadInicial);
    }

    private void AgregarProductoConCantidad(
        Producto producto, decimal cantidad)
    {
        var etiqueta = producto.EsPorPeso ? "El peso" : "La cantidad";
        var error = Cantidades.ErrorCantidad(
            cantidad, producto.UnidadMedida, etiqueta);

        if (error is not null)
        {
            MensajeOperacion = Notificar(error);
            return;
        }

        cantidad = Cantidades.Normalizar(cantidad);

        var existente = LineasVentaItems
            .FirstOrDefault(l => l.Producto.Id == producto.Id);

        var nuevaCantidad = (existente?.Cantidad ?? 0m) + cantidad;

        if (nuevaCantidad > producto.Stock)
        {
            MensajeOperacion = Notificar(
                producto.EsPorPeso
                    ? $"El peso no puede superar el stock disponible " +
                      $"({producto.StockTexto})."
                    : "No hay más stock disponible de ese producto.");
            return;
        }

        if (existente is not null)
            existente.Fusionar(cantidad);
        else
            LineasVentaItems.Add(new LineaVentaItem(
                producto,
                cantidad,
                AumentarLinea,
                DisminuirLinea,
                SolicitarEliminarLinea,
                EditarLineaAsync));

        NotificarVenta();
        MensajeOperacion = string.Empty;
    }

    private async Task EditarLineaAsync(LineaVentaItem linea)
    {
        var cantidad = await PedirCantidadPesoAsync(
            linea.Producto, linea.Cantidad);

        if (cantidad is null)
            return;

        var etiqueta = linea.Producto.EsPorPeso
            ? "El peso"
            : "La cantidad";
        var error = Cantidades.ErrorCantidad(
            cantidad.Value, linea.Producto.UnidadMedida, etiqueta);

        if (error is not null)
        {
            MensajeOperacion = Notificar(error);
            return;
        }

        if (cantidad.Value > linea.Producto.Stock)
        {
            MensajeOperacion = Notificar(
                linea.Producto.EsPorPeso
                    ? $"El peso no puede superar el stock disponible " +
                      $"({linea.Producto.StockTexto})."
                    : "No hay más stock disponible de ese producto.");
            return;
        }

        linea.EstablecerCantidad(cantidad.Value);
        NotificarVenta();
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
        if (linea.EsPorPeso)
            return;

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
        if (linea.EsPorPeso)
            return;

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
        MetodoPagoSeleccionado = MetodoPago.Efectivo;
        MontoEfectivoTexto = string.Empty;
        MontoTarjetaTexto = string.Empty;
        MontoTransferenciaTexto = string.Empty;
        ClienteSeleccionado = ClientesItems.FirstOrDefault();
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

        var pagos = ConstruirPagos();
        var tipoVenta = EsPagoCredito
            ? TipoVenta.Credito
            : TipoVenta.Contado;

        var resultado = await _ventaService.RegistrarAsync(
            lineas,
            pagos,
            Descuento,
            ClienteSeleccionado?.Id,
            tipoVenta);

        if (!resultado.Correcta)
        {
            MensajeOperacion = Notificar(resultado.Mensaje);
            return;
        }

        MensajeOperacion = resultado.Mensaje;

        LimpiarVenta();

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
    /// Se ejecuta después de que la ventana de cierre confirma que la caja se
    /// cerró. Si el respaldo automático falla, lo avisa en la pantalla.
    /// </summary>
    public async Task EjecutarRespaldoAlCerrarCajaAsync()
    {
        if (_respaldoAutomatico is null) return;

        await EjecutarSeguroAsync(async () =>
        {
            var resultado = await _respaldoAutomatico.CrearAlCerrarCajaAsync();

            if (!resultado.Correcta)
                MensajeOperacion = Notificar(resultado.Mensaje);
        });
    }

    /// <summary>
    /// La vista lo usa al seleccionar una caja del historial.
    /// </summary>
    public async Task<CajaHistorialViewModel?> CrearHistorialViewModelAsync(Caja caja)
    {
        if (_ventaService is null || _sesion is null) return null;

        var ventas = await _ventaService.ObtenerPorCajaAsync(caja.Id);
        return new CajaHistorialViewModel(caja, ventas, _ventaService, _sesion);
    }

    /// <summary>
    /// Crea el resumen imprimible de una caja cerrada con las ventas netas y
    /// los montos congelados al cierre. Se usa al cerrar la caja y al
    /// reimprimirlo desde el detalle del historial.
    /// </summary>
    public async Task<CorteCajaViewModel?> CrearCorteCajaViewModelAsync(Caja caja)
    {
        if (_cajaService is null) return null;

        var resumen = await _cajaService.ObtenerResumenPagosAsync(caja.Id);
        return new CorteCajaViewModel(caja, resumen);
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
        MetodoPagoSeleccionado = MetodoPago.Efectivo;
        MontoEfectivoTexto = string.Empty;
        MontoTarjetaTexto = string.Empty;
        MontoTransferenciaTexto = string.Empty;

        CajaAbierta = null;
        MontoEsperado = 0;

        NotificarVenta();
    }

    private void NotificarVenta()
    {
        OnPropertyChanged(nameof(SubtotalVenta));
        OnPropertyChanged(nameof(SubtotalVentaTexto));
        OnPropertyChanged(nameof(Descuento));
        OnPropertyChanged(nameof(DescuentoTextoMostrar));
        OnPropertyChanged(nameof(TotalVenta));
        OnPropertyChanged(nameof(TotalVentaTexto));
        OnPropertyChanged(nameof(HayLineasVenta));

        NotificarPagos();
    }

    private List<LineaPago> ConstruirPagos()
    {
        if (TotalVenta <= 0 || EsPagoCredito)
            return [];

        if (EsPagoEfectivo)
        {
            return
            [
                new LineaPago(MetodoPago.Efectivo, TotalVenta, MontoEfectivo)
            ];
        }

        if (!EsPagoMixto)
        {
            return [new LineaPago(MetodoPagoSeleccionado, TotalVenta)];
        }

        var pagos = new List<LineaPago>();

        if (MontoTarjeta > 0)
            pagos.Add(new LineaPago(MetodoPago.Tarjeta, MontoTarjeta));

        if (MontoTransferencia > 0)
        {
            pagos.Add(new LineaPago(
                MetodoPago.Transferencia, MontoTransferencia));
        }

        var efectivoAplicado = TotalVenta - MontoTarjeta - MontoTransferencia;

        if (efectivoAplicado > 0)
        {
            pagos.Add(new LineaPago(
                MetodoPago.Efectivo, efectivoAplicado, MontoEfectivo));
        }
        else if (MontoEfectivo > 0)
        {
            // La tarjeta/transferencia ya cubrieron el total; el efectivo
            // recibido se registra solo para calcular el cambio.
            pagos.Add(new LineaPago(MetodoPago.Efectivo, 0m, MontoEfectivo));
        }

        return pagos;
    }

    private void NotificarPagos()
    {
        OnPropertyChanged(nameof(EsPagoEfectivo));
        OnPropertyChanged(nameof(EsPagoMixto));
        OnPropertyChanged(nameof(EsPagoCredito));
        OnPropertyChanged(nameof(MuestraEfectivo));
        OnPropertyChanged(nameof(EsPublico));
        OnPropertyChanged(nameof(ClienteSeleccionadoTexto));
        OnPropertyChanged(nameof(CreditoDisponibleTexto));
        OnPropertyChanged(nameof(MontoEfectivo));
        OnPropertyChanged(nameof(MontoTarjeta));
        OnPropertyChanged(nameof(MontoTransferencia));
        OnPropertyChanged(nameof(EfectivoAplicado));
        OnPropertyChanged(nameof(EfectivoRecibido));
        OnPropertyChanged(nameof(TotalRecibido));
        OnPropertyChanged(nameof(TotalRecibidoTexto));
        OnPropertyChanged(nameof(TotalPagado));
        OnPropertyChanged(nameof(TotalPagadoTexto));
        OnPropertyChanged(nameof(Faltante));
        OnPropertyChanged(nameof(FaltanteTexto));
        OnPropertyChanged(nameof(HayFaltante));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(CambioTexto));
        OnPropertyChanged(nameof(HayCambio));
        OnPropertyChanged(nameof(PagosValidos));
    }

    private string MensajePagosInvalido()
    {
        if (EsPagoCredito)
        {
            if (ClienteSeleccionado?.Id is null)
                return "Selecciona un cliente para vender a crédito.";

            return $"El cliente no tiene crédito suficiente. " +
                   $"Disponible: {CreditoDisponibleTexto}.";
        }

        if (MontoEfectivo < 0 || MontoTarjeta < 0 || MontoTransferencia < 0)
            return "Los montos de pago no pueden ser negativos.";

        if (EsPagoEfectivo)
            return "El efectivo recibido es menor al total de la venta.";

        if (EsPagoMixto)
        {
            if (MontoTarjeta + MontoTransferencia > TotalVenta)
            {
                return "Los pagos con tarjeta o transferencia no pueden " +
                       "superar el total de la venta.";
            }

            if (TotalRecibido < TotalVenta)
            {
                return "La suma de los pagos debe ser al menos el total " +
                       "de la venta.";
            }
        }

        return "Revisa los pagos de la venta.";
    }

    private decimal Parse(string? texto)
        => TryParseDecimal(texto, out var valor) ? valor : 0m;

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
