using PosInventario.Models;

namespace PosInventario.Service;

/// <summary>
/// Parte de un reembolso que corresponde a un pago concreto de la venta.
/// </summary>
public sealed record ReembolsoPago(PagoVenta Pago, decimal Monto);
