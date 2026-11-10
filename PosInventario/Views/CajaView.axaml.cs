using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.Models;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class CajaView : UserControl
{
    private CajaViewModel? _viewModel;

    public CajaView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as CajaViewModel);

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Caja;
    }

    private void ConectarViewModel(CajaViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.SeleccionarProductoSolicitado -= OnSeleccionarProducto;
            _viewModel.EliminarLineaSolicitada -= OnEliminarLinea;
            _viewModel.TicketSolicitado -= OnTicket;
        }

        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.SeleccionarProductoSolicitado += OnSeleccionarProducto;
            _viewModel.EliminarLineaSolicitada += OnEliminarLinea;
            _viewModel.TicketSolicitado += OnTicket;
        }
    }

    private async void OnSeleccionarProducto(SeleccionarProductoViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await viewModel.CargarAsync();

        var producto = await new SeleccionarProductoWindow { DataContext = viewModel }
            .ShowDialog<Producto?>(ventana);

        if (producto is not null)
            _viewModel?.AgregarProducto(producto);
    }

    private async void OnEliminarLinea(LineaVentaItem linea)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Eliminar producto",
            $"¿Quitar {linea.Nombre} de la venta?");

        if (await confirmacion.ShowDialog<bool>(ventana))
            _viewModel?.EliminarLinea(linea);
    }

    private async void OnTicket(Venta venta)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await new TicketWindow
        {
            DataContext = new TicketViewModel(venta)
        }.ShowDialog(ventana);
    }

    private async void CerrarCaja_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CajaViewModel viewModel || viewModel.CajaAbierta is null)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Cerrar caja",
            $"¿Cerrar la caja abierta por {viewModel.UsuarioAperturaTexto}? " +
            "Se guardará el monto esperado y la diferencia con el monto contado.");

        if (await confirmacion.ShowDialog<bool>(ventana))
            await viewModel.CerrarCajaAsync();
    }
}
