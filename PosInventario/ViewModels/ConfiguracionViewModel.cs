using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ConfiguracionViewModel : OperacionViewModel
{
    private readonly RespaldoService? _respaldoService;
    private readonly SesionActual? _sesion;

    public ConfiguracionViewModel() { }

    public ConfiguracionViewModel(
        RespaldoService respaldoService, string rutaBaseDatos, SesionActual sesion)
    {
        _respaldoService = respaldoService;
        _sesion = sesion;
        RutaBaseDatos = rutaBaseDatos;

        var carpeta = Path.GetDirectoryName(rutaBaseDatos);

        CarpetaSugerida = string.IsNullOrWhiteSpace(carpeta)
            ? Path.Combine(AppContext.BaseDirectory, "Respaldos")
            : Path.Combine(carpeta, "Respaldos");
    }

    [ObservableProperty] private string rutaBaseDatos = "—";
    [ObservableProperty] private string carpetaSugerida = "—";
    [ObservableProperty] private string ultimoRespaldoTexto = string.Empty;

    public bool HayUltimoRespaldo => !string.IsNullOrEmpty(UltimoRespaldoTexto);

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();
        UltimoRespaldoTexto = string.Empty;
    }

    partial void OnUltimoRespaldoTextoChanged(string value)
        => OnPropertyChanged(nameof(HayUltimoRespaldo));

    [RelayCommand]
    private Task CrearRespaldoSugeridoAsync()
        => CrearRespaldoAsync(CarpetaSugerida);

    public Task CrearRespaldoAsync(string carpetaDestino) => EjecutarSeguroAsync(async () =>
    {
        if (_respaldoService is null) return;

        if (_sesion is not null && !_sesion.EsAdministrador)
        {
            MensajeOperacion = Notificar("Solo un administrador puede crear respaldos.");
            return;
        }

        if (string.IsNullOrWhiteSpace(carpetaDestino))
        {
            MensajeOperacion = Notificar("Selecciona una carpeta para el respaldo.");
            return;
        }

        var resultado = await _respaldoService.CrearRespaldoAsync(carpetaDestino);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta && resultado.Valor is not null)
            UltimoRespaldoTexto = resultado.Valor;
    });
}
