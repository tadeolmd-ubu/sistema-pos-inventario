using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.Service;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ComprasView : UserControl
{
    public ComprasView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Compras;
    }

    private async void Anular_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ComprasViewModel viewModel || viewModel.SelectedCompra is null)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var confirmacion = new ConfirmacionWindow(
            "Anular compra",
            $"¿Anular la compra C-{viewModel.SelectedCompra.Id:000000}? " +
            "Se revertirá el stock de sus productos.");

        if (await confirmacion.ShowDialog<bool>(ventana))
            await viewModel.AnularSeleccionadaAsync();
    }

    private async void Exportar_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ComprasViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        try
        {
            var ruta = await ExportadorCsvArchivo.GuardarAsync(
                ventana,
                "Exportar compras a CSV",
                viewModel.NombreArchivoExportacion,
                viewModel.GenerarCsv());

            if (ruta is null)
                return;

            viewModel.NotificarExportacion(
                $"Se exportaron {viewModel.CantidadExportable} compras a:\n{ruta}");
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudieron exportar las compras.", ex);

            viewModel.NotificarExportacion(
                $"No se pudieron exportar las compras: {ex.Message}",
                correcta: false);
        }
    }
}
