using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ConfiguracionView : UserControl
{
    public ConfiguracionView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Configuracion;
    }

    private async void ElegirCarpeta_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConfiguracionViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var carpetas = await ventana.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Seleccionar carpeta para el respaldo",
                AllowMultiple = false
            });

        var carpeta = carpetas.FirstOrDefault()?.Path.LocalPath;

        if (!string.IsNullOrWhiteSpace(carpeta))
            await viewModel.CrearRespaldoAsync(carpeta);
    }
}
