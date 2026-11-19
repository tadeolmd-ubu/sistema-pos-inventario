using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.Service;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ReportesView : UserControl
{
    public ReportesView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Reportes;
    }

    private async void ExportarExcel_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportesViewModel viewModel ||
            !viewModel.PuedeExportar ||
            TopLevel.GetTopLevel(this) is not Window ventana)
        {
            return;
        }

        try
        {
            var ruta = await ExportadorExcelArchivo.GuardarAsync(
                ventana,
                "Exportar reporte a Excel",
                viewModel.NombreArchivoExportacion,
                viewModel.GenerarExcel());

            if (ruta is null)
                return;

            viewModel.NotificarExportacion(
                $"Se exportó el reporte a:\n{ruta}");
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo exportar el reporte.", ex);

            viewModel.NotificarExportacion(
                $"No se pudo exportar el reporte: {ex.Message}",
                correcta: false);
        }
    }
}
