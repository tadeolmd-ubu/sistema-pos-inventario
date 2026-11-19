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
    public decimal Cantidad { get; set; }
    public decimal StockAnterior { get; set; }
    public decimal StockResultante { get; set; }
    public string? Referencia { get; set; }

    public int? IdVenta { get; set; }
    public Venta? Venta { get; set; }

    public int? IdCompra { get; set; }
    public Compra? Compra { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public UnidadMedida Unidad =>
        Producto?.UnidadMedida ?? UnidadMedida.Pieza;

    public string CantidadTexto => Unidad.FormatearConUnidad(Cantidad);

    public string StockAnteriorTexto =>
        Unidad.FormatearConUnidad(StockAnterior);

    public string StockResultanteTexto =>
        Unidad.FormatearConUnidad(StockResultante);
}
