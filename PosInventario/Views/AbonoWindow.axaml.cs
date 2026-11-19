using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class AbonoWindow : Window
{
    private AbonoViewModel? _viewModel;

    public AbonoWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as AbonoViewModel);

        Loaded += (_, _) =>
        {
            MontoBox.Focus();
            MontoBox.SelectAll();
        };
    }

    private void ConectarViewModel(AbonoViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.AbonoRegistrado -= OnAbonoRegistrado;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.AbonoRegistrado += OnAbonoRegistrado;
    }

    private void OnAbonoRegistrado() => Close(true);

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
