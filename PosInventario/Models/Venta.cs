using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class Venta
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int IdCaja { get; set; }
    public Caja Caja { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.Now;
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    /// <summary>
    /// Total ya devuelto al cliente por devoluciones totales o parciales.
    /// No puede superar el total de la venta.
    /// </summary>
    public decimal TotalDevuelto { get; set; }

    /// <summary>
    /// Total neto de devoluciones: lo que la venta sigue aportando.
    /// </summary>
    public decimal TotalNeto => Total - TotalDevuelto;

    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
    public decimal MontoRecibido { get; set; }
    public decimal Vuelto { get; set; }

    public EstadoVenta Estado { get; set; } = EstadoVenta.Completada;
    public DateTime? FechaAnulacion { get; set; }
    public int? IdUsuarioAnulacion { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = [];
    public ICollection<MovimientoCaja> MovimientosCaja { get; set; } = [];
    public ICollection<MovimientoInventario> Movimientos { get; set; } = [];
}
