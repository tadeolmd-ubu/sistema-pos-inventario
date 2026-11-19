using ClosedXML.Excel;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class ExportadorReporteExcelTests
{
    private static ReporteAdministracion CrearReporte() => new(
        new DateTime(2026, 11, 1),
        new DateTime(2026, 11, 12),
        AgrupacionReporte.Dia,
        2,
        1000m,
        100m,
        900m,
        400m,
        500m,
        [
            new ReportePeriodo(
                "01/11/2026", new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 2), 2, 900m, 100m, 500m, 1m)
        ],
        [new ReporteMetodo(MetodoPago.Efectivo, 900m, 2, 1m)],
        [new ReporteProducto(
            1, "SKU-1", "Producto", 5, 900m, 400m, 500m, 1m)]);

    [Fact]
    public void ElResumenIncluyeTotalVendidoYUtilidad()
    {
        using var memoria = new MemoryStream(
            ExportadorReporteExcel.Generar(CrearReporte()));
        using var libro = new XLWorkbook(memoria);

        var resumen = libro.Worksheet("Resumen");

        Assert.Equal("Total vendido (neto)", resumen.Cell("A9").GetString());
        Assert.Equal(900d, resumen.Cell("B9").GetDouble());
        Assert.Equal("Utilidad", resumen.Cell("A11").GetString());
        Assert.Equal(500d, resumen.Cell("B11").GetDouble());
    }

    [Fact]
    public void IncluyeLasHojasYLosTotalesPorPeriodo()
    {
        using var memoria = new MemoryStream(
            ExportadorReporteExcel.Generar(CrearReporte()));
        using var libro = new XLWorkbook(memoria);

        Assert.Equal(
            new[] { "Resumen", "Por período", "Métodos de pago", "Productos" },
            libro.Worksheets.Select(h => h.Name).ToArray());

        var periodos = libro.Worksheet("Por período");
        Assert.Equal("Total", periodos.Cell("A3").GetString());
        Assert.Equal(900d, periodos.Cell("C3").GetDouble());
        Assert.Equal(500d, periodos.Cell("E3").GetDouble());
    }

    [Fact]
    public void NombreArchivoIncluyeElRango()
    {
        Assert.Equal(
            "reporte_2026-11-01_2026-11-12.xlsx",
            ExportadorReporteExcel.NombreArchivo(CrearReporte()));
    }
}
