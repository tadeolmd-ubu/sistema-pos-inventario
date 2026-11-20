using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace PosInventario.Service;

/// <summary>
/// Describe una versión publicada en GitHub que se puede instalar.
/// <paramref name="EsZip"/> indica que la descarga es el paquete portable
/// (se copian archivos) en lugar del instalador.
/// </summary>
public sealed record ActualizacionDisponible(
    string Version,
    string UrlDescarga,
    long Tamano,
    string? Sha256,
    bool EsZip);

/// <summary>
/// Consulta la última release pública del repositorio y descarga su paquete.
/// Nunca lanza: si no hay red, el repositorio no responde o el JSON cambia,
/// devuelve null y la aplicación sigue funcionando con normalidad.
/// </summary>
public sealed class ActualizacionService
{
    internal const string Repositorio = "tadeolmd-ubu/sistema-pos-inventario";

    internal const string UrlReleases =
        $"https://github.com/{Repositorio}/releases/latest";

    private const string UrlUltimaRelease =
        $"https://api.github.com/repos/{Repositorio}/releases/latest";

    private const string SufijoInstalador = "-win-x64-installer.exe";

    private const string SufijoPaquete = "-win-x64.zip";

    private static readonly HttpClient Cliente = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly HttpClient ClienteDescarga = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public ActualizacionService(string? versionActual = null)
        => VersionActual = versionActual ?? ObtenerVersionLocal();

    public string VersionActual { get; }

    /// <summary>
    /// Devuelve la actualización disponible o null si ya está al día.
    /// </summary>
    public async Task<ActualizacionDisponible?> BuscarAsync(
        CancellationToken cancelacion = default)
    {
        try
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, UrlUltimaRelease);
            solicitud.Headers.UserAgent.ParseAdd("SistemaInventario/1.0");
            solicitud.Headers.Accept.ParseAdd("application/vnd.github+json");

            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                ?? Environment.GetEnvironmentVariable("GH_TOKEN");

