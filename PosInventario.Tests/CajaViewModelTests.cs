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
    public void PagoEfectivoUnicoCalculaFaltanteYCambio()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));

        Assert.False(vm.PagosValidos);

        vm.MontoEfectivoTexto = "120";

        Assert.True(vm.PagosValidos);
        Assert.Equal(100m, vm.TotalPagado);
        Assert.Equal(120m, vm.TotalRecibido);
        Assert.Equal(0m, vm.Faltante);
        Assert.Equal(20m, vm.Cambio);
        Assert.True(vm.HayCambio);
    }

    [Fact]
    public void PagoMixtoSumaLosTresMetodosYCalculaCambio()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));
        vm.MetodoPagoSeleccionado = MetodoPago.Mixto;
        vm.MontoEfectivoTexto = "50";
        vm.MontoTarjetaTexto = "60";

        Assert.True(vm.PagosValidos);
        Assert.Equal(110m, vm.TotalRecibido);
        Assert.Equal(100m, vm.TotalPagado);
        Assert.Equal(0m, vm.Faltante);
        Assert.Equal(10m, vm.Cambio);
    }

    [Fact]
    public void PagoMixtoNoEsValidoSiLaSumaNoCubreElTotal()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));
        vm.MetodoPagoSeleccionado = MetodoPago.Mixto;
        vm.MontoEfectivoTexto = "30";
        vm.MontoTarjetaTexto = "60";

        Assert.False(vm.PagosValidos);
        Assert.Equal(90m, vm.TotalRecibido);
        Assert.Equal(10m, vm.Faltante);
        Assert.True(vm.HayFaltante);
    }

    [Fact]
    public void PagosElectronicosNoPuedenSuperarElTotal()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));
        vm.MetodoPagoSeleccionado = MetodoPago.Mixto;
        vm.MontoTarjetaTexto = "60";
        vm.MontoTransferenciaTexto = "50";

        Assert.False(vm.PagosValidos);
        Assert.Equal(110m, vm.TotalRecibido);
    }

    [Fact]
    public void PagoConTarjetaNoRequiereCapturarMonto()
    {
        var vm = new CajaViewModel();
        vm.AgregarProducto(CrearProducto(precio: 100m));
        vm.MetodoPagoSeleccionado = MetodoPago.Tarjeta;

        Assert.True(vm.PagosValidos);
        Assert.Equal(100m, vm.TotalPagado);
        Assert.Equal(100m, vm.TotalRecibido);
        Assert.Equal(0m, vm.Cambio);
    }
}
