using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PosInventario.Service;

namespace PosInventario.Views;

/// <summary>
/// Abre el selector de archivos y escribe el CSV elegido. Se mantiene fuera de
/// los ViewModels para que la lógica de exportación siga siendo testeable.
/// </summary>
internal static class ExportadorCsvArchivo
{
    public static async Task<string?> GuardarAsync(
        Window ventana,
        string titulo,
        string nombreSugerido,
        string contenido)
    {
        var archivo = await ventana.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = titulo,
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "csv",
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    new FilePickerFileType("Archivo CSV")
                    {
                        Patterns = ["*.csv"],
                        MimeTypes = ["text/csv"]
                    }
                ]
            });

        if (archivo is null)
            return null;

        await using var flujo = await archivo.OpenWriteAsync();
        await ExportadorCsv.EscribirAsync(flujo, contenido);

        return archivo.Path.LocalPath;
    }
}
