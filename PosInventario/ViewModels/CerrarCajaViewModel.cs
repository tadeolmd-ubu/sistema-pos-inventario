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

    public CerrarCajaViewModel() { }

    public CerrarCajaViewModel(CajaService cajaService, Caja caja, decimal montoEsperado)
    {
        _cajaService = cajaService;
        UsuarioAperturaTexto = caja.UsuarioApertura?.Nombre ?? "—";
        FechaAperturaTexto = caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura);
        MontoInicialTexto = caja.MontoInicial.ToString("0.00", Cultura);
        MontoEsperadoTexto = montoEsperado.ToString("0.00", Cultura);
    }

    [ObservableProperty] private string usuarioAperturaTexto = string.Empty;
    [ObservableProperty] private string fechaAperturaTexto = string.Empty;
    [ObservableProperty] private string montoInicialTexto = string.Empty;
    [ObservableProperty] private string montoEsperadoTexto = string.Empty;
    [ObservableProperty] private string montoFinalTexto = string.Empty;
    [ObservableProperty] private string observaciones = string.Empty;

    public event Action? CajaCerrada;

    [RelayCommand]
    private Task CerrarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_cajaService is null) return;

        if (!TryParseDecimal(MontoFinalTexto, out var montoFinal) || montoFinal < 0)
        {
            MensajeOperacion = "El monto contado debe ser un número mayor o igual a 0.";
            return;
        }

        var resultado = await _cajaService.CerrarAsync(montoFinal, Observaciones);

        MensajeOperacion = resultado.Mensaje;

        if (resultado.Correcta)
            CajaCerrada?.Invoke();
    });

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, Cultura, out valor);
    }
}
