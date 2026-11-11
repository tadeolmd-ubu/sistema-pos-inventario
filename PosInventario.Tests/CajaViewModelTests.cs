using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class CajaViewModelTests
{
    private static Producto CrearProducto(int id = 1, decimal precio = 100m, int stock = 10)
        => new()
        {
            Id = id,
            Sku = $"SKU-{id}",
            Nombre = $"Producto {id}",
            PrecioVenta = precio,
            Stock = stock
        };

    [Fact]
    public void DescuentoSeRestaDelTotal()
    {
        var vm = new CajaViewModel();
        var producto = CrearProducto();

        vm.AgregarProducto(producto);
        vm.AgregarProducto(producto);
        vm.DescuentoTexto = "50";

        Assert.Equal(200m, vm.SubtotalVenta);
        Assert.Equal(50m, vm.Descuento);
        Assert.Equal(150m, vm.TotalVenta);
        Assert.Equal("150.00", vm.TotalVentaTexto);
    }

    [Fact]
    public void DescuentoMayorAlSubtotalDejaTotalEnCero()
    {
        var vm = new CajaViewModel();

        vm.AgregarProducto(CrearProducto());
        vm.DescuentoTexto = "500";

        Assert.Equal(100m, vm.SubtotalVenta);
        Assert.Equal(0m, vm.TotalVenta);
    }

    [Fact]
    public void DescuentoInvalidoONegativoSeIgnora()
    {
        var vm = new CajaViewModel();

        vm.AgregarProducto(CrearProducto());

        vm.DescuentoTexto = "-10";
        Assert.Equal(0m, vm.Descuento);
        Assert.Equal(100m, vm.TotalVenta);

        vm.DescuentoTexto = "abc";
        Assert.Equal(0m, vm.Descuento);
        Assert.Equal(100m, vm.TotalVenta);
    }

    [Fact]
    public void LimpiarVentaBorraDescuento()
    {
        var vm = new CajaViewModel();

        vm.AgregarProducto(CrearProducto());
        vm.DescuentoTexto = "25";

        vm.LimpiarVentaCommand.Execute(null);

        Assert.Equal(string.Empty, vm.DescuentoTexto);
        Assert.Equal(0m, vm.TotalVenta);
    }

    [Fact]
    public void PagoMixtoCalculaFaltanteYCambio()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));

        Assert.Equal(100m, vm.TotalPagado);

        vm.PagosItems[0].MontoTexto = "40";
        vm.PagosItems[0].RecibidoTexto = "50";
        vm.AgregarPagoCommand.Execute(null);

        Assert.Equal(2, vm.PagosItems.Count);
        Assert.Equal(MetodoPago.Tarjeta, vm.PagosItems[1].Metodo);
        Assert.Equal(60m, vm.PagosItems[1].Monto);
        Assert.Equal(100m, vm.TotalPagado);
        Assert.Equal(0m, vm.Faltante);
        Assert.Equal(10m, vm.Cambio);
        Assert.True(vm.PagosValidos);
    }

    [Fact]
    public void PagoQueNoCubreElTotalNoEsValido()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));

        vm.PagosItems[0].MontoTexto = "40";

        Assert.False(vm.PagosValidos);
        Assert.Equal(60m, vm.Faltante);
        Assert.True(vm.HayFaltante);
    }

    [Fact]
    public void EfectivoRecibidoMenorAlAplicadoNoEsValido()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));

        vm.PagosItems[0].RecibidoTexto = "50";

        Assert.False(vm.PagosValidos);
        Assert.Equal(0m, vm.Cambio);
    }

    [Fact]
    public void NoPermiteRepetirMetodoDePago()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));
        vm.AgregarPagoCommand.Execute(null);

        vm.PagosItems[1].Metodo = MetodoPago.Efectivo;

        Assert.False(vm.PagosValidos);
    }
}
