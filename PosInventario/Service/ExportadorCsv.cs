using System.Globalization;
using System.Text;
using PosInventario.Models;

namespace PosInventario.Service;

/// <summary>
/// Genera archivos CSV compatibles con Excel a partir de los datos que ya
/// muestran los módulos de Ventas, Inventario y Compras. Usa coma como
/// separador, punto decimal y UTF-8 con BOM para conservar los acentos.
/// </summary>
public static class ExportadorCsv
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private const string FinLinea = "\r\n";

    public static string Ventas(IEnumerable<Venta> ventas)
    {
        var contenido = new StringBuilder();

        contenido.Append(Linea(
            "Ticket", "Fecha", "Cajero", "Método", "Estado",
            "Subtotal", "Descuento", "Total", "Devuelto", "Neto"));

        foreach (var venta in ventas)
        {
            contenido.Append(Linea(
                $"V-{venta.Id:000000}",
                Fecha(venta.Fecha),
                venta.Usuario?.Nombre ?? string.Empty,
                venta.MetodoPago.ToString(),
                venta.Estado.ToString(),
                Monto(venta.Subtotal),
                Monto(venta.Descuento),
                Monto(venta.Total),
                Monto(venta.TotalDevuelto),
                Monto(venta.TotalNeto)));
        }

        return contenido.ToString();
    }

    public static string Inventario(IEnumerable<MovimientoInventario> movimientos)
    {
        var contenido = new StringBuilder();

        contenido.Append(Linea(
            "Fecha", "Producto", "SKU", "Tipo", "Cantidad",
            "Stock anterior", "Stock resultante", "Referencia", "Usuario"));

        foreach (var movimiento in movimientos)
        {
            contenido.Append(Linea(
                Fecha(movimiento.Fecha),
                movimiento.Producto?.Nombre ?? string.Empty,
                movimiento.Producto?.Sku ?? string.Empty,
                movimiento.Tipo.ToString(),
                movimiento.Cantidad.ToString(Cultura),
                movimiento.StockAnterior.ToString(Cultura),
                movimiento.StockResultante.ToString(Cultura),
                movimiento.Referencia ?? string.Empty,
                movimiento.Usuario?.Nombre ?? string.Empty));
        }

        return contenido.ToString();
    }

    public static string Compras(IEnumerable<Compra> compras)
    {
        var contenido = new StringBuilder();

        contenido.Append(Linea(
            "Compra", "Fecha", "Proveedor", "Comprobante",
            "Estado", "Artículos", "Total", "Usuario"));

        foreach (var compra in compras)
        {
            contenido.Append(Linea(
                $"C-{compra.Id:000000}",
                Fecha(compra.Fecha),
                compra.Proveedor?.Nombre ?? string.Empty,
                compra.NumeroComprobante ?? string.Empty,
                compra.Estado.ToString(),
                compra.Detalles.Sum(d => d.Cantidad).ToString(Cultura),
                Monto(compra.Total),
                compra.Usuario?.Nombre ?? string.Empty));
        }

        return contenido.ToString();
    }

    public static string NombreArchivo(
        string prefijo, DateTime desde, DateTime hasta, int pagina = 1)
    {
        var nombre = $"{prefijo}_{desde:yyyy-MM-dd}_{hasta:yyyy-MM-dd}";

        // La exportación incluye solo la página visible. Numerar las páginas
        // evita que una exportación sobrescriba a la anterior.
        if (pagina > 1)
            nombre += $"_pagina{pagina}";

        return nombre + ".csv";
    }

    /// <summary>
    /// Escribe el contenido con UTF-8 y BOM, que es lo que Excel usa para
    /// reconocer los acentos sin pedir una importación manual.
    /// </summary>
    public static async Task EscribirAsync(Stream destino, string contenido)
    {
        await using var escritor = new StreamWriter(
            destino,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            bufferSize: 1024,
            leaveOpen: true);

        await escritor.WriteAsync(contenido);
        await escritor.FlushAsync();
    }

    private static string Linea(params string[] campos)
        => string.Join(",", campos.Select(Campo)) + FinLinea;

    private static string Campo(string? valor)
    {
        var texto = valor ?? string.Empty;

        // Excel interpreta como fórmula cualquier texto que empiece con estos
        // caracteres. El apóstrofo lo obliga a tratarlo como texto.
        var inicio = texto.TrimStart();

        if (inicio.Length > 0 && "=+-@".Contains(inicio[0]))
            texto = "'" + texto;

        if (texto.Contains('"') || texto.Contains(',') ||
            texto.Contains('\n') || texto.Contains('\r'))
            texto = "\"" + texto.Replace("\"", "\"\"") + "\"";

        return texto;
    }

    private static string Fecha(DateTime fecha)
        => fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    private static string Monto(decimal monto)
        => monto.ToString("0.00", Cultura);
}
