namespace PosInventario.Models.Enums;

public enum MetodoPago
{
    Efectivo,
    Tarjeta,
    Transferencia,

    /// <summary>
    /// Solo como resumen de la venta: indica que se pagó con más de un
    /// método. No es válido como una fila de pago individual.
    /// </summary>
    Mixto
}
