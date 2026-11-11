namespace PosInventario.Service;

/// <summary>
/// Bloqueo de instancia única basado en un archivo. A diferencia de un Mutex
/// con nombre, funciona entre procesos en Windows y Linux: el sistema libera
/// el bloqueo automáticamente si la aplicación termina o se cierra de golpe.
/// </summary>
public sealed class InstanciaUnica : IDisposable
{
    private readonly FileStream _bloqueo;

    private InstanciaUnica(FileStream bloqueo) => _bloqueo = bloqueo;

    public static string RutaPredeterminada => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SistemaInventario",
        "instancia.lock");

    public static bool TryAdquirir(out InstanciaUnica? instancia)
        => TryAdquirir(RutaPredeterminada, out instancia);

    /// <summary>
    /// Intenta quedarse con el bloqueo exclusivo del archivo indicado.
    /// Devuelve false cuando otra instancia ya lo tiene abierto.
    /// </summary>
    public static bool TryAdquirir(string ruta, out InstanciaUnica? instancia)
    {
        instancia = null;

        try
        {
            var carpeta = Path.GetDirectoryName(ruta);

            if (!string.IsNullOrEmpty(carpeta))
                Directory.CreateDirectory(carpeta);

            var flujo = new FileStream(
                ruta,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);

            instancia = new InstanciaUnica(flujo);
            return true;
        }
        catch (IOException)
        {
            // El archivo está bloqueado por otra instancia.
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public void Dispose() => _bloqueo.Dispose();
}
