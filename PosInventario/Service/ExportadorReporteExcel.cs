using System.Globalization;
using ClosedXML.Excel;

namespace PosInventario.Service;

/// <summary>
/// Genera el libro .xlsx del reporte de administración. Incluye una hoja
/// Resumen con el total vendido y la utilidad, y una hoja por cada tabla.
/// </summary>
public static class ExportadorReporteExcel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private const string FormatoMonto = "#,##0.00";
    private const string FormatoCantidad = "0.###";
    private const string FormatoPorcentaje = "0.0%";
    private const string ColorEncabezado = "#1F4E79";
    private const string ColorTotales = "#E8F1FA";

    public static string NombreArchivo(ReporteAdministracion reporte)
        => $"reporte_{reporte.Desde.ToString("yyyy-MM-dd", Cultura)}_" +
           $"{reporte.Hasta.ToString("yyyy-MM-dd", Cultura)}.xlsx";

    public static byte[] Generar(ReporteAdministracion reporte)
    {
        using var libro = new XLWorkbook();

        AgregarResumen(libro, reporte);
        AgregarPeriodos(libro, reporte);
        AgregarMetodos(libro, reporte);
        AgregarProductos(libro, reporte);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void AgregarResumen(
        XLWorkbook libro, ReporteAdministracion reporte)
    {
        var hoja = libro.Worksheets.Add("Resumen");

        hoja.Cell("A1").Value = "Reporte de ventas";
        hoja.Cell("A1").Style.Font.Bold = true;
        hoja.Cell("A1").Style.Font.FontSize = 16;

        hoja.Cell("A3").Value = "Período";
        hoja.Cell("B3").Value = Periodo(reporte);
        hoja.Cell("A4").Value = "Agrupación";
        hoja.Cell("B4").Value = NombreAgrupacion(reporte.Agrupacion);

        hoja.Cell("A6").Value = "Concepto";
        hoja.Cell("B6").Value = "Total";
        Encabezado(hoja.Range("A6:B6"));

        var fila = 7;
        fila = FilaResumen(hoja, fila, "Ventas brutas",
            (double)reporte.VentasBrutas, FormatoMonto);
        fila = FilaResumen(hoja, fila, "Devoluciones",
            (double)reporte.Devoluciones, FormatoMonto);
        fila = FilaResumen(hoja, fila, "Total vendido (neto)",
            (double)reporte.VentasNetas, FormatoMonto, destacar: true);
        fila = FilaResumen(hoja, fila, "Costo de lo vendido",
            (double)reporte.Costo, FormatoMonto);
        fila = FilaResumen(hoja, fila, "Utilidad",
            (double)reporte.Utilidad, FormatoMonto, destacar: true);
        fila = FilaResumen(hoja, fila, "Margen",
            (double)reporte.Margen, FormatoPorcentaje);
        fila = FilaResumen(hoja, fila, "Cantidad de ventas",
            reporte.CantidadVentas, "0");
        FilaResumen(hoja, fila, "Ticket promedio",
            (double)reporte.TicketPromedio, FormatoMonto);

        hoja.Column(1).Width = 28;
        hoja.Column(2).Width = 20;
    }

    private static int FilaResumen(
        IXLWorksheet hoja,
        int fila,
        string concepto,
        double valor,
        string formato,
        bool destacar = false)
    {
        hoja.Cell(fila, 1).Value = concepto;
        hoja.Cell(fila, 2).Value = valor;
        hoja.Cell(fila, 2).Style.NumberFormat.Format = formato;

        if (destacar)
        {
            hoja.Range(fila, 1, fila, 2).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 2).Style.Fill.BackgroundColor =
                XLColor.FromHtml(ColorTotales);
        }

        return fila + 1;
    }

    private static void AgregarPeriodos(
        XLWorkbook libro, ReporteAdministracion reporte)
    {
        var hoja = libro.Worksheets.Add("Por período");
        var encabezados = new[]
        {
            "Período", "Ventas", "Neto", "Devuelto", "Utilidad"
        };

        for (var c = 0; c < encabezados.Length; c++)
            hoja.Cell(1, c + 1).Value = encabezados[c];

        Encabezado(hoja.Range(1, 1, 1, encabezados.Length));

        var fila = 2;
        foreach (var periodo in reporte.Periodos)
        {
            hoja.Cell(fila, 1).Value = periodo.Etiqueta;
            hoja.Cell(fila, 2).Value = (double)periodo.CantidadVentas;
            hoja.Cell(fila, 3).Value = (double)periodo.VentasNetas;
            hoja.Cell(fila, 4).Value = (double)periodo.Devoluciones;
            hoja.Cell(fila, 5).Value = (double)periodo.Utilidad;
            hoja.Range(fila, 3, fila, 5).Style.NumberFormat.Format =
                FormatoMonto;
            fila++;
        }

        hoja.Cell(fila, 1).Value = "Total";
        hoja.Cell(fila, 2).Value = (double)reporte.CantidadVentas;
        hoja.Cell(fila, 3).Value = (double)reporte.VentasNetas;
        hoja.Cell(fila, 4).Value = (double)reporte.Devoluciones;
        hoja.Cell(fila, 5).Value = (double)reporte.Utilidad;
        hoja.Range(fila, 1, fila, 5).Style.Font.Bold = true;
        hoja.Range(fila, 1, fila, 5).Style.Fill.BackgroundColor =
            XLColor.FromHtml(ColorTotales);
        hoja.Range(fila, 3, fila, 5).Style.NumberFormat.Format = FormatoMonto;

        hoja.SheetView.FreezeRows(1);
        hoja.Column(1).Width = 26;
        hoja.Column(2).Width = 11;
        hoja.Column(3).Width = 14;
        hoja.Column(4).Width = 14;
        hoja.Column(5).Width = 14;
    }

    private static void AgregarMetodos(
        XLWorkbook libro, ReporteAdministracion reporte)
    {
        var hoja = libro.Worksheets.Add("Métodos de pago");
        var encabezados = new[] { "Método", "Monto", "Ventas", "Porcentaje" };

        for (var c = 0; c < encabezados.Length; c++)
            hoja.Cell(1, c + 1).Value = encabezados[c];

        Encabezado(hoja.Range(1, 1, 1, encabezados.Length));

        var fila = 2;
        foreach (var metodo in reporte.Metodos)
        {
            hoja.Cell(fila, 1).Value = metodo.Nombre;
            hoja.Cell(fila, 2).Value = (double)metodo.Monto;
            hoja.Cell(fila, 2).Style.NumberFormat.Format = FormatoMonto;
            hoja.Cell(fila, 3).Value = (double)metodo.CantidadVentas;
            hoja.Cell(fila, 4).Value = (double)metodo.Proporcion;
            hoja.Cell(fila, 4).Style.NumberFormat.Format = FormatoPorcentaje;
            fila++;
        }

        var totalPorcentaje = Math.Clamp(
            reporte.Metodos.Sum(m => m.Proporcion), 0m, 1m);

        hoja.Cell(fila, 1).Value = "Total";
        hoja.Cell(fila, 2).Value = (double)reporte.VentasNetas;
        hoja.Cell(fila, 2).Style.NumberFormat.Format = FormatoMonto;
        hoja.Cell(fila, 4).Value = (double)totalPorcentaje;
        hoja.Cell(fila, 4).Style.NumberFormat.Format = FormatoPorcentaje;
        hoja.Range(fila, 1, fila, 4).Style.Font.Bold = true;
        hoja.Range(fila, 1, fila, 4).Style.Fill.BackgroundColor =
            XLColor.FromHtml(ColorTotales);

        hoja.SheetView.FreezeRows(1);
        hoja.Column(1).Width = 20;
        hoja.Column(2).Width = 14;
        hoja.Column(3).Width = 11;
        hoja.Column(4).Width = 13;
    }

    private static void AgregarProductos(
        XLWorkbook libro, ReporteAdministracion reporte)
    {
        var hoja = libro.Worksheets.Add("Productos");
        var encabezados = new[]
        {
            "SKU", "Producto", "Unidad", "Cantidad",
            "Ventas", "Costo", "Utilidad"
        };

        for (var c = 0; c < encabezados.Length; c++)
            hoja.Cell(1, c + 1).Value = encabezados[c];

        Encabezado(hoja.Range(1, 1, 1, encabezados.Length));

        var fila = 2;
        foreach (var producto in reporte.Productos)
        {
            hoja.Cell(fila, 1).Value = producto.Sku;
            hoja.Cell(fila, 2).Value = producto.Nombre;
            hoja.Cell(fila, 3).Value = producto.UnidadTexto;
            hoja.Cell(fila, 4).Value = (double)producto.Cantidad;
            hoja.Cell(fila, 4).Style.NumberFormat.Format = FormatoCantidad;
            hoja.Cell(fila, 5).Value = (double)producto.Ventas;
            hoja.Cell(fila, 6).Value = (double)producto.Costo;
            hoja.Cell(fila, 7).Value = (double)producto.Utilidad;
            hoja.Range(fila, 5, fila, 7).Style.NumberFormat.Format =
                FormatoMonto;
            fila++;
        }

        hoja.SheetView.FreezeRows(1);
        hoja.Column(1).Width = 16;
        hoja.Column(2).Width = 40;
        hoja.Column(3).Width = 10;
        hoja.Column(4).Width = 12;
        hoja.Column(5).Width = 14;
        hoja.Column(6).Width = 14;
        hoja.Column(7).Width = 14;
    }

    private static void Encabezado(IXLRange rango)
    {
        rango.Style.Font.Bold = true;
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml(ColorEncabezado);
        rango.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;
    }

    private static string Periodo(ReporteAdministracion reporte)
        => $"{reporte.Desde.ToString("dd/MM/yyyy", Cultura)} – " +
           reporte.Hasta.ToString("dd/MM/yyyy", Cultura);

    private static string NombreAgrupacion(AgrupacionReporte agrupacion)
        => agrupacion switch
        {
            AgrupacionReporte.Semana => "Semana",
            AgrupacionReporte.Mes => "Mes",
            _ => "Día"
        };
}
