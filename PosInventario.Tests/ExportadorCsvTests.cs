using System.Text;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.Tests;

public class ExportadorCsvTests
{
    [Fact]
    public void VentasIncluyeLosDatosQueMuestraElHistorial()
    {
        var venta = new Venta
        {
            Id = 32,
            Usuario = new Usuario { Nombre = "Laura Gómez" },
            Fecha = new DateTime(2026, 11, 12, 14, 30, 0),
            Subtotal = 900m,
            Descuento = 50m,
            Total = 850m,
            TotalDevuelto = 100m,
            MetodoPago = MetodoPago.Efectivo,
            Estado = EstadoVenta.Completada
        };

        var lineas = ExportadorCsv.Ventas([venta])
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(
            "Ticket,Fecha,Cajero,Método,Estado,Subtotal,Descuento,Total,Devuelto,Neto",
            lineas[0]);
        Assert.Equal(
            "V-000032,12/11/2026 14:30,Laura Gómez,Efectivo,Completada," +
            "900.00,50.00,850.00,100.00,750.00",
            lineas[1]);
    }

    [Fact]
    public void InventarioEscapaComasYComillas()
    {
        var movimiento = new MovimientoInventario
        {
            Fecha = new DateTime(2026, 11, 12, 9, 5, 0),
            Producto = new Producto
            {
                Nombre = "Producto \"especial\", grande",
                Sku = "ABC-001"
            },
            Usuario = new Usuario { Nombre = "Administrador" },
            Tipo = TipoMovimiento.Entrada,
            Cantidad = 5,
            StockAnterior = 2,
            StockResultante = 7,
            Referencia = "Carga, inicial"
        };

        var lineas = ExportadorCsv.Inventario([movimiento])
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(
            "Fecha,Producto,SKU,Tipo,Cantidad,Stock anterior," +
            "Stock resultante,Referencia,Usuario",
            lineas[0]);
        Assert.Equal(
            "12/11/2026 09:05,\"Producto \"\"especial\"\", grande\",ABC-001," +
            "Entrada,5,2,7,\"Carga, inicial\",Administrador",
            lineas[1]);
    }

    [Fact]
    public void ComprasResumeLosArticulosDeCadaCompra()
    {
        var compra = new Compra
        {
            Id = 7,
            Proveedor = new Proveedor { Nombre = "Distribuidora, Central" },
            Usuario = new Usuario { Nombre = "Administrador" },
            Fecha = new DateTime(2026, 11, 12, 9, 15, 0),
            NumeroComprobante = "FAC-001",
            Estado = EstadoCompra.Completada,
            Total = 1800m
        };

        compra.Detalles.Add(new DetalleCompra { Cantidad = 2 });
        compra.Detalles.Add(new DetalleCompra { Cantidad = 3 });

        var lineas = ExportadorCsv.Compras([compra])
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(
            "Compra,Fecha,Proveedor,Comprobante,Estado,Artículos,Total,Usuario",
            lineas[0]);
        Assert.Equal(
            "C-000007,12/11/2026 09:15,\"Distribuidora, Central\"," +
            "FAC-001,Completada,5,1800.00,Administrador",
            lineas[1]);
    }

    [Fact]
    public void ListaVaciaConservaElEncabezado()
    {
        Assert.Equal(
            "Fecha,Producto,SKU,Tipo,Cantidad,Stock anterior," +
            "Stock resultante,Referencia,Usuario\r\n",
            ExportadorCsv.Inventario([]));
    }

    [Fact]
    public void TextoQuePareceFormulaSeNeutraliza()
    {
        var venta = new Venta
        {
            Id = 1,
            Usuario = new Usuario { Nombre = "=2+2" },
            Fecha = new DateTime(2026, 11, 12, 10, 0, 0),
            Estado = EstadoVenta.Completada
        };

        var csv = ExportadorCsv.Ventas([venta]);

        Assert.Contains("'=2+2", csv);
    }

    [Fact]
    public async Task EscrituraUsaUtf8ConBom()
    {
        using var memoria = new MemoryStream();

        await ExportadorCsv.EscribirAsync(memoria, "Método,Referencia\r\n");

        var bytes = memoria.ToArray();

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Equal(
            "Método,Referencia\r\n",
            Encoding.UTF8.GetString(bytes[3..]));
    }

    [Fact]
    public void NombreArchivoIncluyeElRango()
    {
        Assert.Equal(
            "ventas_2026-11-01_2026-11-12.csv",
            ExportadorCsv.NombreArchivo(
                "ventas", new DateTime(2026, 11, 1), new DateTime(2026, 11, 12)));
    }

    [Fact]
    public void NombreArchivoNumeraLasPaginasSiguientes()
    {
        Assert.Equal(
            "compras_2026-11-01_2026-11-12_pagina3.csv",
            ExportadorCsv.NombreArchivo(
                "compras", new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 12), pagina: 3));
    }
}
