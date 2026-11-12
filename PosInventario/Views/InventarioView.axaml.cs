using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.Service;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class InventarioView : UserControl
{
    public InventarioView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Inventario;
    }

    private async void Exportar_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InventarioViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        try
        {
            var ruta = await ExportadorCsvArchivo.GuardarAsync(
                ventana,
                "Exportar inventario a CSV",
                viewModel.NombreArchivoExportacion,
                viewModel.GenerarCsv());

            if (ruta is null)
                return;

            viewModel.NotificarExportacion(
                $"Se exportaron {viewModel.CantidadExportable} movimientos a:\n{ruta}");
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo exportar el inventario.", ex);

            viewModel.NotificarExportacion(
                $"No se pudo exportar el inventario: {ex.Message}",
                correcta: false);
        }
    }
}
