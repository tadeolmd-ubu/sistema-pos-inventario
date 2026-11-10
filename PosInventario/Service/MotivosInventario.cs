using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Motivos disponibles para los movimientos manuales de inventario.
/// La referencia del movimiento se guarda con el texto elegido.
/// </summary>
public static class MotivosInventario
{
    public static IReadOnlyList<string> Entrada { get; } =
        ["Carga inicial", "Devolución de cliente", "Corrección", "Otro"];

    public static IReadOnlyList<string> Salida { get; } =
        ["Merma", "Pérdida", "Rotura", "Vencimiento", "Uso interno", "Otro"];

    public static IReadOnlyList<string> Ajuste { get; } =
        ["Conteo físico", "Corrección de inventario", "Otro"];

    public static IReadOnlyList<string> Para(TipoMovimiento tipo) => tipo switch
    {
        TipoMovimiento.Entrada => Entrada,
        TipoMovimiento.Salida => Salida,
        TipoMovimiento.Ajuste => Ajuste,
        _ => []
    };
}
