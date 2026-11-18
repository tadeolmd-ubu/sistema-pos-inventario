using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace PosInventario.Views;

/// <summary>
/// Abre el selector de archivos y guarda el libro de Excel del reporte.
/// </summary>
internal static class ExportadorExcelArchivo
{
    public static async Task<string?> GuardarAsync(
        Window ventana,
        string titulo,
        string nombreSugerido,
        byte[] contenido)
    {
        var archivo = await ventana.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = titulo,
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "xlsx",
                ShowOverwritePrompt = true,
                FileTypeChoices =
                [
                    new FilePickerFileType("Libro de Excel")
                    {
                        Patterns = ["*.xlsx"],
                        MimeTypes =
                        [
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        ]
                    }
                ]
            });

        if (archivo is null)
            return null;

        await using var flujo = await archivo.OpenWriteAsync();
        await flujo.WriteAsync(contenido);
        await flujo.FlushAsync();

        return archivo.Path.LocalPath;
    }
}
