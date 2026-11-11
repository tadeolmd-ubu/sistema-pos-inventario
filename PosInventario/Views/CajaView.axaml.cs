using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

        AddHandler(KeyDownEvent, Caja_KeyDownTunnel, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, Caja_KeyDownBubble, RoutingStrategies.Bubble);

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as CajaViewModel);

        Loaded += (_, _) => EnfocarBusqueda();

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

        EnfocarBusqueda();
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

        EnfocarBusqueda();
    }

    private async void OnEliminarLinea(LineaVentaItem linea)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var focoEnGrid = LineasVentaGrid.IsKeyboardFocusWithin;

        var confirmacion = new ConfirmacionWindow(
            "Eliminar producto",
            $"¿Quitar {linea.Nombre} de la venta?");

        if (await confirmacion.ShowDialog<bool>(ventana))
            _viewModel?.EliminarLinea(linea);

        if (focoEnGrid)
            LineasVentaGrid.Focus();
        else
            EnfocarBusqueda();
    }

    private async void OnTicket(Venta venta)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await new TicketWindow
        {
            DataContext = new TicketViewModel(venta)
        }.ShowDialog(ventana);

        EnfocarBusqueda();
    }

    private async void OnRegistrarMovimiento(RegistrarMovimientoCajaViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var registrado = await new RegistrarMovimientoCajaWindow { DataContext = viewModel }
            .ShowDialog<bool>(ventana);

        if (registrado && _viewModel is not null)
            await _viewModel.CargarAsync();

        EnfocarBusqueda();
    }

    private async void OnCerrarCaja(CerrarCajaViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var cerrada = await new CerrarCajaWindow { DataContext = viewModel }
            .ShowDialog<bool>(ventana);

        if (cerrada && _viewModel is not null)
            await _viewModel.CargarAsync();

        EnfocarBusqueda();
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

        EnfocarBusqueda();
    }

    private async void BusquedaRapida_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        if (_viewModel?.AgregarPorCodigoCommand.CanExecute(null) == true)
            await _viewModel.AgregarPorCodigoCommand.ExecuteAsync(null);

        EnfocarBusqueda();
    }

    private async void Pago_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        await CobrarAsync();
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

        EnfocarBusqueda();
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
        {
            EnfocarBusqueda();
            return;
        }

        var huboDevolucion = await new CajaHistorialWindow { DataContext = detalle }
            .ShowDialog<bool>(ventana);

        if (huboDevolucion && _viewModel is not null)
            await _viewModel.CargarAsync();

        EnfocarBusqueda();
    }

    private async void Caja_KeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (LineasVentaGrid.IsKeyboardFocusWithin &&
            (e.Key == Key.Delete ||
             e.Key == Key.Add || e.Key == Key.OemPlus ||
             e.Key == Key.Subtract || e.Key == Key.OemMinus))
        {
            e.Handled = true;
            EjecutarAccionLinea(e.Key);
            return;
        }

        switch (e.Key)
        {
            case Key.F1:
                e.Handled = true;
                await MostrarAtajosAsync();
                break;
            case Key.F2:
                e.Handled = true;
                EnfocarBusqueda();
                break;
            case Key.F3:
                e.Handled = true;
                if (_viewModel.SeleccionarProductosCommand.CanExecute(null))
                    _viewModel.SeleccionarProductosCommand.Execute(null);
                break;
            case Key.F4:
                e.Handled = true;
                await CobrarAsync();
                break;
            case Key.F5:
                e.Handled = true;
                await _viewModel.CargarAsync();
                EnfocarBusqueda();
                break;
            case Key.F6:
                e.Handled = true;
                DescuentoBox.Focus();
                DescuentoBox.SelectAll();
                break;
            case Key.F7:
                e.Handled = true;
                EnfocarMetodoPago();
                break;
            case Key.F8:
                e.Handled = true;
                if (_viewModel.SolicitarCancelarVentaCommand.CanExecute(null))
                    _viewModel.SolicitarCancelarVentaCommand.Execute(null);
                break;
            case Key.F9:
                e.Handled = true;
                if (_viewModel.SolicitarRegistrarMovimientoCommand.CanExecute(null))
                    _viewModel.SolicitarRegistrarMovimientoCommand.Execute(null);
                break;
            case Key.F10:
                e.Handled = true;
                if (_viewModel.SolicitarCerrarCajaCommand.CanExecute(null))
                    await _viewModel.SolicitarCerrarCajaCommand.ExecuteAsync(null);
                break;
            case Key.Enter when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                e.Handled = true;
                await CobrarAsync();
                break;
        }
    }

    private void EjecutarAccionLinea(Key key)
    {
        if (LineasVentaGrid.SelectedItem is not LineaVentaItem linea)
            return;

        switch (key)
        {
            case Key.Delete:
                linea.EliminarCommand.Execute(null);
                break;
            case Key.Add:
            case Key.OemPlus:
                linea.AumentarCommand.Execute(null);
                break;
            case Key.Subtract:
            case Key.OemMinus:
                linea.DisminuirCommand.Execute(null);
                break;
        }
    }

    private void EnfocarMetodoPago()
    {
        if (_viewModel?.EsPagoMixto == true)
            MetodoPagoMixtoBox.Focus();
        else
            MetodoPagoSimpleBox.Focus();
    }

    private async Task MostrarAtajosAsync()
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var texto = string.Join(Environment.NewLine, new[]
        {
            "F1   Mostrar esta ayuda",
            "F2   Buscar o escanear",
            "F3   Seleccionar productos",
            "F4   Cobrar",
            "F5   Actualizar caja",
            "F6   Enfocar descuento",
            "F7   Enfocar método de pago",
            "F8   Devolver venta",
            "F9   Registrar movimiento de efectivo",
            "F10  Cerrar caja",
            "Esc  Limpiar venta o cerrar diálogo",
            "Ctrl+Enter  Cobrar",
            "Enter en búsqueda  Agregar producto",
            "Enter en pago  Cobrar",
            "Delete  Quitar la línea seleccionada",
            "+ / -  Cambiar cantidad de la línea seleccionada",
            "Ctrl+P en el ticket  Imprimir"
        });

        await new MensajeWindow(new MensajeDialogo(
            "Atajos de teclado", texto, TipoMensajeOperacion.Informacion))
            .ShowDialog(ventana);

        EnfocarBusqueda();
    }

    private async void Caja_KeyDownBubble(object? sender, KeyEventArgs e)
    {
        if (e.Handled || _viewModel is null || e.Key != Key.Escape)
            return;

        var texto = TextoEnfocado();

        if (texto is not null && !string.IsNullOrEmpty(texto.Text))
        {
            texto.Text = string.Empty;
            e.Handled = true;
            return;
        }

        e.Handled = true;

        await LimpiarVentaConConfirmacionAsync();
    }

    private async Task CobrarAsync()
    {
        if (_viewModel is null)
            return;

        await _viewModel.CobrarCommand.ExecuteAsync(null);

        EnfocarBusqueda();
    }

    private async Task LimpiarVentaConConfirmacionAsync()
    {
        if (_viewModel is null)
            return;

        if (_viewModel.LineasVentaItems.Count == 0)
        {
            _viewModel.LimpiarVentaCommand.Execute(null);
            EnfocarBusqueda();
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Limpiar venta",
            "¿Quitar todos los productos de la venta?");

        if (await confirmacion.ShowDialog<bool>(ventana))
            _viewModel.LimpiarVentaCommand.Execute(null);

        EnfocarBusqueda();
    }

    private TextBox? TextoEnfocado()
    {
        if (BusquedaRapidaBox.IsFocused) return BusquedaRapidaBox;
        if (DescuentoBox.IsFocused) return DescuentoBox;
        if (PagoEfectivoBox.IsFocused) return PagoEfectivoBox;
        if (PagoEfectivoMixtoBox.IsFocused) return PagoEfectivoMixtoBox;
        if (PagoTarjetaBox.IsFocused) return PagoTarjetaBox;
        if (PagoTransferenciaBox.IsFocused) return PagoTransferenciaBox;

        return null;
    }

    private void EnfocarBusqueda()
    {
        Dispatcher.UIThread.Post(() =>
        {
            BusquedaRapidaBox.Focus();
            BusquedaRapidaBox.SelectAll();
        }, DispatcherPriority.Input);
    }
}
