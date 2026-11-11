using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Pago de entrada para registrar una venta. <see cref="Monto"/> es la parte
/// aplicada a la venta; en efectivo, <see cref="MontoRecibido"/> es lo que
/// entrega el cliente y permite calcular el vuelto.
/// </summary>
public sealed record LineaPago(
    MetodoPago Metodo,
    decimal Monto,
    decimal? MontoRecibido = null);
