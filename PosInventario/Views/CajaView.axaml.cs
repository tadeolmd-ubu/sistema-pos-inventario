using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PosInventario.Models;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class CajaView : UserControl
{
    private CajaViewModel? _viewModel;

    public CajaView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as CajaViewModel);

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Caja;
    }

    private void ConectarViewModel(CajaViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.SeleccionarProductoSolicitado -= OnSeleccionarProducto;
            _viewModel.EliminarLineaSolicitada -= OnEliminarLinea;
            _viewModel.TicketSolicitado -= OnTicket;
            _viewModel.RegistrarMovimientoSolicitado -= OnRegistrarMovimiento;
            _viewModel.CerrarCajaSolicitado -= OnCerrarCaja;
            _viewModel.CancelarVentaSolicitado -= OnCancelarVenta;
        }

        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.SeleccionarProductoSolicitado += OnSeleccionarProducto;
            _viewModel.EliminarLineaSolicitada += OnEliminarLinea;
            _viewModel.TicketSolicitado += OnTicket;
            _viewModel.RegistrarMovimientoSolicitado += OnRegistrarMovimiento;
            _viewModel.CerrarCajaSolicitado += OnCerrarCaja;
            _viewModel.CancelarVentaSolicitado += OnCancelarVenta;
        }
    }

    private async void OnSeleccionarProducto(SeleccionarProductoViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await viewModel.CargarAsync();

        var producto = await new SeleccionarProductoWindow { DataContext = viewModel }
            .ShowDialog<Producto?>(ventana);

        if (producto is not null)
            _viewModel?.AgregarProducto(producto);
    }

    private async void OnEliminarLinea(LineaVentaItem linea)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Eliminar producto",
            $"¿Quitar {linea.Nombre} de la venta?");

        if (await confirmacion.ShowDialog<bool>(ventana))
            _viewModel?.EliminarLinea(linea);
    }

    private async void OnTicket(Venta venta)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await new TicketWindow
        {
            DataContext = new TicketViewModel(venta)
        }.ShowDialog(ventana);
    }

    private async void OnRegistrarMovimiento(RegistrarMovimientoCajaViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var registrado = await new RegistrarMovimientoCajaWindow { DataContext = viewModel }
            .ShowDialog<bool>(ventana);

        if (registrado && _viewModel is not null)
            await _viewModel.CargarAsync();
    }

    private async void OnCerrarCaja(CerrarCajaViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var cerrada = await new CerrarCajaWindow { DataContext = viewModel }
            .ShowDialog<bool>(ventana);

        if (cerrada && _viewModel is not null)
            await _viewModel.CargarAsync();
    }

    private async void OnCancelarVenta(CancelarVentaViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await viewModel.CargarAsync();

        var cancelada = await new CancelarVentaWindow { DataContext = viewModel }
            .ShowDialog<bool>(ventana);

        if (cancelada && _viewModel is not null)
            await _viewModel.CargarAsync();
    }

    private void BusquedaRapida_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        if (_viewModel?.AgregarPorCodigoCommand.CanExecute(null) == true)
            _viewModel.AgregarPorCodigoCommand.Execute(null);
    }

    private async void Movimientos_SelectionChanged(
        object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lista ||
            lista.SelectedItem is not MovimientoCaja movimiento)
            return;

        lista.SelectedIndex = -1;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await new MovimientoCajaWindow
        {
            DataContext = new MovimientoCajaViewModel(movimiento)
        }.ShowDialog(ventana);
    }

    private async void Historial_SelectionChanged(
        object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lista || lista.SelectedItem is not Caja caja)
            return;

        lista.SelectedIndex = -1;

        if (_viewModel is null || TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var detalle = await _viewModel.CrearHistorialViewModelAsync(caja);

        if (detalle is null)
            return;

        var huboDevolucion = await new CajaHistorialWindow { DataContext = detalle }
            .ShowDialog<bool>(ventana);

        if (huboDevolucion && _viewModel is not null)
            await _viewModel.CargarAsync();
    }
}
