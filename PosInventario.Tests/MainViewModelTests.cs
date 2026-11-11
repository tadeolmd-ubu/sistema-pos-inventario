using PosInventario.Service;
using PosInventario.ViewModels;

namespace PosInventario.Tests;

public class MainViewModelTests
{
    [Fact]
    public void EstablecerComandosReemplazaElPanel()
    {
        var vm = Crear();

        vm.EstablecerComandos("Caja · Búsqueda", [
            new AtajoItem("F2", "Buscar"),
            new AtajoItem("Enter", "Agregar")
        ]);

        Assert.Equal("Caja · Búsqueda", vm.ComandosTitulo);
        Assert.Collection(
            vm.ComandosItems,
            item => Assert.Equal("F2", item.Tecla),
            item => Assert.Equal("Enter", item.Tecla));

        vm.EstablecerComandos("Caja · Pago", [
            new AtajoItem("F4", "Cobrar")
        ]);

        Assert.Equal("Caja · Pago", vm.ComandosTitulo);
        Assert.Single(vm.ComandosItems);
        Assert.Equal("F4", vm.ComandosItems[0].Tecla);
    }

    [Fact]
    public async Task EntrarACajaCargaSusComandosGenerales()
    {
        var vm = Crear();

        await vm.IrACajaCommand.ExecuteAsync(null);

        Assert.Equal("Caja", vm.ComandosTitulo);
        Assert.Contains(vm.ComandosItems, i => i.Tecla == "F2");
        Assert.Contains(vm.ComandosItems, i => i.Tecla == "F4");
    }

    private static MainViewModel Crear()
        => new(
            new InicioViewModel(),
            new CajaViewModel(),
            new ProductosViewModel(),
            new ProveedoresViewModel(),
            new InventarioViewModel(),
            new ComprasViewModel(),
            new VentasViewModel(),
            new UsuariosViewModel(),
            new ConfiguracionViewModel(),
            new VersionService(),
            new SesionActual());
}
