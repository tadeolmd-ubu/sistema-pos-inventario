using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PosInventario.Models;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class SeleccionarProductoWindow : Window
{
    private SeleccionarProductoViewModel? _viewModel;

    public SeleccionarProductoWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as SeleccionarProductoViewModel);
    }

    private void ConectarViewModel(SeleccionarProductoViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.ProductoSeleccionado -= OnProductoSeleccionado;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.ProductoSeleccionado += OnProductoSeleccionado;
    }

    private void OnProductoSeleccionado(Producto producto) => Close(producto);

    private void Productos_DoubleTapped(object? sender, TappedEventArgs e)
        => _viewModel?.SeleccionarCommand.Execute(null);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(null);
}
