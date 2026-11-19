using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Captura un abono para una venta a crédito concreta.
/// </summary>
public partial class AbonoViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly ClienteService? _clienteService;
    private readonly VentaCreditoResumen? _venta;

    public AbonoViewModel() { }

    public AbonoViewModel(
        ClienteService clienteService,
        Cliente cliente,
        VentaCreditoResumen venta)
    {
        _clienteService = clienteService;
        _venta = venta;

        ClienteNombre = cliente.Nombre;
        Ticket = venta.Ticket;
        FechaTexto = venta.FechaTexto;
        PendienteTexto = venta.PendienteTexto;
        MontoTexto = venta.PendienteTexto;
    }

    public string ClienteNombre { get; } = string.Empty;

    public string Ticket { get; } = string.Empty;

    public string FechaTexto { get; } = string.Empty;

    public string PendienteTexto { get; } = "0.00";

    public IReadOnlyList<MetodoPago> Metodos { get; } =
        [MetodoPago.Efectivo, MetodoPago.Tarjeta, MetodoPago.Transferencia];

    [ObservableProperty] private string montoTexto = string.Empty;
    [ObservableProperty] private MetodoPago metodoSeleccionado = MetodoPago.Efectivo;
    [ObservableProperty] private string referencia = string.Empty;

    public decimal Pendiente =>
        _venta?.Pendiente ?? 0m;

    public decimal Monto =>
        TryParseDecimal(MontoTexto, out var monto) ? monto : 0m;

    public bool PuedeConfirmar =>
        _venta is not null &&
        Monto > 0m &&
        Monto <= Pendiente;

    public event Action? AbonoRegistrado;

    partial void OnMontoTextoChanged(string value)
    {
        OnPropertyChanged(nameof(Monto));
        OnPropertyChanged(nameof(PuedeConfirmar));
    }

    [RelayCommand]
    private Task ConfirmarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_clienteService is null || _venta is null) return;

        if (!PuedeConfirmar)
        {
            MensajeOperacion =
                $"El abono debe ser mayor que 0 y no superar " +
                $"los {PendienteTexto} pendientes.";
            return;
        }

        var resultado = await _clienteService.RegistrarAbonoAsync(
            _venta.IdVenta,
            Monto,
            MetodoSeleccionado,
            Referencia);

        MensajeOperacion = resultado.Mensaje;

        if (resultado.Correcta)
            AbonoRegistrado?.Invoke();
    });

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(
            limpio, NumberStyles.Number, Cultura, out valor);
    }
}
