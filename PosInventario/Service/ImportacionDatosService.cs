using System.Globalization;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Importa productos, proveedores y stock inicial desde un archivo CSV
/// exportado de Excel u otro sistema. La lectura no modifica la base: primero
/// valida todo y devuelve una vista previa; la importación se ejecuta después,
/// en una sola transacción.
/// </summary>
public class ImportacionDatosService
{
    private const int MaxSku = 40;
    private const int MinNombre = 3;
    private const int MaxNombre = 100;
    private const int MaxDescripcion = 500;
    private const int MinTelefono = 3;
    private const int MaxTelefono = 30;
    private const int MaxEmail = 120;
    private const int MaxDireccion = 200;

    private static readonly Dictionary<string, string> Columnas = new()
    {
        ["sku"] = "sku",
        ["codigo"] = "sku",
        ["codigo_barras"] = "sku",
        ["codigo_de_barras"] = "sku",
        ["codigo_de_barras_sku"] = "sku",
        ["clave"] = "sku",
        ["nombre"] = "nombre",
        ["producto"] = "nombre",
        ["articulo"] = "nombre",
        ["unidad"] = "unidad",
        ["unidad_medida"] = "unidad",
        ["tipo_unidad"] = "unidad",
        ["vende_por_peso"] = "unidad",
        ["por_peso"] = "unidad",
        ["descripcion"] = "descripcion",
        ["detalle"] = "descripcion",
        ["precio_venta"] = "precio_venta",
        ["precio"] = "precio_venta",
        ["precioventa"] = "precio_venta",
        ["precio_de_venta"] = "precio_venta",
        ["costo"] = "costo",
        ["costo_unitario"] = "costo",
        ["costounitario"] = "costo",
        ["stock_minimo"] = "stock_minimo",
        ["minimo"] = "stock_minimo",
        ["stock_min"] = "stock_minimo",
        ["stock_inicial"] = "stock_inicial",
        ["stock"] = "stock_inicial",
        ["existencia"] = "stock_inicial",
        ["existencias"] = "stock_inicial",
        ["cantidad"] = "stock_inicial",
        ["stock_actual"] = "stock_inicial",
        ["proveedor"] = "proveedor",
        ["nombre_proveedor"] = "proveedor",
        ["nombre_del_proveedor"] = "proveedor",
        ["proveedor_telefono"] = "proveedor_telefono",
        ["telefono"] = "proveedor_telefono",
        ["tel"] = "proveedor_telefono",
        ["proveedor_email"] = "proveedor_email",
        ["correo"] = "proveedor_email",
        ["email"] = "proveedor_email",
        ["correo_electronico"] = "proveedor_email",
        ["proveedor_direccion"] = "proveedor_direccion",
        ["direccion"] = "proveedor_direccion",
        ["domicilio"] = "proveedor_direccion"
    };

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public ImportacionDatosService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<Resultado<ImportacionVistaPrevia>> LeerAsync(string ruta)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado<ImportacionVistaPrevia>(false, autorizacion, null);

        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
            return new Resultado<ImportacionVistaPrevia>(
                false, "Selecciona un archivo CSV que exista.", null);

        var extension = Path.GetExtension(ruta).ToLowerInvariant();

        if (extension is not (".csv" or ".txt" or ".tsv"))
            return new Resultado<ImportacionVistaPrevia>(
                false,
                "El archivo debe ser CSV. En Excel usa Archivo → Guardar como → " +
                "CSV UTF-8. Los archivos .xlsx no se leen directamente.",
                null);

        string contenido;

        try
        {
            contenido = Decodificar(await File.ReadAllBytesAsync(ruta));
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException)
        {
            RegistroApp.Escribir("No se pudo leer el archivo de importación.", ex);

            return new Resultado<ImportacionVistaPrevia>(
                false, $"No se pudo leer el archivo: {ex.Message}", null);
        }

        var registros = ImportadorCsv.Leer(contenido);

        if (registros.Count == 0)
            return new Resultado<ImportacionVistaPrevia>(
                false, "El archivo está vacío.", null);

