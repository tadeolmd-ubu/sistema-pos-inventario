using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ImportarDatosViewModel : ObservableObject
{
    private readonly ImportacionDatosService? _servicio;
    private readonly ImportacionVistaPrevia? _vista;

    public ImportarDatosViewModel() { }

    public ImportarDatosViewModel(
        ImportacionDatosService servicio,
        ImportacionVistaPrevia vista)
    {
        _servicio = servicio;
        _vista = vista;

        ArchivoTexto = $"Archivo: {Path.GetFileName(vista.Ruta)}";
        ResumenTexto =
            $"{vista.ProductosNuevos} productos nuevos, " +
            $"{vista.ProductosActualizados} se actualizarán, " +
            $"{vista.ProveedoresNuevos} proveedores nuevos " +
            $"({vista.ProveedoresReutilizados} ya existen) y " +
            $"{vista.UnidadesStock.ToString("0.###", CultureInfo.InvariantCulture)} de stock inicial.";

        foreach (var error in vista.Errores)
            Errores.Add($"Fila {error.Fila}: {error.Mensaje}");

        if (Errores.Count == 0)
            MensajeOperacion = "Sin errores. Revisa el resumen y presiona Importar.";
    }

    public ObservableCollection<string> Errores { get; } = [];

    [ObservableProperty] private string archivoTexto = string.Empty;
    [ObservableProperty] private string resumenTexto = string.Empty;
    [ObservableProperty] private string mensajeOperacion = string.Empty;
    [ObservableProperty] private bool importando;
    [ObservableProperty] private bool importada;

    public bool HayErrores => Errores.Count > 0;

    public bool PuedeImportar => _vista?.PuedeImportar == true && !Importando;

    public string MensajeResultado { get; private set; } = string.Empty;

    public async Task ImportarAsync()
    {
        if (_servicio is null || _vista is null || Importando)
            return;

        Importando = true;

        try
        {
            var resultado = await _servicio.ImportarAsync(_vista);
            MensajeOperacion = resultado.Mensaje;

            if (!resultado.Correcta)
                return;

            MensajeResultado = resultado.Mensaje;
            Importada = true;
        }
        catch (Exception ex)
        {
            RegistroApp.Escribir("No se pudo completar la importación.", ex);
            MensajeOperacion = $"No se pudo importar: {ex.Message}";
        }
        finally
        {
            Importando = false;
        }
    }

    partial void OnImportandoChanged(bool value)
        => OnPropertyChanged(nameof(PuedeImportar));
}
