using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace PosInventario.Service;

/// <summary>
/// Impresión en Windows con la API del sistema. Fija el tamaño del papel para
/// que el texto de 32/48 columnas salga legible en rollos de 58 y 80 mm.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class ImpresionWindows
{
    public static Resultado Imprimir(string contenido, AnchoTicket ancho)
    {
        try
        {
            var lineas = contenido.Replace("\r\n", "\n").Split('\n');
            var indice = 0;

            using var documento = new PrintDocument
            {
                DocumentName = "Ticket de venta",
                PrintController = new StandardPrintController(),
                OriginAtMargins = true
            };

            if (!documento.PrinterSettings.IsValid)
            {
                RegistroApp.Escribir("No hay una impresora predeterminada válida.");

                return new Resultado(false, ImpresionPlataforma.MensajeError);
            }

            var anchoCentesimas =
                (int)Math.Round(ancho.Milimetros() / 25.4 * 100);
            var altoCentesimas = Math.Max(300, lineas.Length * 16 + 40);

            try
            {
                documento.DefaultPageSettings.PaperSize =
                    new PaperSize(ancho.Etiqueta(), anchoCentesimas, altoCentesimas);
            }
            catch (Exception ex)
            {
                // Si el driver no acepta el tamaño personalizado, se imprime
                // con el papel predeterminado en lugar de fallar.
                RegistroApp.Escribir(
                    "La impresora no aceptó el ancho de papel; se usará el predeterminado.",
                    ex);
            }

            documento.DefaultPageSettings.Margins = new Margins(8, 8, 8, 8);
            documento.DefaultPageSettings.Landscape = false;

            documento.PrintPage += (_, e) =>
            {
                var graphics = e.Graphics;

                if (graphics is null)
                {
                    e.HasMorePages = false;
                    return;
                }

                var anchoDisponible = Math.Max(
                    1f, e.MarginBounds.Width / 100f * graphics.DpiX);
                var altoDisponible = Math.Max(
                    1f, e.MarginBounds.Height / 100f * graphics.DpiY);

                var lineaMasLarga = lineas
                    .OrderByDescending(l => l.Length)
                    .FirstOrDefault() ?? string.Empty;

                using var fuenteBase = new Font(
                    "Consolas", 9f, FontStyle.Regular, GraphicsUnit.Point);

                var anchoMedido = graphics.MeasureString(
                    lineaMasLarga, fuenteBase).Width;

                var escala = anchoMedido > 0
                    ? Math.Min(1f, anchoDisponible / anchoMedido)
                    : 1f;

                var tamano = Math.Clamp(9f * escala, 6f, 10f);

                using var fuente = new Font(
                    "Consolas", tamano, FontStyle.Regular, GraphicsUnit.Point);

                var altoLinea = fuente.GetHeight(graphics);
                var y = 0f;
                var lineasEnPagina = 0;

                while (indice < lineas.Length)
                {
                    if (lineasEnPagina > 0 && y + altoLinea > altoDisponible)
                    {
                        e.HasMorePages = true;
                        break;
                    }

                    if (lineas[indice].Length > 0)
                    {
                        graphics.DrawString(
                            lineas[indice], fuente, Brushes.Black, 0f, y);
                    }

                    y += altoLinea;
                    indice++;
                    lineasEnPagina++;
                }

                if (indice >= lineas.Length)
                    e.HasMorePages = false;
            };

            documento.Print();

            return new Resultado(true, "Ticket enviado a la impresora.");
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo imprimir el ticket.", ex);

            return new Resultado(false, ImpresionPlataforma.MensajeError);
        }
    }
}
