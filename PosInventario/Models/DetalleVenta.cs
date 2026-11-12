namespace PosInventario.Models;

public class DetalleVenta
{
    public int Id { get; set; }

    public int IdVenta { get; set; }
    public Venta Venta { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }

    /// <summary>
    /// Unidades ya devueltas de esta línea. No puede superar la cantidad vendida.
    /// </summary>
    public int CantidadDevuelta { get; set; }

    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Costo del producto al momento de la venta. Se guarda como foto del
    /// costo para que la utilidad histórica no cambie si el producto se
    /// actualiza después.
    /// </summary>
    public decimal CostoUnitario { get; set; }

    public decimal Subtotal { get; set; }

    /// <summary>
    /// Unidades de la línea que aún se pueden devolver.
    /// </summary>
    public int CantidadPendiente => Cantidad - CantidadDevuelta;
}
