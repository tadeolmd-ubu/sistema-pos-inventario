using PosInventario.Models.Enums;

namespace PosInventario.Models;

/// <summary>
/// Parte de una venta pagada con un método concreto. Una venta puede tener
/// varias filas cuando el cliente paga con una combinación de métodos.
/// </summary>
public class PagoVenta
{
    public int Id { get; set; }

    public int IdVenta { get; set; }
    public Venta Venta { get; set; } = null!;

    public MetodoPago Metodo { get; set; }

    /// <summary>
    /// Monto aplicado a la venta con este método. No incluye el vuelto.
    /// </summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Parte de este pago ya reembolsada al cliente por devoluciones.
    /// </summary>
    public decimal MontoDevuelto { get; set; }

    public decimal MontoPendiente => Monto - MontoDevuelto;
}
