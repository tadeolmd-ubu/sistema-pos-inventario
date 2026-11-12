namespace PosInventario.Service;

/// <summary>
/// Impresora predeterminada del sistema. En Windows usa la API de impresión
/// para fijar el ancho de papel (58/80 mm); en Linux/macOS usa CUPS. Cualquier
/// fallo se convierte en un mensaje para que la venta no se vea afectada.
/// </summary>
public sealed class ImpresoraSistema : IImpresoraTicket
{
    private readonly Func<string, AnchoTicket, Task<Resultado>> _imprimir;

    public ImpresoraSistema()
        : this(ImpresionPlataforma.ImprimirAsync)
    {
    }

    /// <summary>
    /// Constructor para pruebas: permite sustituir el envío real a la
    /// impresora sin depender del sistema operativo.
    /// </summary>
    public ImpresoraSistema(Func<string, AnchoTicket, Task<Resultado>> imprimir)
        => _imprimir = imprimir;

    public async Task<Resultado> ImprimirAsync(string contenido, AnchoTicket ancho)
    {
        try
        {
            return await _imprimir(contenido, ancho);
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo imprimir.", ex);

            return new Resultado(false, ImpresionPlataforma.MensajeError);
        }
    }
}

internal static class ImpresionPlataforma
{
    public const string MensajeError =
        "No se pudo imprimir. Revisa que la impresora esté encendida y configurada.";

    public static Task<Resultado> ImprimirAsync(string contenido, AnchoTicket ancho)
    {
        if (OperatingSystem.IsWindows())
            return ImprimirEnWindowsAsync(contenido, ancho);

        return ImpresionUnix.ImprimirAsync(contenido, ancho);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static Task<Resultado> ImprimirEnWindowsAsync(
        string contenido, AnchoTicket ancho)
        => Task.Run(() => ImpresionWindows.Imprimir(contenido, ancho));
}
