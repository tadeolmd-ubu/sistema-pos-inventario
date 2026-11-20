using System.Diagnostics;

namespace PosInventario.Service;

/// <summary>
/// Prepara y lanza la actualización: descarga, respaldo previo, instalación
/// silenciosa y reapertura de la aplicación. La aplicación actual debe cerrarse
/// después de llamar a <see cref="PrepararAsync"/> con resultado correcto.
/// </summary>
public static class Actualizador
{
    private const string NombreCarpeta = "PosInventario-Actualizacion";

    public static async Task<Resultado<string>> PrepararAsync(
        ActualizacionService servicio,
        ActualizacionDisponible actualizacion,
        RespaldoService respaldos,
        string carpetaRespaldos,
        IProgress<int>? progreso = null,
        CancellationToken cancelacion = default)
    {
        if (!OperatingSystem.IsWindows())
            return new Resultado<string>(
                false, "La actualización automática solo está disponible en Windows.", null);

        var ejecutable = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(ejecutable))
            return new Resultado<string>(
                false, "No se encontró el ejecutable de la aplicación.", null);

        string carpeta;

        try
        {
            carpeta = Path.Combine(Path.GetTempPath(), NombreCarpeta);
            Directory.CreateDirectory(carpeta);
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo preparar la carpeta de actualización.", ex);
            return new Resultado<string>(
                false, "No se pudo preparar la carpeta temporal para actualizar.", null);
        }

        var instalador = Path.Combine(
            carpeta, $"PosInventario-{actualizacion.Version}-win-x64-installer.exe");

        var descarga = await servicio.DescargarAsync(
            actualizacion, instalador, progreso, cancelacion);

        if (!descarga.Correcta)
            return new Resultado<string>(false, descarga.Mensaje, null);

        try
        {
            var respaldo = await respaldos.CrearRespaldoAsync(carpetaRespaldos);

            if (!respaldo.Correcta)
                RegistroApp.Escribir(
                    $"No se pudo crear el respaldo previo a la actualización: {respaldo.Mensaje}");
        }
        catch (Exception ex)
        {
            // El instalador no toca la carpeta de datos; el respaldo es una
            // precaución adicional y su fallo no debe bloquear la actualización.
            RegistroApp.Escribir("No se pudo crear el respaldo previo a la actualización.", ex);
        }

        try
        {
            var guion = Path.Combine(carpeta, "actualizar.ps1");

            var contenido = $"""
Start-Sleep -Milliseconds 1500
Start-Process -FilePath '{instalador}' -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/CLOSEAPPLICATIONS','/FORCECLOSEAPPLICATIONS' -Wait
Start-Process -FilePath '{ejecutable}'
""";

            await File.WriteAllTextAsync(guion, contenido, cancelacion);

            var inicio = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            inicio.ArgumentList.Add("-NoProfile");
            inicio.ArgumentList.Add("-ExecutionPolicy");
            inicio.ArgumentList.Add("Bypass");
            inicio.ArgumentList.Add("-WindowStyle");
            inicio.ArgumentList.Add("Hidden");
            inicio.ArgumentList.Add("-File");
            inicio.ArgumentList.Add(guion);

            Process.Start(inicio);

            return new Resultado<string>(true, "Actualización iniciada.", guion);
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo iniciar la actualización.", ex);
            return new Resultado<string>(
                false, "No se pudo iniciar el instalador de la actualización.", null);
        }
    }
}
