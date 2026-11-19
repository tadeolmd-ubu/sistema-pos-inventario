using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Reglas comunes para cantidades. Los productos por peso usan kilogramos con
/// tres decimales; los productos por pieza solo aceptan enteros.
/// </summary>
public static class Cantidades
{
    public const int Decimales = 3;

    public static decimal Normalizar(decimal cantidad)
        => decimal.Round(cantidad, Decimales, MidpointRounding.AwayFromZero);

    public static bool TieneDemasiadosDecimales(decimal cantidad)
        => Normalizar(cantidad) != cantidad;

    public static bool EsEntera(decimal cantidad)
        => decimal.Truncate(cantidad) == cantidad;

    public static bool EsCantidadValida(
        decimal cantidad,
        UnidadMedida unidad,
        bool permitirCero = false)
    {
        if (cantidad < 0 || (!permitirCero && cantidad == 0))
            return false;

        if (TieneDemasiadosDecimales(cantidad))
            return false;

        return unidad != UnidadMedida.Pieza || EsEntera(cantidad);
    }

    /// <summary>
    /// Devuelve solo el primer problema de la cantidad, para que la interfaz
    /// pueda mostrar un mensaje corto y específico.
    /// </summary>
    public static string? ErrorCantidad(
        decimal cantidad,
        UnidadMedida unidad,
        string etiqueta = "La cantidad")
    {
        if (cantidad <= 0)
            return $"{etiqueta} debe ser mayor que 0.";

        if (TieneDemasiadosDecimales(cantidad))
            return $"{etiqueta} no puede tener más de 3 decimales.";

        if (unidad == UnidadMedida.Pieza && !EsEntera(cantidad))
            return $"{etiqueta} debe ser un número entero.";

        return null;
    }

    public static string Formatear(decimal cantidad, UnidadMedida unidad)
        => unidad.Formatear(cantidad);

    public static string FormatearConUnidad(
        decimal cantidad, UnidadMedida unidad)
        => unidad.FormatearConUnidad(cantidad);
}
