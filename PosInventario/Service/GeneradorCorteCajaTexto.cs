using System.Globalization;
using System.Text;
using PosInventario.Models;

namespace PosInventario.Service;

/// <summary>
/// Genera el texto del corte de caja para impresión térmica. Incluye las ventas,
/// el desglose por método de pago y la comparación entre el efectivo esperado y
/// el contado. Igual que el ticket, se ajusta al ancho de papel (32 columnas
/// para 58 mm y 48 para 80 mm) y no depende de la interfaz ni de la impresora.
/// </summary>
public static class GeneradorCorteCajaTexto
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public static string Generar(Caja caja, ResumenPagosCaja resumen, AnchoTicket ancho)
    {
        var columnas = ancho.Columnas();
        var lineas = new List<string>();

        AgregarCentrado(lineas, "SISTEMA DE INVENTARIO", columnas);
        AgregarCentrado(lineas, $"Corte de caja #{caja.Id}", columnas);
        lineas.Add(new string('-', columnas));

        EnvolverEtiqueta(
            lineas, "Apertura: ",
            caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura), columnas);
        EnvolverEtiqueta(
            lineas, "Cierre: ",
            caja.FechaCierre?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—",
            columnas);
        EnvolverEtiqueta(
            lineas, "Abrió: ", caja.UsuarioApertura?.Nombre ?? "—", columnas);

        lineas.Add(new string('-', columnas));
        AgregarCentrado(lineas, "VENTAS", columnas);
        AgregarDosColumnas(
            lineas, "Ventas", resumen.CantidadVentas.ToString(Cultura), columnas);
        AgregarDosColumnas(
            lineas, "Total vendido", Dinero(resumen.Total), columnas);
        AgregarDosColumnas(
            lineas, "Efectivo", Dinero(resumen.Efectivo), columnas);
        AgregarDosColumnas(
            lineas, "Tarjeta", Dinero(resumen.Tarjeta), columnas);
        AgregarDosColumnas(
            lineas, "Transferencia", Dinero(resumen.Transferencia), columnas);

        lineas.Add(new string('-', columnas));
        AgregarCentrado(lineas, "EFECTIVO", columnas);
        AgregarDosColumnas(
            lineas, "Monto inicial", Dinero(caja.MontoInicial), columnas);
        AgregarDosColumnas(
            lineas, "Esperado", Dinero(caja.MontoEsperado ?? 0m), columnas);
        AgregarDosColumnas(
            lineas, "Contado", Dinero(caja.MontoFinal ?? 0m), columnas);

        var diferencia = caja.Diferencia
            ?? (caja.MontoFinal ?? 0m) - (caja.MontoEsperado ?? 0m);

        AgregarDosColumnas(
            lineas, "Diferencia", DiferenciaConSigno(diferencia), columnas);

        if (diferencia < 0)
            AgregarCentrado(lineas, $"FALTANTE: {Dinero(Math.Abs(diferencia))}", columnas);
        else if (diferencia > 0)
            AgregarCentrado(lineas, $"SOBRANTE: {Dinero(diferencia)}", columnas);
        else
            AgregarCentrado(lineas, "SIN DIFERENCIA", columnas);

        if (!string.IsNullOrWhiteSpace(caja.Observaciones))
        {
            lineas.Add(new string('-', columnas));
            AgregarEnvuelto(lineas, "Observaciones:", columnas);
            AgregarEnvuelto(lineas, caja.Observaciones.Trim(), columnas);
        }

        lineas.Add(new string('-', columnas));
        lineas.Add(string.Empty);
        AgregarCentrado(lineas, "Firma: ________________", columnas);

        return string.Join('\n', lineas) + '\n';
    }

    private static string Dinero(decimal monto)
        => monto.ToString("0.00", Cultura);

    private static string DiferenciaConSigno(decimal monto)
        => monto.ToString("+0.00;-0.00;0.00", Cultura);

    private static void AgregarCentrado(List<string> lineas, string texto, int columnas)
    {
        foreach (var linea in Envolver(texto, columnas))
            lineas.Add(Centrar(linea, columnas));
    }

    private static void AgregarEnvuelto(List<string> lineas, string texto, int columnas)
    {
        foreach (var linea in Envolver(texto, columnas))
            lineas.Add(linea);
    }

    private static void EnvolverEtiqueta(
        List<string> lineas, string etiqueta, string valor, int columnas)
    {
        var disponible = Math.Max(1, columnas - etiqueta.Length);
        var partes = Envolver(valor, disponible);

        lineas.Add(etiqueta + partes[0]);

        foreach (var parte in partes.Skip(1))
            lineas.Add(new string(' ', etiqueta.Length) + parte);
    }

    private static void AgregarDosColumnas(
        List<string> lineas, string izquierda, string derecha, int columnas)
    {
        izquierda = izquierda.Trim();
        derecha = derecha.Trim();

        var espacios = columnas - izquierda.Length - derecha.Length;

        if (espacios >= 1)
        {
            lineas.Add(izquierda + new string(' ', espacios) + derecha);
            return;
        }

        foreach (var linea in Envolver(izquierda, columnas))
            lineas.Add(linea);

        lineas.Add(AlinearDerecha(derecha, columnas));
    }

    private static string Centrar(string texto, int columnas)
    {
        if (texto.Length >= columnas)
            return Ajustar(texto, columnas);

        var izquierda = (columnas - texto.Length) / 2;
        var derecha = columnas - texto.Length - izquierda;

        return new string(' ', izquierda) + texto + new string(' ', derecha);
    }

    private static string AlinearDerecha(string texto, int columnas)
    {
        if (texto.Length >= columnas)
            return Ajustar(texto, columnas);

        return new string(' ', columnas - texto.Length) + texto;
    }

    private static string Ajustar(string texto, int columnas)
        => texto.Length <= columnas ? texto : texto[..columnas];

    /// <summary>
    /// Parte un texto en líneas que no superan el ancho indicado. Respeta las
    /// palabras y corta las que por sí solas no caben.
    /// </summary>
    private static IReadOnlyList<string> Envolver(string texto, int columnas)
    {
        columnas = Math.Max(1, columnas);

        var resultado = new List<string>();
        var parrafos = (texto ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        foreach (var parrafo in parrafos)
        {
            var actual = new StringBuilder();

            foreach (var palabra in parrafo.Split(
                ' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (palabra.Length > columnas)
                {
                    if (actual.Length > 0)
                    {
                        resultado.Add(actual.ToString());
                        actual.Clear();
                    }

                    for (var i = 0; i < palabra.Length; i += columnas)
                    {
                        resultado.Add(palabra.Substring(
                            i, Math.Min(columnas, palabra.Length - i)));
                    }

                    continue;
                }

                if (actual.Length == 0)
                {
                    actual.Append(palabra);
                }
                else if (actual.Length + 1 + palabra.Length <= columnas)
                {
                    actual.Append(' ').Append(palabra);
                }
                else
                {
                    resultado.Add(actual.ToString());
                    actual.Clear();
                    actual.Append(palabra);
                }
            }

            if (actual.Length > 0)
                resultado.Add(actual.ToString());
        }

        if (resultado.Count == 0)
            resultado.Add(string.Empty);

        return resultado;
    }
}
