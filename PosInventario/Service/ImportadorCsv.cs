using System.Globalization;
using System.Text;

namespace PosInventario.Service;

/// <summary>
/// Lector de archivos separados por comas, punto y coma o tabuladores.
/// Acepta campos entre comillas, comillas escapadas y saltos de línea dentro
/// de un campo, que es lo que produce Excel al exportar a CSV.
/// </summary>
public static class ImportadorCsv
{
    public const string EncabezadoPlantilla =
        "sku,nombre,unidad,descripcion,precio_venta,costo,stock_minimo," +
        "stock_inicial,proveedor,proveedor_telefono,proveedor_email," +
        "proveedor_direccion";

    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public static string Plantilla => EncabezadoPlantilla + "\r\n";

    public static List<RegistroCsv> Leer(string contenido)
    {
        var registros = new List<RegistroCsv>();

        if (string.IsNullOrEmpty(contenido))
            return registros;

        var delimitador = DetectarDelimitador(contenido);
        var campos = new List<string>();
        var campo = new StringBuilder();
        var enComillas = false;
        var linea = 1;
        var lineaInicio = 1;

        for (var i = 0; i < contenido.Length; i++)
        {
            var caracter = contenido[i];

            if (enComillas)
            {
                if (caracter == '"')
                {
                    if (i + 1 < contenido.Length && contenido[i + 1] == '"')
                    {
                        campo.Append('"');
                        i++;
                    }
                    else
                    {
                        enComillas = false;
                    }
                }
                else
                {
                    if (caracter == '\n')
                        linea++;

                    campo.Append(caracter);
                }

                continue;
            }

            switch (caracter)
            {
                case '"':
                    enComillas = true;
                    break;

                case '\r':
                    break;

                case '\n':
                    campos.Add(campo.ToString());
                    campo.Clear();
                    AgregarRegistro(registros, lineaInicio, campos);
                    campos = [];
                    linea++;
                    lineaInicio = linea;
                    break;

                default:
                    if (caracter == delimitador)
                    {
                        campos.Add(campo.ToString());
                        campo.Clear();
                    }
                    else
                    {
                        campo.Append(caracter);
                    }

                    break;
            }
        }

        if (campo.Length > 0 || campos.Count > 0)
        {
            campos.Add(campo.ToString());
            AgregarRegistro(registros, lineaInicio, campos);
        }

        return registros;
    }

    public static char DetectarDelimitador(string contenido)
    {
        var conteo = new Dictionary<char, int>
        {
            [','] = 0,
            [';'] = 0,
            ['\t'] = 0
        };

        var enComillas = false;

        foreach (var caracter in contenido)
        {
            if (caracter == '"')
            {
                enComillas = !enComillas;
                continue;
            }

            if (enComillas)
                continue;

            if (caracter == '\n')
                break;

            if (conteo.ContainsKey(caracter))
                conteo[caracter]++;
        }

        var elegido = conteo.OrderByDescending(p => p.Value).First();
        return elegido.Value > 0 ? elegido.Key : ',';
    }

    public static string NormalizarEncabezado(string valor)
    {
        var descompuesto = (valor ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var resultado = new StringBuilder();

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) ==
                UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(caracter) || caracter == '-')
                resultado.Append('_');
            else if (char.IsLetterOrDigit(caracter) || caracter == '_')
                resultado.Append(caracter);
        }

        return resultado.ToString();
    }

    /// <summary>
    /// Acepta importes como 1234.56, 1,234.56, 1234,56 y 1.234,56.
    /// Un valor vacío se interpreta como cero.
    /// </summary>
    public static bool TryParseDecimal(string? texto, out decimal valor)
    {
        valor = 0;

        var limpio = (texto ?? string.Empty)
            .Trim()
            .Replace("$", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("MXN", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (limpio.Length == 0)
            return true;

        var tieneComa = limpio.Contains(',');
        var tienePunto = limpio.Contains('.');

        if (tieneComa && tienePunto)
        {
            if (limpio.LastIndexOf(',') > limpio.LastIndexOf('.'))
                limpio = limpio.Replace(".", string.Empty).Replace(',', '.');
            else
                limpio = limpio.Replace(",", string.Empty);
        }
        else if (tieneComa)
        {
            var partes = limpio.Split(',');

            if (partes.Length > 2 || partes[^1].Length == 3)
                limpio = limpio.Replace(",", string.Empty);
            else
                limpio = limpio.Replace(',', '.');
        }
        else if (tienePunto)
        {
            var partes = limpio.Split('.');

            if (partes.Length > 2)
                limpio = string.Join(string.Empty, partes[..^1]) + "." + partes[^1];
        }

        return decimal.TryParse(
            limpio, NumberStyles.Number, Cultura, out valor);
    }

    public static bool TryParseEntero(string? texto, out int valor)
    {
        valor = 0;

        if (!TryParseDecimal(texto, out var numero))
            return false;

        if (numero != decimal.Truncate(numero) ||
            numero < int.MinValue || numero > int.MaxValue)
            return false;

        valor = (int)numero;
        return true;
    }

    private static void AgregarRegistro(
        List<RegistroCsv> registros, int linea, List<string> campos)
    {
        if (campos.All(string.IsNullOrWhiteSpace))
            return;

        registros.Add(new RegistroCsv(linea, campos.ToArray()));
    }
}

public sealed record RegistroCsv(int Linea, IReadOnlyList<string> Campos);
