using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ConfiguracionViewModel : OperacionViewModel
{
    private static readonly int[] DiasRetencionSugeridos = [1, 2, 7, 15, 30];

    private readonly RespaldoService? _respaldoService;
    private readonly SesionActual? _sesion;
    private readonly ConfiguracionAppService? _configuracionApp;

    public ConfiguracionViewModel() { }

    public ConfiguracionViewModel(
        RespaldoService respaldoService,
        string rutaBaseDatos,
        SesionActual sesion,
        ConfiguracionAppService? configuracionApp = null)
    {
        _respaldoService = respaldoService;
        _sesion = sesion;
        _configuracionApp = configuracionApp;
        RutaBaseDatos = rutaBaseDatos;

        var carpeta = Path.GetDirectoryName(rutaBaseDatos);

        CarpetaSugerida = string.IsNullOrWhiteSpace(carpeta)
            ? Path.Combine(AppContext.BaseDirectory, "Respaldos")
            : Path.Combine(carpeta, "Respaldos");

        CarpetaDestinoRespaldo = CarpetaSugerida;

        if (_configuracionApp is null)
            return;

        RespaldosAutomaticos = _configuracionApp.Actual.RespaldosAutomaticos;

        var dias = _configuracionApp.Actual.RetencionDias;
        var opciones = DiasRetencionSugeridos.ToList();

        if (!opciones.Contains(dias))
        {
            opciones.Add(dias);
            opciones.Sort();
        }

        OpcionesRetencionDias = opciones;
        RetencionDiasSeleccionados = dias;
    }

    [ObservableProperty] private string rutaBaseDatos = "—";
    [ObservableProperty] private string carpetaSugerida = "—";
    [ObservableProperty] private string carpetaDestinoRespaldo = "—";
    [ObservableProperty] private string ultimoRespaldoTexto = string.Empty;
    [ObservableProperty] private bool respaldosAutomaticos = true;
    [ObservableProperty] private int retencionDiasSeleccionados =
        ConfiguracionAppService.RetencionDiasPredeterminada;

    public IReadOnlyList<int> OpcionesRetencionDias { get; private set; } =
        DiasRetencionSugeridos;

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

    /// <summary>
    /// Solo cambia la carpeta de destino. La creación del respaldo queda en
    /// manos del botón "Crear respaldo" para no generar archivos sin querer.
    /// </summary>
    public void SeleccionarCarpetaRespaldo(string carpeta)
    {
        if (string.IsNullOrWhiteSpace(carpeta))
            return;

        try
        {
            CarpetaDestinoRespaldo = Path.GetFullPath(carpeta.Trim());
            MensajeOperacion = string.Empty;
        }
        catch (Exception ex) when (
            ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            MensajeOperacion = Notificar(
                "La carpeta seleccionada no es válida.");
        }
    }

    [RelayCommand]
    private Task CrearRespaldoEnDestinoAsync()
        => CrearRespaldoAsync(CarpetaDestinoRespaldo);

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

        MensajeOperacion = resultado.Correcta
            ? resultado.Mensaje
            : Notificar(resultado.Mensaje);

        if (resultado.Correcta && resultado.Valor is not null)
            UltimoRespaldoTexto = resultado.Valor;
    });

    [RelayCommand]
    private void GuardarConfiguracion()
    {
        if (_configuracionApp is null) return;

        if (!EsAdministrador())
        {
            MensajeOperacion = Notificar(
                "Solo un administrador puede cambiar los respaldos automáticos.");
            return;
        }

        var resultado = _configuracionApp.Guardar(
            RespaldosAutomaticos, RetencionDiasSeleccionados);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);
    }

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
