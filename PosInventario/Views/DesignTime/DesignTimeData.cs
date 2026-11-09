using PosInventario.Models;
using PosInventario.ViewModels;

namespace PosInventario.Views.DesignTime;

public sealed class DesignTimeData
{
    public static readonly InicioDesignTime Inicio = new();
    public static readonly ElementosDesignTime Elementos = new();
}

public sealed class InicioDesignTime : InicioViewModel
{
    public InicioDesignTime()
    {
        TotalElementos = 12;
        TotalCategorias = 4;
    }
}

public sealed class ElementosDesignTime : ElementosViewModel
{
    public ElementosDesignTime()
    {
        var categoria = new Categoria { Id = 1, Nombre = "General", Activo = 1 };
        CategoriasItems.Add(categoria);

        var elemento = new Elemento
        {
            Id = 1,
            Nombre = "Elemento de ejemplo",
            Descripcion = "Dato de diseño, no viene de la base.",
            IdCategoria = 1,
            Categoria = categoria,
            Activo = 1
        };

        ElementosItems.Add(elemento);
        SelectedElemento = elemento;
    }
}
