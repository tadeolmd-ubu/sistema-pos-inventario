using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace PosInventario.Service;

/// <summary>
/// Impresión en Linux/macOS mediante CUPS. Si no hay impresora o CUPS no está
/// instalado, devuelve un aviso en lugar de lanzar una excepción.
/// </summary>
internal static class ImpresionUnix
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(20);

    public static async Task<Resultado> ImprimirAsync(
        string contenido, AnchoTicket ancho)
    {
        var archivo = Path.Combine(
            Path.GetTempPath(), $"ticket-{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(
                archivo, contenido, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var resultado = await EjecutarAsync("lp", archivo);

            if (resultado is not null)
                return resultado;

            resultado = await EjecutarAsync("lpr", archivo);

            if (resultado is not null)
                return resultado;

            RegistroApp.Escribir(
                "No se encontró el comando lp ni lpr para imprimir el ticket.");

            return new Resultado(
                false,
                "No se encontró una impresora configurada. Instala CUPS o configura una impresora predeterminada.");
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo imprimir el ticket en Unix.", ex);

            return new Resultado(false, ImpresionPlataforma.MensajeError);
        }
        finally
        {
            try
            {
                File.Delete(archivo);
            }
            catch
            {
                // El archivo temporal no debe afectar el resultado.
            }
        }
    }

    /// <summary>
    /// Ejecuta el comando de impresión. Devuelve null cuando el comando no
    /// existe para intentar con el siguiente.
    /// </summary>
    private static async Task<Resultado?> EjecutarAsync(
        string comando, string archivo)
    {
        try
        {
            var inicio = new ProcessStartInfo
            {
                FileName = comando,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            inicio.ArgumentList.Add(archivo);

            using var proceso = Process.Start(inicio);

            if (proceso is null)
                return null;

            var salida = proceso.StandardOutput.ReadToEndAsync();
            var error = proceso.StandardError.ReadToEndAsync();

            using var tiempo = new CancellationTokenSource(EsperaMaxima);

            try
            {
                await proceso.WaitForExitAsync(tiempo.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    proceso.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Si ya terminó, no hay nada que cancelar.
                }

                return new Resultado(
                    false,
                    "La impresora no respondió. Revisa que esté encendida y conectada.");
            }

            var textoSalida = await salida;
            var textoError = await error;

            if (proceso.ExitCode == 0)
                return new Resultado(true, "Ticket enviado a la impresora.");

            RegistroApp.Escribir(
                $"El comando {comando} falló al imprimir: " +
                $"{textoError.Trim()} {textoSalida.Trim()}");

            return new Resultado(false, ImpresionPlataforma.MensajeError);
        }
        catch (Win32Exception)
        {
            // El comando no está instalado; se intenta el siguiente.
            return null;
        }
    }
}
