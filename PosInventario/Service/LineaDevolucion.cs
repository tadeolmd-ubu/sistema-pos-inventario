namespace PosInventario.Service;

/// <summary>
/// Línea de entrada para registrar una devolución: cuántas unidades del
/// detalle de la venta se devuelven.
/// </summary>
public sealed record LineaDevolucion(int IdDetalleVenta, decimal Cantidad);
