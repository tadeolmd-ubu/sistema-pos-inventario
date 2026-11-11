using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class RegistrarMovimientoCajaViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly CajaService? _cajaService;

    public RegistrarMovimientoCajaViewModel() { }

    public RegistrarMovimientoCajaViewModel(CajaService cajaService, decimal montoEsperado)
    {
        _cajaService = cajaService;
        MontoEsperadoTexto = montoEsperado.ToString("0.00", Cultura);
    }

    public IReadOnlyList<TipoMovimientoCaja> TiposMovimiento { get; } =
        [TipoMovimientoCaja.Ingreso, TipoMovimientoCaja.Retiro];

    [ObservableProperty] private TipoMovimientoCaja tipoMovimientoSeleccionado = TipoMovimientoCaja.Ingreso;
    [ObservableProperty] private string montoTexto = string.Empty;
    [ObservableProperty] private string descripcion = string.Empty;
    [ObservableProperty] private string montoEsperadoTexto = string.Empty;

    /// <summary>
    /// Lo escucha la ventana para cerrarse después de registrar.
    /// </summary>
    public event Action? MovimientoRegistrado;

    [RelayCommand]
    private Task RegistrarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_cajaService is null) return;

        if (!TryParseDecimal(MontoTexto, out var monto) || monto <= 0)
        {
            MensajeOperacion = "El monto debe ser un número mayor que 0.";
            return;
        }

        var resultado = TipoMovimientoSeleccionado == TipoMovimientoCaja.Retiro
            ? await _cajaService.RegistrarRetiroAsync(monto, Descripcion)
            : await _cajaService.RegistrarIngresoAsync(monto, Descripcion);

        MensajeOperacion = resultado.Mensaje;

        if (resultado.Correcta)
            MovimientoRegistrado?.Invoke();
    });

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, Cultura, out valor);
    }
}
