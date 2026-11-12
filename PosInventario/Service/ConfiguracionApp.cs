namespace PosInventario.Service;

/// <summary>
/// Preferencias locales de la aplicación que no forman parte de la base de
/// datos de negocio.
/// </summary>
public sealed record ConfiguracionApp(
    bool RespaldosAutomaticos = true,
    int RetencionDias = ConfiguracionAppService.RetencionDiasPredeterminada);
