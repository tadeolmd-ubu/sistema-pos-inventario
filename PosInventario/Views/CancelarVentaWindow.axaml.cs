using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class CancelarVentaWindow : Window
{
    private CancelarVentaViewModel? _viewModel;

    public CancelarVentaWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as CancelarVentaViewModel);
    }

    private void ConectarViewModel(CancelarVentaViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.VentaCancelada -= OnVentaCancelada;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.VentaCancelada += OnVentaCancelada;
    }

    private void OnVentaCancelada() => Close(true);

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close(false);

    private async void Cancelar_Click(object? sender, RoutedEventArgs e)
        => await ConfirmarYCancelarAsync();

    private async void Ventas_DoubleTapped(
        object? sender, Avalonia.Input.TappedEventArgs e)
        => await ConfirmarYCancelarAsync();

    private async Task ConfirmarYCancelarAsync()
    {
        if (_viewModel?.SelectedVenta is null || !_viewModel.PuedeCancelar)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Cancelar venta",
            $"¿Cancelar la venta V-{_viewModel.SelectedVenta.Id:000000}? " +
            "Se devolverá el stock y, si fue en efectivo, el dinero a la caja.");

        if (await confirmacion.ShowDialog<bool>(ventana))
            await _viewModel.CancelarSeleccionadaAsync();
    }
}
