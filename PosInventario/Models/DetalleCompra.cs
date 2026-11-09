namespace PosInventario.Models;

public class DetalleCompra
{
    public int Id { get; set; }

    public int IdCompra { get; set; }
    public Compra Compra { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
