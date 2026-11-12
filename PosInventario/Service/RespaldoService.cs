using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;

namespace PosInventario.Service;

public class RespaldoService
{
    private const string FirmaSqlite = "SQLite format 3\0";

    private static readonly string[] TablasRequeridas =
    [
        "Usuario",
        "Producto",
        "Caja",
        "Venta",
        "__EFMigrationsHistory"
    ];

    private readonly AppDbContext _db;
    private readonly SesionActual? _sesion;

    public RespaldoService(AppDbContext db, SesionActual? sesion = null)
    {
        _db = db;
        _sesion = sesion;
    }

    public Task<Resultado<string>> CrearRespaldoAsync(string carpetaDestino)
        => CrearRespaldoInternoAsync(carpetaDestino, "respaldo");

    /// <summary>
    /// Respaldo identificable como automático para que la retención pueda
    /// limpiarlo sin borrar los respaldos manuales.
    /// </summary>
    public Task<Resultado<string>> CrearRespaldoAutomaticoAsync(string carpetaDestino)
        => CrearRespaldoInternoAsync(carpetaDestino, "respaldo_auto");

    private async Task<Resultado<string>> CrearRespaldoInternoAsync(
        string carpetaDestino, string prefijo)
    {
        if (string.IsNullOrWhiteSpace(carpetaDestino))
            return new Resultado<string>(
                false, "La carpeta de destino es obligatoria.", null);

        string? rutaTemporal = null;

        try
        {
            var cadenaConexion = _db.Database.GetConnectionString();

            if (string.IsNullOrWhiteSpace(cadenaConexion))
                return new Resultado<string>(
                    false, "No se encontró la conexión de la base de datos.", null);

            var opcionesOrigen = new SqliteConnectionStringBuilder(cadenaConexion);

            if (opcionesOrigen.Mode == SqliteOpenMode.Memory ||
                opcionesOrigen.DataSource == ":memory:" ||
                string.IsNullOrWhiteSpace(opcionesOrigen.DataSource))
                return new Resultado<string>(
                    false, "El respaldo requiere una base de datos guardada en disco.", null);

            opcionesOrigen.Mode = SqliteOpenMode.ReadOnly;
            opcionesOrigen.Pooling = false;

            var carpeta = Path.GetFullPath(carpetaDestino.Trim());
            Directory.CreateDirectory(carpeta);

            var identificador = Guid.NewGuid().ToString("N");
            var nombreArchivo =
                $"{prefijo}_{DateTime.Now:yyyyMMdd_HHmmss}_{identificador}.db";
            var rutaDestino = Path.Combine(carpeta, nombreArchivo);

            rutaTemporal = Path.Combine(carpeta, $".respaldo_{identificador}.tmp");

            var opcionesDestino = new SqliteConnectionStringBuilder
            {
                DataSource = rutaTemporal,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            };

            await using (var origen = new SqliteConnection(opcionesOrigen.ToString()))
            {
                await using var destino =
                    new SqliteConnection(opcionesDestino.ToString());

                await origen.OpenAsync();
                await destino.OpenAsync();
                origen.BackupDatabase(destino);
            }

            var verificacion = await ValidarRespaldoGeneradoAsync(rutaTemporal);

            if (!verificacion.Correcta)
            {
                RegistroApp.Escribir(
                    "El respaldo generado no pasó la verificación de integridad. " +
                    verificacion.Mensaje);

                return new Resultado<string>(
                    false,
                    "El respaldo no pasó la verificación de integridad y no se " +
                    "guardó. " + verificacion.Mensaje,
                    null);
            }

            File.Move(rutaTemporal, rutaDestino);

            // La validación abre el temporal como SQLite y puede dejar sus
            // archivos auxiliares; se limpian antes de darlo por terminado.
            EliminarTemporal(rutaTemporal);
            rutaTemporal = null;

            return new Resultado<string>(true, "Respaldo creado.", rutaDestino);
        }
        catch (SqliteException)
        {
            return new Resultado<string>(
                false, "No se pudo respaldar la base de datos.", null);
        }
        catch (UnauthorizedAccessException)
        {
            return new Resultado<string>(
                false, "No hay permisos para escribir en la carpeta seleccionada.", null);
        }
        catch (IOException)
        {
            return new Resultado<string>(
                false, "No se pudo escribir el archivo de respaldo.", null);
        }
        catch (ArgumentException)
        {
            return new Resultado<string>(
                false, "La ruta de destino no es válida.", null);
        }
        catch (NotSupportedException)
        {
            return new Resultado<string>(
                false, "El formato de la ruta de destino no es compatible.", null);
        }
        finally
        {
            if (rutaTemporal is not null)
                EliminarTemporal(rutaTemporal);
        }
    }

