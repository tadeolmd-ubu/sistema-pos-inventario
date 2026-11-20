using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class ExportacionProductosTests
{
    [Fact]
    public void ElArchivoExportadoUsaLaPlantillaDeImportacion()
    {
        var productos = new[]
        {
            new Producto
            {
                Sku = "PZA-001",
                Nombre = "Refresco 600 ml",
                Descripcion = "Botella, 600 ml",
                UnidadMedida = UnidadMedida.Pieza,
                PrecioVenta = 18m,
                Costo = 12m,
                StockMinimo = 5m,
                Stock = 10m
            },
            new Producto
            {
                Sku = "KG-001",
                Nombre = "Carne molida",
                UnidadMedida = UnidadMedida.Kilogramo,
                PrecioVenta = 120m,
                Costo = 80m,
                StockMinimo = 1.5m,
                Stock = 2.5m
            }
        };

        var csv = ExportadorCsv.Productos(productos);
        var registros = ImportadorCsv.Leer(csv);

        Assert.Equal(3, registros.Count);

        Assert.Equal(
            ImportadorCsv.EncabezadoPlantilla.Split(','),
            registros[0].Campos);

        Assert.Equal(12, registros[1].Campos.Count);
        Assert.Equal("PZA-001", registros[1].Campos[0]);
        Assert.Equal("Refresco 600 ml", registros[1].Campos[1]);
        Assert.Equal("pieza", registros[1].Campos[2]);
        Assert.Equal("Botella, 600 ml", registros[1].Campos[3]);
        Assert.Equal("18.00", registros[1].Campos[4]);
        Assert.Equal("12.00", registros[1].Campos[5]);

        Assert.True(ImportadorCsv.TryParseDecimal(registros[1].Campos[6], out var minimo));
        Assert.Equal(5m, minimo);

        Assert.True(ImportadorCsv.TryParseDecimal(registros[1].Campos[7], out var stock));
        Assert.Equal(10m, stock);

        Assert.Equal("KG-001", registros[2].Campos[0]);
        Assert.Equal("kg", registros[2].Campos[2]);

        Assert.True(ImportadorCsv.TryParseDecimal(registros[2].Campos[6], out var minimoKg));
        Assert.Equal(1.5m, minimoKg);

        Assert.True(ImportadorCsv.TryParseDecimal(registros[2].Campos[7], out var stockKg));
        Assert.Equal(2.5m, stockKg);
    }
}
