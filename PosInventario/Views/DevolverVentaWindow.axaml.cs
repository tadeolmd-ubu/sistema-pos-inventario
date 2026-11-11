using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class DevolverVentaWindow : Window
{
    private DevolverVentaViewModel? _viewModel;

    public DevolverVentaWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as DevolverVentaViewModel);
    }

    private void ConectarViewModel(DevolverVentaViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.DevolucionRegistrada -= OnDevolucionRegistrada;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.DevolucionRegistrada += OnDevolucionRegistrada;
    }

    private void OnDevolucionRegistrada() => Close(true);

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close(false);

    private async void Devolver_Click(object? sender, RoutedEventArgs e)
        => await ConfirmarAsync();

    private async Task ConfirmarAsync()
    {
        if (_viewModel is null || !_viewModel.PuedeConfirmar)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Devolver venta",
            $"¿Registrar la devolución de {_viewModel.UnidadesTexto} unidad(es) " +
            $"por {_viewModel.ReembolsoTexto}? Se regresa el stock al inventario.");

        if (await confirmacion.ShowDialog<bool>(ventana))
            await _viewModel.ConfirmarAsync();
    }
}
