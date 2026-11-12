namespace PosInventario.Service;

/// <summary>
/// Decide cuándo crear respaldos automáticos y aplica la retención configurada.
/// La retención solo elimina archivos con el prefijo de respaldo automático.
/// </summary>
public class RespaldoAutomaticoService
{
    private const string PrefijoAutomatico = "respaldo_auto_";
    private const string PatronTodos = "respaldo_*.db";

    private readonly RespaldoService _respaldoService;
    private readonly ConfiguracionAppService _configuracion;
    private readonly string _carpetaRespaldos;

    public RespaldoAutomaticoService(
        RespaldoService respaldoService,
        ConfiguracionAppService configuracion,
        string carpetaRespaldos)
    {
        _respaldoService = respaldoService;
        _configuracion = configuracion;
        _carpetaRespaldos = carpetaRespaldos;
    }

    /// <summary>
    /// Respaldo posterior al cierre de caja. Siempre crea uno nuevo cuando los
    /// respaldos automáticos están habilitados.
    /// </summary>
    public Task<Resultado<string>> CrearAlCerrarCajaAsync()
        => CrearAsync("cierre de caja", omitirSiHayRespaldoHoy: false);

    /// <summary>
    /// Respaldo diario al iniciar sesión. Si ya hay un respaldo de hoy,
    /// incluido uno manual, no crea otro.
    /// </summary>
    public Task<Resultado<string>> CrearDiarioAsync()
        => CrearAsync("control diario", omitirSiHayRespaldoHoy: true);

    private async Task<Resultado<string>> CrearAsync(
        string motivo, bool omitirSiHayRespaldoHoy)
    {
        var configuracion = _configuracion.Actual;

        if (!configuracion.RespaldosAutomaticos)
            return new Resultado<string>(
                true, "Los respaldos automáticos están desactivados.", null);

        if (omitirSiHayRespaldoHoy && HayRespaldoDeHoy())
            return new Resultado<string>(
                true, "Ya existe un respaldo de hoy.", null);

        var respaldo = await _respaldoService.CrearRespaldoAutomaticoAsync(
            _carpetaRespaldos);

        if (!respaldo.Correcta || respaldo.Valor is null)
            return new Resultado<string>(
                false,
                $"No se pudo crear el respaldo automático ({motivo}). " +
                respaldo.Mensaje,
                null);

        AplicarRetencion(configuracion.RetencionDias);

        return new Resultado<string>(
            true,
            $"Respaldo automático creado ({motivo}).",
            respaldo.Valor);
    }

    private bool HayRespaldoDeHoy()
    {
        try
        {
            if (!Directory.Exists(_carpetaRespaldos))
                return false;

            return Directory
                .EnumerateFiles(_carpetaRespaldos, PatronTodos)
                .Any(ruta => File.GetLastWriteTime(ruta).Date == DateTime.Today);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void AplicarRetencion(int retencionDias)
    {
        var dias = Math.Max(1, retencionDias);
        var fechaLimite = DateTime.Today.AddDays(-(dias - 1));

        try
        {
            if (!Directory.Exists(_carpetaRespaldos))
                return;

            var archivos = Directory
                .EnumerateFiles(_carpetaRespaldos, $"{PrefijoAutomatico}*.db")
                .Select(ruta => new FileInfo(ruta))
                .ToList();

            foreach (var archivo in archivos)
            {
                if (archivo.LastWriteTime.Date >= fechaLimite)
                    continue;

                try
                {
                    archivo.Delete();
                }
                catch (IOException ex)
                {
                    RegistroApp.Escribir(
                        $"No se pudo eliminar el respaldo antiguo {archivo.Name}.",
                        ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    RegistroApp.Escribir(
                        $"No se pudo eliminar el respaldo antiguo {archivo.Name}.",
                        ex);
                }
            }
        }
        catch (IOException ex)
        {
            RegistroApp.Escribir(
                "No se pudo aplicar la retención de respaldos automáticos.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            RegistroApp.Escribir(
                "No se pudo aplicar la retención de respaldos automáticos.", ex);
        }
    }
}
