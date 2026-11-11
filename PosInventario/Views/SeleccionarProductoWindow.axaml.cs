using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);

        Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            BusquedaBox.Focus();
            BusquedaBox.SelectAll();
        });
    }

    private void ConectarViewModel(SeleccionarProductoViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.ProductoSeleccionado -= OnProductoSeleccionado;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.ProductoSeleccionado += OnProductoSeleccionado;
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || _viewModel is null)
            return;

        if (BusquedaBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;

            if (_viewModel.SelectedProducto is null &&
                _viewModel.ProductosItems.Count > 0)
            {
                _viewModel.SelectedProducto = _viewModel.ProductosItems[0];
            }

            SeleccionarProducto();
            return;
        }

        if (ProductosLista.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            SeleccionarProducto();
        }
    }

    private void SeleccionarProducto()
    {
        if (_viewModel?.SeleccionarCommand.CanExecute(null) == true)
            _viewModel.SeleccionarCommand.Execute(null);
    }

    private void OnProductoSeleccionado(Producto producto) => Close(producto);

    private void Productos_DoubleTapped(object? sender, TappedEventArgs e)
        => _viewModel?.SeleccionarCommand.Execute(null);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(null);
}
