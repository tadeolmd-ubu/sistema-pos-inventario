using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class CerrarCajaViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly CajaService? _cajaService;
    private readonly decimal _montoEsperado;

    public CerrarCajaViewModel() { }

    public CerrarCajaViewModel(CajaService cajaService, Caja caja, decimal montoEsperado)
        : this(cajaService, caja, montoEsperado, null)
    {
    }

    public CerrarCajaViewModel(
        CajaService cajaService,
        Caja caja,
        decimal montoEsperado,
        ResumenPagosCaja? resumen)
    {
        _cajaService = cajaService;
        _montoEsperado = montoEsperado;
        UsuarioAperturaTexto = caja.UsuarioApertura?.Nombre ?? "—";
        FechaAperturaTexto = caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura);
        MontoInicialTexto = caja.MontoInicial.ToString("0.00", Cultura);
        MontoEsperadoTexto = montoEsperado.ToString("0.00", Cultura);

        if (resumen is null)
            return;

        TieneResumen = true;
        CantidadVentasTexto = resumen.CantidadVentas.ToString(Cultura);
        TotalVentasTexto = resumen.Total.ToString("0.00", Cultura);
        TotalEfectivoTexto = resumen.Efectivo.ToString("0.00", Cultura);
        TotalTarjetaTexto = resumen.Tarjeta.ToString("0.00", Cultura);
        TotalTransferenciaTexto = resumen.Transferencia.ToString("0.00", Cultura);
    }

    [ObservableProperty] private string usuarioAperturaTexto = string.Empty;
    [ObservableProperty] private string fechaAperturaTexto = string.Empty;
    [ObservableProperty] private string montoInicialTexto = string.Empty;
    [ObservableProperty] private string montoEsperadoTexto = string.Empty;
    [ObservableProperty] private string montoFinalTexto = string.Empty;
    [ObservableProperty] private string observaciones = string.Empty;

    [ObservableProperty] private string cantidadVentasTexto = string.Empty;
    [ObservableProperty] private string totalVentasTexto = string.Empty;
    [ObservableProperty] private string totalEfectivoTexto = string.Empty;
    [ObservableProperty] private string totalTarjetaTexto = string.Empty;
    [ObservableProperty] private string totalTransferenciaTexto = string.Empty;

    public decimal MontoEsperado => _montoEsperado;

    public bool MontoContadoValido =>
        TryParseDecimal(MontoFinalTexto, out var monto) && monto >= 0;

    public decimal MontoContado =>
        TryParseDecimal(MontoFinalTexto, out var monto) ? monto : 0m;

    public decimal Diferencia => MontoContado - MontoEsperado;

    public bool HayDiferencia => MontoContadoValido && Diferencia != 0;

    public bool HayFaltante => HayDiferencia && Diferencia < 0;

    public bool HaySobrante => HayDiferencia && Diferencia > 0;

    public bool SinDiferencia => MontoContadoValido && Diferencia == 0;

    public string MontoContadoTexto => MontoContado.ToString("0.00", Cultura);

    public string FaltanteTexto =>
        $"Faltante: {Math.Abs(Diferencia).ToString("0.00", Cultura)}";

    public string SobranteTexto =>
        $"Sobrante: {Diferencia.ToString("0.00", Cultura)}";

    public string DiferenciaDetalleTexto =>
        $"Esperado: {MontoEsperadoTexto} · Contado: {MontoContadoTexto}";

    public bool TieneResumen { get; }

    public event Action? CajaCerrada;

    [RelayCommand]
    private Task CerrarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_cajaService is null) return;

        if (!MontoContadoValido)
        {
            MensajeOperacion = "El monto contado debe ser un número mayor o igual a 0.";
            return;
        }

        var resultado = await _cajaService.CerrarAsync(MontoContado, Observaciones);

        MensajeOperacion = resultado.Mensaje;

        if (resultado.Correcta)
            CajaCerrada?.Invoke();
    });

    partial void OnMontoFinalTextoChanged(string value) => NotificarDiferencia();

    private void NotificarDiferencia()
    {
        OnPropertyChanged(nameof(MontoContadoValido));
        OnPropertyChanged(nameof(MontoContado));
        OnPropertyChanged(nameof(Diferencia));
        OnPropertyChanged(nameof(HayDiferencia));
        OnPropertyChanged(nameof(HayFaltante));
        OnPropertyChanged(nameof(HaySobrante));
        OnPropertyChanged(nameof(SinDiferencia));
        OnPropertyChanged(nameof(MontoContadoTexto));
        OnPropertyChanged(nameof(FaltanteTexto));
        OnPropertyChanged(nameof(SobranteTexto));
        OnPropertyChanged(nameof(DiferenciaDetalleTexto));
    }

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, Cultura, out valor);
    }
}
