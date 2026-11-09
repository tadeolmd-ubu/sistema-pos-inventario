namespace PosInventario.Models;

public class Producto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Costo { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }

    public DateOnly? FechaArchivado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaModificacion { get; set; } = DateTime.Now;

    public ICollection<DetalleCompra> DetallesCompra { get; set; } = [];
    public ICollection<DetalleVenta> DetallesVenta { get; set; } = [];
    public ICollection<MovimientoInventario> Movimientos { get; set; } = [];
}
