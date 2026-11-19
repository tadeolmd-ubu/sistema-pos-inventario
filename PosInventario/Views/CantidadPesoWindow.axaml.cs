using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class CantidadPesoWindow : Window
{
    private CantidadPesoViewModel? _viewModel;

    public CantidadPesoWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as CantidadPesoViewModel);

        Loaded += (_, _) =>
        {
            CantidadBox.Focus();
            CantidadBox.SelectAll();
        };
    }

    private void ConectarViewModel(CantidadPesoViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.Confirmado -= OnConfirmado;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.Confirmado += OnConfirmado;
    }

    private void OnConfirmado(decimal cantidad) => Close((decimal?)cantidad);

    private void Cerrar_Click(object? sender, RoutedEventArgs e)
        => Close((decimal?)null);
}
