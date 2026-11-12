namespace PosInventario.Service;

/// <summary>
/// Resultado de una restauración: ruta de la base restaurada y del respaldo
/// de seguridad que se creó antes de reemplazarla.
/// </summary>
public sealed record RestauracionRespaldo(
    string RutaBaseDatos,
    string RutaRespaldoSeguridad);