        var mapa = MapearColumnas(registros[0].Campos);
        var faltantes = new[] { "sku", "nombre", "precio_venta" }
            .Where(c => !mapa.ContainsKey(c))
            .ToList();

        if (faltantes.Count > 0)
        {
            var nombres = string.Join(", ", faltantes);
            return new Resultado<ImportacionVistaPrevia>(
                false,
                $"Faltan columnas obligatorias: {nombres}. " +
                "Descarga la plantilla para ver el formato esperado.",
                null);
        }

        var errores = new List<ErrorImportacion>();
        var filas = new List<FilaImportacion>();

        for (var i = 1; i < registros.Count; i++)
        {
            var fila = LeerFila(registros[i], mapa, errores);

            if (fila is not null)
                filas.Add(fila);
        }

        if (filas.Count == 0 && errores.Count == 0)
            return new Resultado<ImportacionVistaPrevia>(
                false, "El archivo no tiene filas de datos.", null);

        AgregarSkuRepetidos(filas, errores);

        var skus = filas
            .Select(f => f.Sku)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var productos = skus.Count == 0
            ? new List<Producto>()
            : await _db.Productos
                .AsNoTracking()
                .Where(p => skus.Contains(p.Sku))
                .ToListAsync();

        var porSku = productos.ToDictionary(
            p => p.Sku, StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            if (porSku.TryGetValue(fila.Sku, out var producto) &&
                producto.FechaArchivado is not null)
            {
                errores.Add(new ErrorImportacion(
                    fila.Fila,
                    $"El SKU {fila.Sku} está archivado. Restáuralo antes de importar."));
            }

            if (porSku.TryGetValue(fila.Sku, out producto) &&
                producto.FechaArchivado is null &&
                producto.UnidadMedida != fila.UnidadMedida)
            {
                errores.Add(new ErrorImportacion(
                    fila.Fila,
                    $"La unidad de venta de {fila.Sku} no coincide con la del " +
                    "producto existente. Edita el producto o corrige el archivo."));
            }
        }

        var telefonos = filas
            .Select(f => f.ProveedorTelefono)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var telefonosExistentes = telefonos.Count == 0
            ? new List<string>()
            : await _db.Proveedores
                .AsNoTracking()
                .Select(p => p.Telefono)
                .ToListAsync();

        var setExistentes = new HashSet<string>(
            telefonosExistentes, StringComparer.OrdinalIgnoreCase);

        var proveedoresNuevos = telefonos.Count(t => !setExistentes.Contains(t));
        var proveedoresReutilizados = telefonos.Count - proveedoresNuevos;

        var filasNuevas = filas
            .Where(f => !porSku.ContainsKey(f.Sku))
            .ToList();

        var vista = new ImportacionVistaPrevia
        {
            Ruta = ruta,
            Filas = filas,
            Errores = errores,
            ProductosNuevos = filasNuevas.Count,
            ProductosActualizados = filas.Count - filasNuevas.Count,
            ProveedoresNuevos = proveedoresNuevos,
            ProveedoresReutilizados = proveedoresReutilizados,
            UnidadesStock = filasNuevas.Sum(f => f.StockInicial)
        };

