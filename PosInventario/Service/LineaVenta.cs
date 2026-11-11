namespace PosInventario.Service;

/// <summary>
/// Línea de entrada para registrar una venta. El servicio toma el precio
/// actual del producto y calcula el subtotal.
/// </summary>
public sealed record LineaVenta(int IdProducto, int Cantidad);
