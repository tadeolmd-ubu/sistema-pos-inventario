namespace PosInventario.Views;

/// <summary>
/// Decide si la ventana principal cabe en el área de trabajo de la pantalla.
/// Cuando no cabe (por ejemplo, 1366x768), se abre maximizada para que todos
/// los controles sigan siendo alcanzables.
/// </summary>
public static class AjusteVentana
{
    public const double MargenPredeterminado = 16;

    public static bool DebeMaximizar(
        double anchoDeseado,
        double altoDeseado,
        double anchoDisponible,
        double altoDisponible,
        double margen = MargenPredeterminado)
    {
        if (!EsMedidaValida(anchoDeseado) || !EsMedidaValida(altoDeseado) ||
            !EsMedidaValida(anchoDisponible) || !EsMedidaValida(altoDisponible))
            return false;

        return anchoDeseado > anchoDisponible - margen ||
               altoDeseado > altoDisponible - margen;
    }

    private static bool EsMedidaValida(double valor)
        => double.IsFinite(valor) && valor > 0;
}