        return new Resultado<ImportacionVistaPrevia>(
            true, "Archivo leído.", vista);
    }

    public async Task<Resultado<ImportacionResultado>> ImportarAsync(
        ImportacionVistaPrevia vista)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado<ImportacionResultado>(false, autorizacion, null);

        if (vista is null || !vista.PuedeImportar)
            return new Resultado<ImportacionResultado>(
                false, "Corrige los errores del archivo antes de importar.", null);

        try
        {
            await using var transaccion = await _db.Database.BeginTransactionAsync();

            var ahora = DateTime.Now;
            var proveedores = await CrearProveedoresAsync(vista.Filas, ahora);

            var resultado = await CrearOActualizarProductosAsync(
                vista.Filas, ahora);

            await transaccion.CommitAsync();

            var mensaje =
                $"Importación completada: {resultado.ProductosNuevos} productos nuevos, " +
                $"{resultado.ProductosActualizados} actualizados, " +
                $"{proveedores.Nuevos} proveedores nuevos y " +
                $"{resultado.UnidadesStock.ToString("0.###", CultureInfo.InvariantCulture)} de stock inicial.";

            return new Resultado<ImportacionResultado>(
                true,
                mensaje,
                new ImportacionResultado(
                    resultado.ProductosNuevos,
                    resultado.ProductosActualizados,
                    proveedores.Nuevos,
                    resultado.UnidadesStock,
                    mensaje));
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo importar el archivo de datos.", ex);

            return new Resultado<ImportacionResultado>(
                false, $"No se pudo importar: {ex.Message}", null);
        }
    }

    private async Task<(int Nuevos, int Reutilizados)> CrearProveedoresAsync(
        IReadOnlyList<FilaImportacion> filas,
        DateTime ahora)
    {
        var telefonos = filas
            .Select(f => f.ProveedorTelefono)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (telefonos.Count == 0)
            return (0, 0);

        var existentes = await _db.Proveedores.ToListAsync();
        var setTelefonos = new HashSet<string>(
            telefonos, StringComparer.OrdinalIgnoreCase);

        var porTelefono = existentes
            .Where(p => setTelefonos.Contains(p.Telefono))
            .GroupBy(p => p.Telefono, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var nuevos = 0;

        foreach (var fila in filas)
        {
            var telefono = fila.ProveedorTelefono?.Trim();

            if (string.IsNullOrWhiteSpace(telefono) ||
                porTelefono.ContainsKey(telefono))
                continue;

            var proveedor = new Proveedor
            {
                Nombre = fila.ProveedorNombre!.Trim(),
                Telefono = telefono,
                Email = NormalizarOpcional(fila.ProveedorEmail),
                Direccion = NormalizarOpcional(fila.ProveedorDireccion),
                Activo = 1,
                FechaCreacion = ahora
            };

            _db.Proveedores.Add(proveedor);
            porTelefono[telefono] = proveedor;
            nuevos++;
        }

        return (nuevos, telefonos.Count - nuevos);
    }

    private async Task<(int ProductosNuevos, int ProductosActualizados, decimal UnidadesStock)>
        CrearOActualizarProductosAsync(
            IReadOnlyList<FilaImportacion> filas,
            DateTime ahora)
    {
        var skus = filas
            .Select(f => f.Sku)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existentes = await _db.Productos
            .Where(p => skus.Contains(p.Sku))
            .ToListAsync();

        var porSku = existentes.ToDictionary(
            p => p.Sku, StringComparer.OrdinalIgnoreCase);

        var nuevos = 0;
        var actualizados = 0;
        var unidades = 0m;

        foreach (var fila in filas)
        {
            if (porSku.TryGetValue(fila.Sku, out var producto))
            {
                if (producto.FechaArchivado is not null)
                    throw new InvalidOperationException(
                        $"El SKU {fila.Sku} está archivado.");

                producto.Nombre = fila.Nombre;
                producto.Descripcion = NormalizarOpcional(fila.Descripcion);
                producto.PrecioVenta = fila.PrecioVenta;
                producto.Costo = fila.Costo;
                producto.StockMinimo = fila.StockMinimo;
                producto.FechaModificacion = ahora;
                actualizados++;
                continue;
            }

            var nuevo = new Producto
            {
                Sku = fila.Sku,
                Nombre = fila.Nombre,
                Descripcion = NormalizarOpcional(fila.Descripcion),
                UnidadMedida = fila.UnidadMedida,
                PrecioVenta = fila.PrecioVenta,
                Costo = fila.Costo,
                Stock = fila.StockInicial,
                StockMinimo = fila.StockMinimo,
                FechaCreacion = ahora,
                FechaModificacion = ahora
            };

            _db.Productos.Add(nuevo);
            porSku[fila.Sku] = nuevo;
            nuevos++;
            unidades += fila.StockInicial;

            if (fila.StockInicial > 0)
            {
                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    Producto = nuevo,
                    IdUsuario = _sesion.IdUsuario,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = fila.StockInicial,
                    StockAnterior = 0,
                    StockResultante = fila.StockInicial,
                    Referencia = "Importación inicial",
                    Fecha = ahora
                });
            }
        }

        await _db.SaveChangesAsync();
        return (nuevos, actualizados, unidades);
    }

    private static FilaImportacion? LeerFila(
        RegistroCsv registro,
        IReadOnlyDictionary<string, int> mapa,
        List<ErrorImportacion> errores)
    {
        var mensajes = new List<string>();

        string Campo(string clave)
            => mapa.TryGetValue(clave, out var indice) &&
               indice < registro.Campos.Count
                ? registro.Campos[indice].Trim()
                : string.Empty;

        var sku = Campo("sku").ToUpperInvariant();

        if (sku.Length == 0)
            mensajes.Add("El SKU es obligatorio.");
        else if (sku.Length > MaxSku)
            mensajes.Add($"El SKU no puede superar los {MaxSku} caracteres.");
        else if (sku.Any(char.IsWhiteSpace))
            mensajes.Add("El SKU no puede contener espacios.");

        var nombre = Campo("nombre");

        if (nombre.Length == 0)
            mensajes.Add("El nombre es obligatorio.");
        else if (nombre.Length < MinNombre)
            mensajes.Add($"El nombre debe tener al menos {MinNombre} caracteres.");
        else if (nombre.Length > MaxNombre)
            mensajes.Add($"El nombre no puede superar los {MaxNombre} caracteres.");

        var unidad = LeerUnidad(Campo("unidad"), mensajes);
        var descripcion = Campo("descripcion");

        if (descripcion.Length > MaxDescripcion)
            mensajes.Add(
                $"La descripción no puede superar los {MaxDescripcion} caracteres.");

        if (!ImportadorCsv.TryParseDecimal(Campo("precio_venta"), out var precio))
            mensajes.Add("El precio de venta no es un número válido.");
        else if (precio < 0)
            mensajes.Add("El precio de venta no puede ser negativo.");

        if (!ImportadorCsv.TryParseDecimal(Campo("costo"), out var costo))
            mensajes.Add("El costo no es un número válido.");
        else if (costo < 0)
            mensajes.Add("El costo no puede ser negativo.");

        if (!ImportadorCsv.TryParseDecimal(Campo("stock_minimo"), out var stockMinimo))
            mensajes.Add("El stock mínimo no es un número válido.");
        else if (stockMinimo < 0)
            mensajes.Add("El stock mínimo no puede ser negativo.");
        else if (Cantidades.TieneDemasiadosDecimales(stockMinimo))
            mensajes.Add("El stock mínimo no puede tener más de 3 decimales.");
        else if (unidad == UnidadMedida.Pieza &&
                 !Cantidades.EsEntera(stockMinimo))
            mensajes.Add("El stock mínimo debe ser entero para productos por pieza.");

        if (!ImportadorCsv.TryParseDecimal(Campo("stock_inicial"), out var stockInicial))
            mensajes.Add("El stock inicial no es un número válido.");
        else if (stockInicial < 0)
            mensajes.Add("El stock inicial no puede ser negativo.");
        else if (Cantidades.TieneDemasiadosDecimales(stockInicial))
            mensajes.Add("El stock inicial no puede tener más de 3 decimales.");
        else if (unidad == UnidadMedida.Pieza &&
                 !Cantidades.EsEntera(stockInicial))
            mensajes.Add("El stock inicial debe ser entero para productos por pieza.");

        var proveedorNombre = Campo("proveedor");
        var proveedorTelefono = Campo("proveedor_telefono");
        var proveedorEmail = Campo("proveedor_email");
        var proveedorDireccion = Campo("proveedor_direccion");

        var hayDatosProveedor =
            proveedorNombre.Length > 0 ||
            proveedorTelefono.Length > 0 ||
            proveedorEmail.Length > 0 ||
            proveedorDireccion.Length > 0;

        if (hayDatosProveedor)
        {
            if (proveedorNombre.Length == 0)
                mensajes.Add("El nombre del proveedor es obligatorio.");
            else if (proveedorNombre.Length < MinNombre)
                mensajes.Add(
                    $"El nombre del proveedor debe tener al menos {MinNombre} caracteres.");
            else if (proveedorNombre.Length > MaxNombre)
                mensajes.Add(
                    $"El nombre del proveedor no puede superar los {MaxNombre} caracteres.");

            if (proveedorTelefono.Length == 0)
                mensajes.Add("El teléfono del proveedor es obligatorio.");
            else if (proveedorTelefono.Length < MinTelefono)
                mensajes.Add(
                    $"El teléfono del proveedor debe tener al menos {MinTelefono} caracteres.");
            else if (proveedorTelefono.Length > MaxTelefono)
                mensajes.Add(
                    $"El teléfono del proveedor no puede superar los {MaxTelefono} caracteres.");

            if (proveedorEmail.Length > MaxEmail)
                mensajes.Add(
                    $"El correo del proveedor no puede superar los {MaxEmail} caracteres.");
            else if (proveedorEmail.Length > 0 && !EsEmailValido(proveedorEmail))
                mensajes.Add("El correo del proveedor no tiene un formato válido.");

            if (proveedorDireccion.Length > MaxDireccion)
                mensajes.Add(
                    $"La dirección del proveedor no puede superar los {MaxDireccion} caracteres.");
        }

        if (mensajes.Count > 0)
        {
            foreach (var mensaje in mensajes)
                errores.Add(new ErrorImportacion(registro.Linea, mensaje));

            return null;
        }

        return new FilaImportacion(
            registro.Linea,
            sku,
            nombre,
            unidad,
            NormalizarOpcional(descripcion),
            precio,
            costo,
            Cantidades.Normalizar(stockMinimo),
            Cantidades.Normalizar(stockInicial),
            NormalizarOpcional(proveedorNombre),
            NormalizarOpcional(proveedorTelefono),
            NormalizarOpcional(proveedorEmail),
            NormalizarOpcional(proveedorDireccion));
    }

    private static UnidadMedida LeerUnidad(
        string texto, List<string> mensajes)
    {
        var normalizado = ImportadorCsv.NormalizarEncabezado(texto);

        if (normalizado.Length == 0 ||
            normalizado is "pieza" or "piezas" or "pza" or "unidad" or
            "unidades" or "no" or "false" or "0")
            return UnidadMedida.Pieza;

        if (normalizado is "kg" or "kilo" or "kilos" or "kilogramo" or
            "kilogramos" or "peso" or "si" or "true" or "1")
            return UnidadMedida.Kilogramo;

        mensajes.Add("La unidad debe ser pieza o kg.");
        return UnidadMedida.Pieza;
    }

    private static Dictionary<string, int> MapearColumnas(
        IReadOnlyList<string> encabezados)
    {
        var mapa = new Dictionary<string, int>();

        for (var i = 0; i < encabezados.Count; i++)
        {
            var normalizado = ImportadorCsv.NormalizarEncabezado(encabezados[i]);

            if (Columnas.TryGetValue(normalizado, out var canonica) &&
                !mapa.ContainsKey(canonica))
                mapa[canonica] = i;
        }

        return mapa;
    }

    private static void AgregarSkuRepetidos(
        IReadOnlyList<FilaImportacion> filas,
        List<ErrorImportacion> errores)
    {
        foreach (var grupo in filas
                     .GroupBy(f => f.Sku, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))
        {
            var primera = grupo.First().Fila;

            foreach (var fila in grupo.Skip(1))
                errores.Add(new ErrorImportacion(
                    fila.Fila,
                    $"El SKU {fila.Sku} está repetido en el archivo " +
                    $"(también aparece en la fila {primera})."));
        }
    }

    private static string Decodificar(byte[] bytes)
    {
        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private string? ValidarAdministrador()
        => _sesion.EsAdministrador
            ? null
            : "Solo un administrador puede importar datos.";

    private static string? NormalizarOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static bool EsEmailValido(string email)
        => MailAddress.TryCreate(email, out var direccion) &&
           string.Equals(direccion.Address, email, StringComparison.OrdinalIgnoreCase);
}
