using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.Models;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel? _viewModel;

    public LoginWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as LoginViewModel);
    }

    private void ConectarViewModel(LoginViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.AccesoConcedido -= OnAccesoConcedido;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.AccesoConcedido += OnAccesoConcedido;
    }

    private void OnAccesoConcedido(Usuario usuario) => Close(usuario);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(null);
}
