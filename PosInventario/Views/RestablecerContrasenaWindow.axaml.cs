using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class RestablecerContrasenaWindow : Window
{
    private RestablecerContrasenaViewModel? _viewModel;

    public RestablecerContrasenaWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as RestablecerContrasenaViewModel);
    }

    private void ConectarViewModel(RestablecerContrasenaViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.ContrasenaRestablecida -= OnContrasenaRestablecida;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.ContrasenaRestablecida += OnContrasenaRestablecida;
    }

    private void OnContrasenaRestablecida() => Close(true);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
