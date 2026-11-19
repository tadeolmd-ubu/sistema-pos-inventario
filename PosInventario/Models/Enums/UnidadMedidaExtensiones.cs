using System.Globalization;

namespace PosInventario.Models.Enums;

public static class UnidadMedidaExtensiones
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public static bool EsPorPeso(this UnidadMedida unidad)
        => unidad == UnidadMedida.Kilogramo;

    public static string Abreviatura(this UnidadMedida unidad)
        => unidad == UnidadMedida.Kilogramo ? "kg" : "pza";

    public static string Nombre(this UnidadMedida unidad)
        => unidad == UnidadMedida.Kilogramo ? "Kilogramo (kg)" : "Pieza";

    public static string Formatear(this UnidadMedida unidad, decimal cantidad)
        => unidad == UnidadMedida.Kilogramo
            ? cantidad.ToString("0.###", Cultura)
            : cantidad.ToString("0", Cultura);

    public static string FormatearConUnidad(
        this UnidadMedida unidad, decimal cantidad)
        => $"{unidad.Formatear(cantidad)} {unidad.Abreviatura()}";
}
