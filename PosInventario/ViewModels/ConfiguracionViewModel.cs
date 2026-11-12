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

    /// <summary>
    /// Lo escucha App para cerrar la ventana principal y reiniciar el proceso
    /// después de restaurar la base.
    /// </summary>
    public event Action? ReinicioSolicitado;

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

        if (!EsAdministrador())
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

    /// <summary>
    /// Valida el archivo antes de pedir confirmación. Devuelve null cuando el
    /// archivo no se puede usar; en ese caso el mensaje ya se mostró.
    /// </summary>
    public async Task<Resultado<RespaldoInfo>?> ValidarRespaldoAsync(string ruta)
    {
        if (_respaldoService is null) return null;

        if (!EsAdministrador())
        {
            MensajeOperacion = Notificar(
                "Solo un administrador puede restaurar un respaldo.");
            return null;
        }

        Resultado<RespaldoInfo>? resultado = null;

        await EjecutarSeguroAsync(async () =>
        {
            resultado = await _respaldoService.ValidarRespaldoAsync(ruta);

            if (!resultado.Correcta)
                MensajeOperacion = Notificar(resultado.Mensaje);
        });

        return resultado;
    }

    /// <summary>
    /// Restaura el respaldo y devuelve el resultado para que la vista muestre
    /// la confirmación final antes de reiniciar la aplicación.
    /// </summary>
    public async Task<Resultado<RestauracionRespaldo>?> RestaurarRespaldoAsync(string ruta)
    {
        if (_respaldoService is null) return null;

        if (!EsAdministrador())
        {
            MensajeOperacion = Notificar(
                "Solo un administrador puede restaurar un respaldo.");
            return null;
        }

        Resultado<RestauracionRespaldo>? resultado = null;

        await EjecutarSeguroAsync(async () =>
        {
            resultado = await _respaldoService.RestaurarRespaldoAsync(
                ruta, CarpetaSugerida);

            MensajeOperacion = resultado.Correcta
                ? resultado.Mensaje
                : Notificar(resultado.Mensaje);
        });

        return resultado;
    }

    /// <summary>
    /// Pide a la aplicación que se cierre y se vuelva a abrir para leer la
    /// base restaurada. Se llama después de avisar al usuario.
    /// </summary>
    public void SolicitarReinicio() => ReinicioSolicitado?.Invoke();

    private bool EsAdministrador()
        => _sesion is null || _sesion.EsAdministrador;
}
