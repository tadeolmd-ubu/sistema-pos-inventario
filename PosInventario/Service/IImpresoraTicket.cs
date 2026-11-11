namespace PosInventario.Service;

/// <summary>
/// Envía un ticket ya formateado a la impresora predeterminada. Las
/// implementaciones nunca deben lanzar por falta de impresora: devuelven un
/// <see cref="Resultado"/> con el aviso para la interfaz.
/// </summary>
public interface IImpresoraTicket
{
    Task<Resultado> ImprimirAsync(string contenido, AnchoTicket ancho);
}
