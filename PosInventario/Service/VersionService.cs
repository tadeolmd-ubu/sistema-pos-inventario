using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace PosInventario.Service;

/// <summary>
/// Obtiene la última versión publicada en GitHub. Si el repositorio no es
/// accesible (privado o sin conexión), usa la versión del ensamblado.
/// </summary>
public class VersionService
{
    private const string Repositorio = "tadeolmd-ubu/sistema-pos-inventario";
    private const string UrlUltimaRelease =
        $"https://api.github.com/repos/{Repositorio}/releases/latest";

    private static readonly HttpClient Cliente = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public string VersionLocal { get; } = ObtenerVersionLocal();

    public async Task<string> ObtenerVersionAsync()
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

            using var respuesta = await Cliente.SendAsync(solicitud);

            if (!respuesta.IsSuccessStatusCode)
                return VersionLocal;

            using var documento = JsonDocument.Parse(
                await respuesta.Content.ReadAsStringAsync());

            if (!documento.RootElement.TryGetProperty("tag_name", out var tag) ||
                tag.GetString() is not { Length: > 0 } etiqueta)
                return VersionLocal;

            return etiqueta.TrimStart('v', 'V');
        }
        catch
        {
            return VersionLocal;
        }
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
