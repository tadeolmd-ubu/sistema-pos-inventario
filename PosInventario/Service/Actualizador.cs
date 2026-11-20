using System.Diagnostics;
using System.IO.Compression;

namespace PosInventario.Service;

/// <summary>
/// Prepara y lanza la actualización: descarga el paquete, crea un respaldo,
/// reemplaza los archivos de la instalación y vuelve a abrir la aplicación.
/// Se copian archivos en lugar de ejecutar un instalador nuevo porque el
/// Control de aplicaciones de Windows suele bloquear los autoextraíbles sin
/// firma. La aplicación actual debe cerrarse después de obtener un resultado
/// correcto.
/// </summary>
public static class Actualizador
{
    private const string NombreCarpeta = "Actualizaciones";

    /// <summary>
    /// Borra las carpetas de intentos anteriores sin tocar la del proceso
    /// actual. Si alguna sigue en uso, se ignora.
    /// </summary>
    private static void LimpiarCarpetasAnteriores(string carpetaBase, string actual)
    {
        try
        {
            foreach (var carpeta in Directory.EnumerateDirectories(carpetaBase))
            {
                if (string.Equals(carpeta, actual, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    Directory.Delete(carpeta, true);
                }
                catch
                {
                    // Un intento anterior puede seguir usándola.
                }
            }
        }
        catch
        {
            // La limpieza no debe bloquear la actualización.
        }
    }

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
            // Una carpeta por proceso evita que dos intentos simultáneos se
            // bloqueen el archivo de descarga.
            carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaInventario",
                NombreCarpeta,
                Environment.ProcessId.ToString());

            Directory.CreateDirectory(carpeta);

            LimpiarCarpetasAnteriores(
                Path.GetDirectoryName(carpeta)!, carpeta);
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo preparar la carpeta de actualización.", ex);
            return new Resultado<string>(
                false, "No se pudo preparar la carpeta para actualizar.", null);
        }

        var extension = actualizacion.EsZip ? "zip" : "exe";

        var paquete = Path.Combine(
            carpeta, $"PosInventario-{actualizacion.Version}-win-x64.{extension}");

        RegistroApp.Escribir(
            $"Descargando actualización {actualizacion.Version} desde {actualizacion.UrlDescarga}");

        var descarga = await servicio.DescargarAsync(
            actualizacion, paquete, progreso, cancelacion);

        if (!descarga.Correcta)
        {
            RegistroApp.Escribir($"Falló la descarga de la actualización: {descarga.Mensaje}");
            return new Resultado<string>(false, descarga.Mensaje, null);
        }

        string? staging = null;

        if (actualizacion.EsZip)
        {
            try
            {
                staging = Path.Combine(carpeta, $"staging-{actualizacion.Version}");

                if (Directory.Exists(staging))
                    Directory.Delete(staging, true);

                ZipFile.ExtractToDirectory(paquete, staging);

                if (!File.Exists(Path.Combine(staging, "PosInventario.exe")))
                {
                    RegistroApp.Escribir(
                        "El paquete descargado no contiene PosInventario.exe.");

                    return new Resultado<string>(
                        false, "El paquete descargado está incompleto.", null);
                }
            }
            catch (Exception ex)
            {
                RegistroApp.Escribir("No se pudo extraer el paquete de actualización.", ex);
                return new Resultado<string>(
                    false, "No se pudo preparar el paquete de actualización.", null);
            }
        }

        try
        {
            var respaldo = await respaldos.CrearRespaldoAsync(carpetaRespaldos);

            if (!respaldo.Correcta)
                RegistroApp.Escribir(
                    $"No se pudo crear el respaldo previo a la actualización: {respaldo.Mensaje}");
        }
        catch (Exception ex)
        {
            // La actualización no toca la carpeta de datos; el respaldo es una
            // precaución adicional y su fallo no debe bloquearla.
            RegistroApp.Escribir("No se pudo crear el respaldo previo a la actualización.", ex);
        }

        try
        {
            var guion = Path.Combine(carpeta, "actualizar.ps1");
            var proceso = Environment.ProcessId;
            var destino = Path.GetDirectoryName(ejecutable)!;

            var contenido = actualizacion.EsZip && staging is not null
                ? $$"""
Start-Sleep -Milliseconds 800
$p = Get-Process -Id {{proceso}} -ErrorAction SilentlyContinue
if ($p) { [void]$p.WaitForExit(30000) }
Get-ChildItem -Path '{{destino}}' -File | Where-Object { $_.Name -notlike 'unins*' } | Remove-Item -Force -ErrorAction SilentlyContinue
Copy-Item -Path '{{staging}}\*' -Destination '{{destino}}' -Recurse -Force
Start-Process -FilePath '{{ejecutable}}'
"""
                : $$"""
Start-Sleep -Milliseconds 1500
Start-Process -FilePath '{{paquete}}' -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/CLOSEAPPLICATIONS','/FORCECLOSEAPPLICATIONS' -Wait
Start-Process -FilePath '{{ejecutable}}'
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

            RegistroApp.Escribir(
                $"Actualización {actualizacion.Version} lista; se instalará al cerrar.");

            return new Resultado<string>(true, "Actualización iniciada.", guion);
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo iniciar la actualización.", ex);
            return new Resultado<string>(
                false, "No se pudo iniciar la actualización.", null);
        }
    }
}
