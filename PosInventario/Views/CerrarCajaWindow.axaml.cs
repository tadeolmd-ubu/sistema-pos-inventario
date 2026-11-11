using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class CerrarCajaWindow : Window
{
    private CerrarCajaViewModel? _viewModel;

    public CerrarCajaWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as CerrarCajaViewModel);
    }

    private void ConectarViewModel(CerrarCajaViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.CajaCerrada -= OnCajaCerrada;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.CajaCerrada += OnCajaCerrada;
    }

    private void OnCajaCerrada() => Close(true);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