    /// <summary>
    /// Comprueba que el archivo sea una base SQLite íntegra y tenga el esquema
    /// mínimo del sistema. No modifica el archivo ni la base actual.
    /// </summary>
    public async Task<Resultado<RespaldoInfo>> ValidarRespaldoAsync(string rutaRespaldo)
    {
        if (string.IsNullOrWhiteSpace(rutaRespaldo))
            return new Resultado<RespaldoInfo>(
                false, "Selecciona un archivo de respaldo.", null);

        string ruta;

        try
        {
            ruta = Path.GetFullPath(rutaRespaldo.Trim());
        }
        catch (Exception ex) when (
            ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new Resultado<RespaldoInfo>(
                false, "La ruta del respaldo no es válida.", null);
        }

        if (!File.Exists(ruta))
            return new Resultado<RespaldoInfo>(
                false, "El archivo de respaldo no existe.", null);

        try
        {
            if (!await TieneFirmaSqliteAsync(ruta))
                return new Resultado<RespaldoInfo>(
                    false, "El archivo no es una base de datos SQLite válida.", null);
        }
        catch (UnauthorizedAccessException)
        {
            return new Resultado<RespaldoInfo>(
                false, "No hay permisos para leer el archivo seleccionado.", null);
        }
        catch (IOException)
        {
            return new Resultado<RespaldoInfo>(
                false, "No se pudo leer el archivo seleccionado.", null);
        }

        try
        {
            var opciones = new SqliteConnectionStringBuilder
            {
                DataSource = ruta,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };

            await using var conexion = new SqliteConnection(opciones.ToString());
            await conexion.OpenAsync();

            if (!await IntegridadCorrectaAsync(conexion))
                return new Resultado<RespaldoInfo>(
                    false, "El archivo de respaldo está dañado o incompleto.", null);

            if (!await TieneEsquemaDelSistemaAsync(conexion))
                return new Resultado<RespaldoInfo>(
                    false, "El archivo no contiene el esquema del sistema.", null);

            var migraciones = await ContarMigracionesAsync(conexion);

            if (migraciones == 0)
                return new Resultado<RespaldoInfo>(
                    false, "El respaldo no tiene historial de versiones del sistema.", null);

            var archivo = new FileInfo(ruta);

            return new Resultado<RespaldoInfo>(
                true,
                "El respaldo es válido y se puede restaurar.",
                new RespaldoInfo(
                    ruta,
                    archivo.Length,
                    archivo.LastWriteTime,
                    migraciones));
        }
        catch (SqliteException)
        {
            return new Resultado<RespaldoInfo>(
                false,
                "El archivo de respaldo está dañado o no es una base SQLite compatible.",
                null);
        }
        catch (UnauthorizedAccessException)
        {
            return new Resultado<RespaldoInfo>(
                false, "No hay permisos para leer el archivo seleccionado.", null);
        }
        catch (IOException)
        {
            return new Resultado<RespaldoInfo>(
                false, "No se pudo leer el archivo seleccionado.", null);
        }
    }

    /// <summary>
    /// Verificación aplicada al archivo que se acaba de generar. Es virtual
    /// para poder simular un respaldo inválido en las pruebas.
    /// </summary>
    protected virtual Task<Resultado<RespaldoInfo>> ValidarRespaldoGeneradoAsync(
        string ruta)
        => ValidarRespaldoAsync(ruta);

