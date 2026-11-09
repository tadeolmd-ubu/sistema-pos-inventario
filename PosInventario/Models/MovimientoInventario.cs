using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class MovimientoInventario
{
    public int Id { get; set; }

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockResultante { get; set; }
    public string? Referencia { get; set; }

    public int? IdVenta { get; set; }
    public Venta? Venta { get; set; }

    public int? IdCompra { get; set; }
    public Compra? Compra { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;
}
