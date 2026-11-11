using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class CajaHistorialWindow : Window
{
    private bool _huboDevolucion;

    public CajaHistorialWindow() => InitializeComponent();

    private async void Devolver_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: VentaHistorialItem item } ||
            DataContext is not CajaHistorialViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var devolucion = viewModel.CrearDevolucionViewModel(item.Venta);

        if (devolucion is null)
            return;

        var registrada = await new DevolverVentaWindow { DataContext = devolucion }
            .ShowDialog<bool>(ventana);

        if (!registrada)
            return;

        _huboDevolucion = true;
        await viewModel.RecargarAsync();
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close(_huboDevolucion);
}
