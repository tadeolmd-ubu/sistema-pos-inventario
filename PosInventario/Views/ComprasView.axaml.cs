using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ComprasView : UserControl
{
    public ComprasView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Compras;
    }

    private async void Anular_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ComprasViewModel viewModel || viewModel.SelectedCompra is null)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Anular compra",
            $"¿Anular la compra C-{viewModel.SelectedCompra.Id:000000}? " +
            "Se revertirá el stock de sus productos.");

        if (await confirmacion.ShowDialog<bool>(ventana))
            await viewModel.AnularSeleccionadaAsync();
    }
}
