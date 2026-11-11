using PosInventario.Models;
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
}
