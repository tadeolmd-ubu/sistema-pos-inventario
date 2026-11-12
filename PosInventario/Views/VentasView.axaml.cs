using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PosInventario.Models;
using PosInventario.Service;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class VentasView : UserControl
{
    private VentasViewModel? _viewModel;

    public VentasView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as VentasViewModel);

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Ventas;
    }

    private void ConectarViewModel(VentasViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.TicketSolicitado -= OnTicket;
            _viewModel.DevolucionSolicitada -= OnDevolucion;
        }

        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.TicketSolicitado += OnTicket;
            _viewModel.DevolucionSolicitada += OnDevolucion;
        }
    }

    private void Ventas_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel?.VerTicketCommand.CanExecute(null) == true)
            _viewModel.VerTicketCommand.Execute(null);
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

    private async void OnDevolucion(Venta venta)
    {
        if (_viewModel is null || TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var devolucion = _viewModel.CrearDevolucionViewModel(venta);

        if (devolucion is null)
            return;

        var registrada = await new DevolverVentaWindow { DataContext = devolucion }
            .ShowDialog<bool>(ventana);

        if (registrada)
            await _viewModel.CargarAsync();
    }

    private async void Exportar_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not VentasViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        try
        {
            var ruta = await ExportadorCsvArchivo.GuardarAsync(
                ventana,
                "Exportar ventas a CSV",
                viewModel.NombreArchivoExportacion,
                viewModel.GenerarCsv());

            if (ruta is null)
                return;

            viewModel.NotificarExportacion(
                $"Se exportaron {viewModel.CantidadExportable} ventas de la " +
                $"página {viewModel.Paginacion.PaginaActual} a:\n{ruta}");
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudieron exportar las ventas.", ex);

            viewModel.NotificarExportacion(
                $"No se pudieron exportar las ventas: {ex.Message}",
                correcta: false);
        }
    }
}
