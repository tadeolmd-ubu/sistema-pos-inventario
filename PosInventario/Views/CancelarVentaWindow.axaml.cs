using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class CancelarVentaWindow : Window
{
    private bool _huboDevolucion;

    public CancelarVentaWindow() => InitializeComponent();

    private async void Devolver_Click(object? sender, RoutedEventArgs e)
        => await AbrirDevolucionAsync();

    private async void Ventas_DoubleTapped(
        object? sender, Avalonia.Input.TappedEventArgs e)
        => await AbrirDevolucionAsync();

    private async Task AbrirDevolucionAsync()
    {
        if (DataContext is not CancelarVentaViewModel viewModel ||
            !viewModel.PuedeDevolver ||
            TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var devolucion = viewModel.CrearDevolucionViewModel();

        if (devolucion is null)
            return;

        var registrada = await new DevolverVentaWindow { DataContext = devolucion }
            .ShowDialog<bool>(ventana);

        if (!registrada)
            return;

        _huboDevolucion = true;
        await viewModel.CargarAsync();
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e)
        => Close(_huboDevolucion);
}
