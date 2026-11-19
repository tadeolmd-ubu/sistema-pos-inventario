using System.Globalization;
using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class Producto
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public UnidadMedida UnidadMedida { get; set; } = UnidadMedida.Pieza;
    public decimal PrecioVenta { get; set; }
    public decimal Costo { get; set; }
    public decimal Stock { get; set; }
    public decimal StockMinimo { get; set; }

    public DateOnly? FechaArchivado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaModificacion { get; set; } = DateTime.Now;

    public ICollection<DetalleCompra> DetallesCompra { get; set; } = [];
    public ICollection<DetalleVenta> DetallesVenta { get; set; } = [];
    public ICollection<MovimientoInventario> Movimientos { get; set; } = [];

    public bool EsPorPeso => UnidadMedida.EsPorPeso();

    public string UnidadTexto => UnidadMedida.Abreviatura();

    public string PrecioVentaTexto => EsPorPeso
        ? $"{PrecioVenta.ToString("0.00", Cultura)}/kg"
        : PrecioVenta.ToString("0.00", Cultura);

    public string CostoTexto => EsPorPeso
        ? $"{Costo.ToString("0.00", Cultura)}/kg"
        : Costo.ToString("0.00", Cultura);

    public string StockTexto => UnidadMedida.FormatearConUnidad(Stock);

    public string StockMinimoTexto =>
        UnidadMedida.FormatearConUnidad(StockMinimo);
}