    /// <summary>
    /// Valida el respaldo, guarda una copia de seguridad de la base actual y
    /// reemplaza su contenido con el del respaldo. La aplicación debe
    /// reiniciarse después para volver a leer la base.
    /// </summary>
    public async Task<Resultado<RestauracionRespaldo>> RestaurarRespaldoAsync(
        string rutaRespaldo, string carpetaRespaldoActual)
    {
        if (_sesion is not null && !_sesion.EsAdministrador)
            return new Resultado<RestauracionRespaldo>(
                false, "Solo un administrador puede restaurar un respaldo.", null);

        if (string.IsNullOrWhiteSpace(carpetaRespaldoActual))
            return new Resultado<RestauracionRespaldo>(
                false, "La carpeta para el respaldo de seguridad es obligatoria.", null);

        if (!TryObtenerRutaBaseDatos(out var rutaBaseDatos))
            return new Resultado<RestauracionRespaldo>(
                false,
                "La restauración requiere una base de datos guardada en disco.",
                null);

        var validacion = await ValidarRespaldoAsync(rutaRespaldo);

        if (!validacion.Correcta || validacion.Valor is null)
            return new Resultado<RestauracionRespaldo>(
                false, validacion.Mensaje, null);

        var rutaValidada = validacion.Valor.Ruta;

        if (string.Equals(
                Path.GetFullPath(rutaBaseDatos),
                rutaValidada,
                StringComparison.OrdinalIgnoreCase))
            return new Resultado<RestauracionRespaldo>(
                false, "El archivo seleccionado es la base de datos actual.", null);

        var respaldoSeguridad = await CrearRespaldoAsync(carpetaRespaldoActual);

        if (!respaldoSeguridad.Correcta || respaldoSeguridad.Valor is null)
            return new Resultado<RestauracionRespaldo>(
                false,
                "No se pudo respaldar la base actual; no se restauró nada. " +
                respaldoSeguridad.Mensaje,
                null);

        try
        {
            // La restauración reemplaza el archivo de la base actual: primero
            // hay que soltar la conexión del contexto y las conexiones del pool.
            await _db.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();

            var carpetaBaseDatos = Path.GetDirectoryName(rutaBaseDatos)
                ?? Directory.GetCurrentDirectory();

            Directory.CreateDirectory(carpetaBaseDatos);

            var rutaTemporal = Path.Combine(
                carpetaBaseDatos,
                $".restaurando_{Guid.NewGuid():N}.db");

            try
            {
                File.Copy(rutaValidada, rutaTemporal, overwrite: true);

                // Si la base anterior quedó en modo WAL, sus archivos auxiliares
                // contienen páginas viejas que no deben aplicarse al respaldo.
                EliminarArchivo(rutaBaseDatos + "-wal");
                EliminarArchivo(rutaBaseDatos + "-shm");
                EliminarArchivo(rutaBaseDatos + "-journal");

                File.Move(rutaTemporal, rutaBaseDatos, overwrite: true);
            }
            finally
            {
                EliminarTemporal(rutaTemporal);
            }

            return new Resultado<RestauracionRespaldo>(
                true,
                "Respaldo restaurado. La aplicación se reiniciará.",
                new RestauracionRespaldo(
                    rutaBaseDatos, respaldoSeguridad.Valor));
        }
        catch (SqliteException)
        {
            return new Resultado<RestauracionRespaldo>(
                false,
                "No se pudo reemplazar la base de datos. " +
                $"La base anterior sigue en su lugar y su respaldo está en: {respaldoSeguridad.Valor}",
                null);
        }
        catch (UnauthorizedAccessException)
        {
            return new Resultado<RestauracionRespaldo>(
                false,
                "No hay permisos para reemplazar la base de datos. " +
                $"La base anterior sigue en su lugar y su respaldo está en: {respaldoSeguridad.Valor}",
                null);
        }
        catch (IOException)
        {
            return new Resultado<RestauracionRespaldo>(
                false,
                "No se pudo escribir la base restaurada. " +
                $"La base anterior sigue en su lugar y su respaldo está en: {respaldoSeguridad.Valor}",
                null);
        }
    }

    private bool TryObtenerRutaBaseDatos(out string ruta)
    {
        ruta = string.Empty;

        var cadenaConexion = _db.Database.GetConnectionString();

        if (string.IsNullOrWhiteSpace(cadenaConexion))
            return false;

        var opciones = new SqliteConnectionStringBuilder(cadenaConexion);

        if (opciones.Mode == SqliteOpenMode.Memory ||
            opciones.DataSource == ":memory:" ||
            string.IsNullOrWhiteSpace(opciones.DataSource))
            return false;

        ruta = Path.GetFullPath(opciones.DataSource);
        return true;
    }

    private static async Task<bool> TieneFirmaSqliteAsync(string ruta)
    {
        await using var archivo = new FileStream(
            ruta, FileMode.Open, FileAccess.Read, FileShare.Read);

        var cabecera = new byte[16];
        var leidos = 0;

        while (leidos < cabecera.Length)
        {
            var cantidad = await archivo.ReadAsync(cabecera.AsMemory(leidos));

            if (cantidad == 0)
                break;

            leidos += cantidad;
        }

        return leidos == cabecera.Length &&
               Encoding.ASCII.GetString(cabecera) == FirmaSqlite;
    }

    private static async Task<bool> IntegridadCorrectaAsync(SqliteConnection conexion)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "PRAGMA integrity_check;";

        var resultados = new List<string>();

        await using var lector = await comando.ExecuteReaderAsync();

        while (await lector.ReadAsync())
            resultados.Add(lector.GetString(0));

        return resultados.Count == 1 &&
               string.Equals(resultados[0], "ok", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> TieneEsquemaDelSistemaAsync(SqliteConnection conexion)
    {
        await using var comando = conexion.CreateCommand();

        var nombres = string.Join(
            ", ", TablasRequeridas.Select(t => $"'{t}'"));

        comando.CommandText =
            "SELECT COUNT(*) FROM sqlite_master " +
            $"WHERE type = 'table' AND name IN ({nombres});";

        var total = Convert.ToInt32(await comando.ExecuteScalarAsync());

        return total == TablasRequeridas.Length;
    }

    private static async Task<int> ContarMigracionesAsync(SqliteConnection conexion)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";

        return Convert.ToInt32(await comando.ExecuteScalarAsync());
    }

    private static void EliminarTemporal(string ruta)
    {
        EliminarArchivo(ruta);
        EliminarArchivo(ruta + "-wal");
        EliminarArchivo(ruta + "-shm");
        EliminarArchivo(ruta + "-journal");
    }

    private static void EliminarArchivo(string ruta)
    {
        try { File.Delete(ruta); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
