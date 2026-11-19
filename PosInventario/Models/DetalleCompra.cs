using System.Globalization;
using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class DetalleCompra
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public int Id { get; set; }

    public int IdCompra { get; set; }
    public Compra Compra { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public UnidadMedida Unidad =>
        Producto?.UnidadMedida ?? UnidadMedida.Pieza;

    public string CantidadTexto => Unidad.FormatearConUnidad(Cantidad);

    public string CostoUnitarioTexto => Unidad.EsPorPeso()
        ? $"{CostoUnitario.ToString("0.00", Cultura)}/kg"
        : CostoUnitario.ToString("0.00", Cultura);
}
