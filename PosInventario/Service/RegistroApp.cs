using System.Text;

namespace PosInventario.Service;

/// <summary>
/// Registro simple en archivo para diagnosticar errores en la instalación.
/// Nunca debe lanzar: si falla la escritura, la aplicación sigue funcionando.
/// </summary>
public static class RegistroApp
{
    private const int DiasRetencion = 30;

    private static readonly object Candado = new();

    public static string CarpetaLogs { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SistemaInventario",
        "logs");

    public static string ArchivoActual =>
        Path.Combine(CarpetaLogs, $"app-{DateTime.Now:yyyyMMdd}.log");

    public static void Escribir(string mensaje, Exception? excepcion = null)
    {
        try
        {
            lock (Candado)
            {
                Directory.CreateDirectory(CarpetaLogs);

                var texto = new StringBuilder()
                    .Append('[')
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                    .Append("] ")
                    .AppendLine(mensaje);

                if (excepcion is not null)
                    texto.AppendLine(excepcion.ToString());

                File.AppendAllText(ArchivoActual, texto.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // El registro no puede tumbar la aplicación.
        }
    }

    /// <summary>
    /// Elimina los archivos de log más antiguos que la retención configurada.
    /// </summary>
    public static void LimpiarAntiguos(int dias = DiasRetencion, string? carpeta = null)
    {
        try
        {
            var carpetaLogs = carpeta ?? CarpetaLogs;

            if (!Directory.Exists(carpetaLogs))
                return;

            var limite = DateTime.Now.AddDays(-dias);

            foreach (var archivo in Directory.EnumerateFiles(carpetaLogs, "app-*.log"))
            {
                try
                {
                    if (File.GetLastWriteTime(archivo) < limite)
                        File.Delete(archivo);
                }
                catch
                {
                    // Si un archivo no se puede borrar, se ignora.
                }
            }
        }
        catch
        {
            // La limpieza no puede tumbar la aplicación.
        }
    }
}
