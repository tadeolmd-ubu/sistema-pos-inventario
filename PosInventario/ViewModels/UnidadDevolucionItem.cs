using CommunityToolkit.Mvvm.ComponentModel;

namespace PosInventario.ViewModels;

/// <summary>
/// Una unidad marcable del checklist de devolución.
/// </summary>
public partial class UnidadDevolucionItem : ObservableObject
{
    private readonly Action _cambio;

    public UnidadDevolucionItem(string etiqueta, Action cambio)
    {
        Etiqueta = etiqueta;
        _cambio = cambio;
    }

    public string Etiqueta { get; }

    [ObservableProperty] private bool seleccionada;

    public void Definir(bool seleccionada)
    {
        if (Seleccionada == seleccionada) return;

        Seleccionada = seleccionada;
    }

    partial void OnSeleccionadaChanged(bool value) => _cambio();
}
