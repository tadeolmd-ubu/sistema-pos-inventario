namespace PosInventario.Service;

/// <summary>
/// Totales de las ventas completadas de una caja, separados por método de pago.
/// </summary>
public sealed record ResumenPagosCaja(
    int CantidadVentas,
    decimal Efectivo,
    decimal Tarjeta,
    decimal Transferencia)
{
    public decimal Total => Efectivo + Tarjeta + Transferencia;
}
