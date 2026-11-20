using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PosInventario.Service;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ProductosView : UserControl
{
    public ProductosView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Productos;
    }

    private async void Sku_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        if (DataContext is not ProductosViewModel viewModel)
            return;

        await viewModel.EscanearCodigoAsync(SkuBox.Text);

        // Código nuevo: el foco pasa a Nombre para capturar el resto.
        if (viewModel.EsModoCreacion && !string.IsNullOrWhiteSpace(viewModel.Sku))
            NombreBox.Focus();
    }

    private async void Exportar_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductosViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        try
        {
            var ruta = await ExportadorCsvArchivo.GuardarAsync(
                ventana,
                "Exportar productos a CSV",
                viewModel.NombreArchivoExportacion,
                viewModel.GenerarCsvProductos());

            if (ruta is null)
                return;

            viewModel.NotificarExportacion(
                $"Se exportaron {viewModel.CantidadExportable} productos a:\n{ruta}");
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo exportar el catálogo de productos.", ex);

            viewModel.NotificarExportacion(
                $"No se pudo exportar el catálogo: {ex.Message}",
                correcta: false);
        }
    }

    private async void Importar_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductosViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var archivos = await ventana.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Seleccionar archivo para importar",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Archivos CSV")
                    {
                        Patterns = ["*.csv", "*.txt", "*.tsv"]
                    }
                ]
            });

        var ruta = archivos.FirstOrDefault()?.Path.LocalPath;

        if (string.IsNullOrWhiteSpace(ruta))
            return;

        var importacion = await viewModel.PrepararImportacionAsync(ruta);

        if (importacion is null)
            return;

        var importada = await new ImportarDatosWindow
        {
            DataContext = importacion
        }.ShowDialog<bool>(ventana);

        if (importada)
            await viewModel.RecargarTrasImportacionAsync(
                importacion.MensajeResultado);
    }

    private async void Plantilla_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductosViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var ruta = await ExportadorCsvArchivo.GuardarAsync(
            ventana,
            "Guardar plantilla de importación",
            "plantilla-importacion.csv",
            ProductosViewModel.GenerarPlantillaImportacion());

        if (!string.IsNullOrWhiteSpace(ruta))
            viewModel.NotificarPlantillaGuardada(ruta);
    }
}
