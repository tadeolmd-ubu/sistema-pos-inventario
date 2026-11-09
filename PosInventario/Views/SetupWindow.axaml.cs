using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel? _viewModel;

    public SetupWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as SetupViewModel);
    }

    private void ConectarViewModel(SetupViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.AdministradorCreado -= OnAdministradorCreado;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.AdministradorCreado += OnAdministradorCreado;
    }

    private void OnAdministradorCreado() => Close(true);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
