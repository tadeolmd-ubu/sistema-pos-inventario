namespace PosInventario.Service;

/// <summary>
/// Línea de entrada para registrar una compra. El servicio calcula el
/// subtotal y el total; no confía en valores calculados por la UI.
/// </summary>
public sealed record LineaCompra(
    int IdProducto,
    decimal Cantidad,
    decimal CostoUnitario);
