using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class RegistrarMovimientoCajaWindow : Window
{
    private RegistrarMovimientoCajaViewModel? _viewModel;

    public RegistrarMovimientoCajaWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as RegistrarMovimientoCajaViewModel);
    }

    private void ConectarViewModel(RegistrarMovimientoCajaViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.MovimientoRegistrado -= OnMovimientoRegistrado;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.MovimientoRegistrado += OnMovimientoRegistrado;
    }

    private void OnMovimientoRegistrado() => Close(true);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
