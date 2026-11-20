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
            viewModel.SeleccionarCarpetaRespaldo(carpeta);
    }

    private async void CrearRespaldo_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConfiguracionViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var carpeta = viewModel.CarpetaDestinoRespaldo;

        var confirmacion = new ConfirmacionWindow(
            "Crear respaldo",
            $"Se creará un respaldo de la base de datos en:\n\n" +
            $"{carpeta}\n\n" +
            "El respaldo no modifica la base actual y se verifica antes de " +
            "darlo por bueno.\n\n" +
            "¿Crear el respaldo ahora?");

        if (!await confirmacion.ShowDialog<bool>(ventana))
            return;

        var resultado = await viewModel.CrearRespaldoAsync(carpeta);

        // Sin resultado significa que el ViewModel ya explicó el motivo en
        // pantalla (sin permisos o carpeta inválida).
        if (resultado is null)
            return;

        if (!resultado.Correcta)
        {
            await new MensajeWindow(new MensajeDialogo(
                "No se pudo crear el respaldo",
                resultado.Mensaje,
                TipoMensajeOperacion.Error)).ShowDialog(ventana);

            return;
        }

        await new MensajeWindow(new MensajeDialogo(
            "Respaldo creado",
            "El respaldo se creó y se verificó correctamente.\n\n" +
            $"{resultado.Valor}",
            TipoMensajeOperacion.Exito)).ShowDialog(ventana);
    }

    private async void RestaurarRespaldo_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConfiguracionViewModel viewModel)
            return;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var archivos = await ventana.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Seleccionar respaldo de la base de datos",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Respaldos de la base de datos")
                    {
                        Patterns = ["*.db"]
                    }
                ]
            });

        var ruta = archivos.FirstOrDefault()?.Path.LocalPath;

        if (string.IsNullOrWhiteSpace(ruta))
            return;

        var validacion = await viewModel.ValidarRespaldoAsync(ruta);

        if (validacion is null || !validacion.Correcta || validacion.Valor is null)
            return;

        var info = validacion.Valor;
        var tamano = info.TamanoBytes >= 1024 * 1024
            ? $"{info.TamanoBytes / 1024d / 1024d:0.0} MB"
            : $"{info.TamanoBytes / 1024d:0.0} KB";

        var confirmacion = new ConfirmacionWindow(
            "Restaurar respaldo",
            $"Se reemplazará la base de datos actual con:\n\n" +
            $"{Path.GetFileName(info.Ruta)}\n" +
            $"Modificado: {info.FechaModificacion:dd/MM/yyyy HH:mm}\n" +
            $"Tamaño: {tamano}\n\n" +
            "Antes de restaurar se creará un respaldo de seguridad de la base " +
            "actual. Al terminar, la aplicación se reiniciará.\n\n" +
            "¿Restaurar este respaldo?");

        if (!await confirmacion.ShowDialog<bool>(ventana))
            return;

        var resultado = await viewModel.RestaurarRespaldoAsync(info.Ruta);

        if (resultado is null || !resultado.Correcta || resultado.Valor is null)
            return;

        await new MensajeWindow(new MensajeDialogo(
            "Restauración completada",
            "La base de datos se restauró correctamente.\n\n" +
            "Respaldo de seguridad de la base anterior:\n" +
            $"{resultado.Valor.RutaRespaldoSeguridad}\n\n" +
            "La aplicación se reiniciará ahora.",
            TipoMensajeOperacion.Exito)).ShowDialog(ventana);

        viewModel.SolicitarReinicio();
    }
}
