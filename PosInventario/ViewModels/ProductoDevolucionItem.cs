using CommunityToolkit.Mvvm.ComponentModel;
using PosInventario.Models;

namespace PosInventario.ViewModels;

/// <summary>
/// Línea de producto del checklist de devolución. Con una sola unidad
/// pendiente se marca directo; con varias, el encabezado "Nombre - xN"
/// despliega las unidades para marcarlas una por una.
/// </summary>
public partial class ProductoDevolucionItem : ObservableObject
{
    private readonly Action _cambio;

    public ProductoDevolucionItem(DetalleVenta detalle, Action cambio)
    {
        Detalle = detalle;
        _cambio = cambio;

        var nombre = Nombre;

        Unidades = Enumerable
            .Range(1, detalle.CantidadPendiente)
            .Select(_ => new UnidadDevolucionItem(nombre, cambio))
            .ToList();
    }

    public DetalleVenta Detalle { get; }

    public IReadOnlyList<UnidadDevolucionItem> Unidades { get; }

    public int IdDetalleVenta => Detalle.Id;

    public string Nombre => Detalle.Producto?.Nombre ?? "Producto";

    public bool EsUnidadUnica => Unidades.Count == 1;

    public bool EsGrupo => Unidades.Count > 1;

    public string TituloGrupo => $"{Nombre} - x{Unidades.Count}";

    /// <summary>
    /// Única unidad de la línea cuando solo queda una pendiente.
    /// </summary>
    public UnidadDevolucionItem Unica => Unidades[0];

    [ObservableProperty] private bool expandido;

    public int CantidadSeleccionada => Unidades.Count(u => u.Seleccionada);

    public bool TodasSeleccionadas => Unidades.All(u => u.Seleccionada);

    public void DefinirSeleccion(bool seleccionada, bool expandir)
    {
        if (expandir)
            Expandido = true;

        foreach (var unidad in Unidades)
            unidad.Definir(seleccionada);
    }
}
