using System.Globalization;
using System.Text;
using PosInventario.Models;
using PosInventario.Models.Enums;



namespace PosInventario.Service;

/// <summary>
/// Genera el contenido de texto del ticket para impresión térmica. El texto se
/// ajusta al ancho de papel (32 columnas para 58 mm y 48 para 80 mm) y no
/// depende de Avalonia, de modo que se puede probar sin interfaz ni impresora.
/// </summary>
public static class GeneradorTicketTexto
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public static string Generar(Venta venta, AnchoTicket ancho)
    {
        var columnas = ancho.Columnas();
        var lineas = new List<string>();

        AgregarCentrado(lineas, "SISTEMA DE INVENTARIO", columnas);
        AgregarCentrado(lineas, $"Ticket V-{venta.Id:000000}", columnas);

        if (venta.Estado == EstadoVenta.Anulada)
            AgregarCentrado(lineas, "*** ANULADA ***", columnas);

        lineas.Add(new string('-', columnas));
        EnvolverEtiqueta(lineas, "Fecha: ", venta.Fecha.ToString("dd/MM/yyyy HH:mm", Cultura), columnas);
        EnvolverEtiqueta(lineas, "Cajero: ", venta.Usuario?.Nombre ?? "—", columnas);

        if (venta.TieneCliente)
        {
            EnvolverEtiqueta(
                lineas, "Cliente: ", venta.ClienteTexto, columnas);
        }

        lineas.Add(new string('-', columnas));

        foreach (var detalle in venta.Detalles)
        {
            var nombre = detalle.Producto?.Nombre ?? "Producto";
            AgregarEnvuelto(lineas, nombre, columnas);

            var cantidad = detalle.Unidad.Formatear(detalle.Cantidad);
            var precio = detalle.PrecioUnitario.ToString("0.00", Cultura);
            var sufijo = detalle.Unidad.EsPorPeso() ? "/kg" : string.Empty;
            var unidad = detalle.Unidad.EsPorPeso()
                ? $" {detalle.Unidad.Abreviatura()}"
                : string.Empty;
            var izquierda =
                $"{cantidad}{unidad} x {precio}{sufijo}";
            var derecha = detalle.Subtotal.ToString("0.00", Cultura);
            AgregarDosColumnas(lineas, izquierda, derecha, columnas);
        }

        lineas.Add(new string('-', columnas));
        AgregarDosColumnas(lineas, "Subtotal", venta.Subtotal.ToString("0.00", Cultura), columnas);

        if (venta.Descuento > 0)
            AgregarDosColumnas(lineas, "Descuento", venta.Descuento.ToString("0.00", Cultura), columnas);

        AgregarDosColumnas(lineas, "TOTAL", venta.Total.ToString("0.00", Cultura), columnas);

        if (venta.TotalDevuelto > 0)
        {
            AgregarDosColumnas(lineas, "Devuelto", venta.TotalDevuelto.ToString("0.00", Cultura), columnas);
            AgregarDosColumnas(lineas, "Total neto", venta.TotalNeto.ToString("0.00", Cultura), columnas);
        }

        lineas.Add(new string('-', columnas));

        if (venta.EsCredito)
        {
            if (venta.Total > 0)
            {
                AgregarDosColumnas(
                    lineas, "Crédito", venta.Total.ToString("0.00", Cultura), columnas);
            }
        }
        else
        {
            var pagos = venta.Pagos.Count > 0
                ? venta.Pagos.ToList()
                : [new PagoVenta { Metodo = venta.MetodoPago, Monto = venta.Total }];

            if (pagos.Count > 0 && venta.Total > 0)
            {
                foreach (var pago in pagos)
                {
                    AgregarDosColumnas(
                        lineas,
                        pago.Metodo.Nombre(),
                        pago.Monto.ToString("0.00", Cultura),
                        columnas);
                }

                var efectivoAplicado = pagos
                    .Where(p => p.Metodo == MetodoPago.Efectivo)
                    .Sum(p => p.Monto);

                if (efectivoAplicado > 0)
                {
                    var recibido = efectivoAplicado + venta.Vuelto;

                    AgregarDosColumnas(
                        lineas, "Recibido", recibido.ToString("0.00", Cultura), columnas);
                    AgregarDosColumnas(
                        lineas, "Cambio", venta.Vuelto.ToString("0.00", Cultura), columnas);
                }
            }
        }

        lineas.Add(new string('-', columnas));
        AgregarCentrado(lineas, "¡Gracias por su compra!", columnas);

        return string.Join('\n', lineas) + '\n';
    }

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
