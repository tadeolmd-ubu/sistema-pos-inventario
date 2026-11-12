using System.Text.Json;

namespace PosInventario.Service;

/// <summary>
/// Lee y guarda las preferencias locales en un archivo JSON. Si el archivo no
/// existe o está dañado se usan los valores predeterminados.
/// </summary>
public class ConfiguracionAppService
{
    public const int RetencionDiasMinima = 1;
    public const int RetencionDiasMaxima = 365;
    public const int RetencionDiasPredeterminada = 30;

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _rutaArchivo;

    public ConfiguracionApp Actual { get; private set; }

    public ConfiguracionAppService(string? rutaArchivo = null)
    {
        _rutaArchivo = string.IsNullOrWhiteSpace(rutaArchivo)
            ? RutaPredeterminada
            : Path.GetFullPath(rutaArchivo);

        Actual = Cargar();
    }

    public static string RutaPredeterminada => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SistemaInventario",
        "configuracion.json");

    public Resultado<ConfiguracionApp> Guardar(
        bool respaldosAutomaticos, int retencionDias)
    {
        if (retencionDias < RetencionDiasMinima ||
            retencionDias > RetencionDiasMaxima)
            return new Resultado<ConfiguracionApp>(
                false,
                $"La retención debe estar entre {RetencionDiasMinima} y " +
                $"{RetencionDiasMaxima} días.",
                null);

        var nueva = new ConfiguracionApp(respaldosAutomaticos, retencionDias);
        string? temporal = null;

        try
        {
            var carpeta = Path.GetDirectoryName(_rutaArchivo);

            if (!string.IsNullOrWhiteSpace(carpeta))
                Directory.CreateDirectory(carpeta);

            temporal = _rutaArchivo + ".tmp";
            File.WriteAllText(
                temporal, JsonSerializer.Serialize(nueva, OpcionesJson));

            File.Move(temporal, _rutaArchivo, overwrite: true);
            temporal = null;

            Actual = nueva;

            return new Resultado<ConfiguracionApp>(
                true, "Configuración guardada.", Actual);
        }
        catch (UnauthorizedAccessException)
        {
            return new Resultado<ConfiguracionApp>(
                false, "No hay permisos para guardar la configuración.", null);
        }
        catch (IOException)
        {
            return new Resultado<ConfiguracionApp>(
                false, "No se pudo guardar la configuración.", null);
        }
        catch (NotSupportedException)
        {
            return new Resultado<ConfiguracionApp>(
                false, "La ruta de la configuración no es compatible.", null);
        }
        finally
        {
            if (temporal is not null)
                EliminarTemporal(temporal);
        }
    }

    private ConfiguracionApp Cargar()
    {
        try
        {
            if (!File.Exists(_rutaArchivo))
                return new ConfiguracionApp();

            var json = File.ReadAllText(_rutaArchivo);
            var configuracion = JsonSerializer.Deserialize<ConfiguracionJson>(
                json, OpcionesJson);

            if (configuracion is null)
                return new ConfiguracionApp();

            var dias = configuracion.RetencionDias
                ?? RetencionDiasPredeterminada;

            return new ConfiguracionApp(
                configuracion.RespaldosAutomaticos ?? true,
                Math.Clamp(dias, RetencionDiasMinima, RetencionDiasMaxima));
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or
            JsonException or NotSupportedException)
        {
            RegistroApp.Escribir(
                "No se pudo leer la configuración; se usan valores predeterminados.",
                ex);

            return new ConfiguracionApp();
        }
    }

    private static void EliminarTemporal(string ruta)
    {
        try { File.Delete(ruta); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class ConfiguracionJson
    {
        public bool? RespaldosAutomaticos { get; set; }
        public int? RetencionDias { get; set; }
    }
}
