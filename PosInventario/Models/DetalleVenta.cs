using System.Globalization;
using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class DetalleVenta
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public int Id { get; set; }

    public int IdVenta { get; set; }
    public Venta Venta { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public decimal Cantidad { get; set; }

    /// <summary>
    /// Unidades ya devueltas de esta línea. No puede superar la cantidad vendida.
    /// </summary>
    public decimal CantidadDevuelta { get; set; }

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
    public decimal CantidadPendiente => Cantidad - CantidadDevuelta;

    public UnidadMedida Unidad =>
        Producto?.UnidadMedida ?? UnidadMedida.Pieza;

    public string CantidadTexto => Unidad.Formatear(Cantidad);

    public string CantidadConUnidadTexto =>
        Unidad.FormatearConUnidad(Cantidad);

    public string CantidadTicketTexto => Unidad.EsPorPeso()
        ? $"{Unidad.Formatear(Cantidad)} {Unidad.Abreviatura()}"
        : Unidad.Formatear(Cantidad);

    public string CantidadPendienteTexto =>
        Unidad.FormatearConUnidad(CantidadPendiente);

    public string PrecioUnitarioTexto => Unidad.EsPorPeso()
        ? $"{PrecioUnitario.ToString("0.00", Cultura)}/kg"
        : PrecioUnitario.ToString("0.00", Cultura);
}
