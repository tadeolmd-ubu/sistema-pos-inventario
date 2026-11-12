namespace PosInventario.Service;

/// <summary>
/// Datos de un archivo de respaldo que pasó la validación y puede restaurarse.
/// </summary>
public sealed record RespaldoInfo(
    string Ruta,
    long TamanoBytes,
    DateTime FechaModificacion,
    int CantidadMigraciones);