            if (!string.IsNullOrWhiteSpace(token))
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var respuesta = await Cliente.SendAsync(solicitud, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
                return null;

            using var documento = JsonDocument.Parse(
                await respuesta.Content.ReadAsStringAsync(cancelacion));

            var raiz = documento.RootElement;

            if (!raiz.TryGetProperty("tag_name", out var etiqueta) ||
                etiqueta.GetString() is not { Length: > 0 } versionRemota)
                return null;

            if (!EsVersionMasNueva(versionRemota, VersionActual))
                return null;

            if (!raiz.TryGetProperty("assets", out var activos) ||
                activos.ValueKind != JsonValueKind.Array)
                return null;

            ActualizacionDisponible? instalador = null;

            foreach (var activo in activos.EnumerateArray())
            {
                if (!activo.TryGetProperty("name", out var nombre) ||
                    nombre.GetString() is not { Length: > 0 } nombreArchivo)
                    continue;

                if (!activo.TryGetProperty("browser_download_url", out var url) ||
                    url.GetString() is not { Length: > 0 } direccion)
                    continue;

                var tamano = activo.TryGetProperty("size", out var valorTamano)
                    ? valorTamano.GetInt64()
                    : 0;

                string? sha256 = null;

                if (activo.TryGetProperty("digest", out var digest) &&
                    digest.GetString() is { Length: > 7 } textoDigest &&
                    textoDigest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                    sha256 = textoDigest["sha256:".Length..];

                var version = versionRemota.Trim().TrimStart('v', 'V');

                // El paquete portable se prefiere porque no necesita ejecutar un
                // instalador nuevo, que es lo que suele bloquear el Control de
                // aplicaciones de Windows.
                if (EsPaquetePortable(nombreArchivo))
                    return new ActualizacionDisponible(version, direccion, tamano, sha256, true);

                if (instalador is null && EsInstaladorDeWindows(nombreArchivo))
                    instalador = new ActualizacionDisponible(
                        version, direccion, tamano, sha256, false);
            }

            return instalador;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Descarga el paquete y comprueba tamaño y hash SHA-256 cuando GitHub los
    /// publica. Si algo no coincide, borra el archivo y devuelve error.
    /// </summary>
    public async Task<Resultado<string>> DescargarAsync(
        ActualizacionDisponible actualizacion,
        string destino,
        IProgress<int>? progreso = null,
        CancellationToken cancelacion = default)
    {
        try
        {
            using var respuesta = await ClienteDescarga.GetAsync(
                actualizacion.UrlDescarga,
                HttpCompletionOption.ResponseHeadersRead,
                cancelacion);

            if (!respuesta.IsSuccessStatusCode)
                return new Resultado<string>(
                    false, $"No se pudo descargar la actualización ({respuesta.StatusCode}).", null);

            var total = respuesta.Content.Headers.ContentLength ?? actualizacion.Tamano;

            await using (var origen = await respuesta.Content.ReadAsStreamAsync(cancelacion))
            await using (var archivo = new FileStream(
                destino, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long descargado = 0;
                int leidos;

                while ((leidos = await origen.ReadAsync(buffer, cancelacion)) > 0)
                {
                    await archivo.WriteAsync(buffer.AsMemory(0, leidos), cancelacion);
                    descargado += leidos;

                    if (total > 0)
                        progreso?.Report((int)(descargado * 100 / total));
                }
            }

            var info = new FileInfo(destino);

            if (actualizacion.Tamano > 0 && info.Length != actualizacion.Tamano)
            {
                File.Delete(destino);
                return new Resultado<string>(
                    false, "El archivo descargado no tiene el tamaño esperado.", null);
            }

            if (!string.IsNullOrWhiteSpace(actualizacion.Sha256))
            {
                await using var lector = File.OpenRead(destino);
                var hash = await SHA256.HashDataAsync(lector, cancelacion);

                if (!Convert.ToHexString(hash).Equals(
                        actualizacion.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(destino);
                    return new Resultado<string>(
                        false, "El archivo descargado no coincide con la firma publicada.", null);
                }
            }

            progreso?.Report(100);
            return new Resultado<string>(true, "Descarga completada.", destino);
        }
        catch (OperationCanceledException)
        {
            return new Resultado<string>(false, "La descarga se canceló.", null);
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo descargar la actualización.", ex);
            return new Resultado<string>(false, "No se pudo descargar la actualización.", null);
        }
    }

    /// <summary>
    /// True cuando el nombre del activo es el instalador de Windows.
    /// </summary>
    public static bool EsInstaladorDeWindows(string? nombreArchivo)
        => !string.IsNullOrWhiteSpace(nombreArchivo) &&
           nombreArchivo.EndsWith(SufijoInstalador, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True cuando el nombre del activo es el paquete portable.
    /// </summary>
    public static bool EsPaquetePortable(string? nombreArchivo)
        => !string.IsNullOrWhiteSpace(nombreArchivo) &&
           nombreArchivo.EndsWith(SufijoPaquete, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Compara versiones numéricas separadas por puntos. Ignora el prefijo "v"
    /// y cualquier sufijo como "-beta". Sin partes numéricas válidas devuelve
    /// false para no ofrecer actualizaciones por error.
    /// </summary>
    public static bool EsVersionMasNueva(string? versionRemota, string? versionLocal)
    {
        if (!IntentarPartir(versionRemota, out var remota) ||
            !IntentarPartir(versionLocal, out var local))
            return false;

        var partes = Math.Max(remota.Length, local.Length);

        for (var i = 0; i < partes; i++)
        {
            var actual = i < remota.Length ? remota[i] : 0;
            var instalada = i < local.Length ? local[i] : 0;

            if (actual > instalada)
                return true;

            if (actual < instalada)
                return false;
        }

        return false;
    }

    private static bool IntentarPartir(string? texto, out int[] partes)
    {
        partes = [];

        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var limpio = texto.Trim().TrimStart('v', 'V');

        var corte = limpio.IndexOfAny(['-', '+']);

        if (corte >= 0)
            limpio = limpio[..corte];

        var trozos = limpio.Split('.');

        if (trozos.Length == 0)
            return false;

        var numeros = new int[trozos.Length];

        for (var i = 0; i < trozos.Length; i++)
        {
            if (!int.TryParse(
                    trozos[i], NumberStyles.None, CultureInfo.InvariantCulture, out numeros[i]))
                return false;
        }

        partes = numeros;
        return true;
    }

    private static string ObtenerVersionLocal()
    {
        var ensamblado = Assembly.GetExecutingAssembly();

        var informativa = ensamblado
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informativa))
        {
            var limpia = informativa.Split('+')[0].Trim();

            if (!string.IsNullOrWhiteSpace(limpia))
                return limpia;
        }

        var version = ensamblado.GetName().Version;
        return version?.ToString(3) ?? "1.0.0";
    }
}
