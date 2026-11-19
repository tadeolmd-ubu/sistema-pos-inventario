namespace PosInventario.Models.Enums;

public static class MetodoPagoExtensiones
{
    public static string Nombre(this MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transferencia",
        MetodoPago.Credito => "Crédito",
        _ => "Mixto"
    };
}

public enum MetodoPago
{
    Efectivo,
    Tarjeta,
    Transferencia,

    /// <summary>
    /// La venta queda pendiente en la cuenta del cliente. No genera un pago
    /// en efectivo ni una fila de PagoVenta.
    /// </summary>
    Credito,

    /// <summary>
    /// Solo como resumen de la venta: indica que se pagó con más de un
    /// método. No es válido como una fila de pago individual.
    /// </summary>
    Mixto
}
