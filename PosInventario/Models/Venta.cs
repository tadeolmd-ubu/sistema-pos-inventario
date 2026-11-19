using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class Venta
{
    public int Id { get; set; }

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int IdCaja { get; set; }
    public Caja Caja { get; set; } = null!;

    /// <summary>
    /// Cliente de la venta. Es nulo para las ventas a público general.
    /// </summary>
    public int? IdCliente { get; set; }
    public Cliente? Cliente { get; set; }

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

    /// <summary>
    /// Indica si la venta ya tiene alguna devolución registrada.
    /// </summary>
    public bool HayDevolucion => TotalDevuelto > 0;

    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    /// <summary>
    /// En una venta a crédito el total queda pendiente en la cuenta del
    /// cliente; el monto recibido y el vuelto son cero.
    /// </summary>
    public TipoVenta TipoVenta { get; set; } = TipoVenta.Contado;

    public decimal MontoRecibido { get; set; }
    public decimal Vuelto { get; set; }

    public EstadoVenta Estado { get; set; } = EstadoVenta.Completada;
    public DateTime? FechaAnulacion { get; set; }
    public int? IdUsuarioAnulacion { get; set; }

    public bool EsCredito => TipoVenta == Enums.TipoVenta.Credito;

    public bool TieneCliente => IdCliente is not null;

    public string ClienteTexto => Cliente?.Nombre ?? "Público";

    public string MetodoPagoTexto => MetodoPago.Nombre();

    public string TipoVentaTexto => EsCredito ? "Crédito" : "Contado";

    public ICollection<DetalleVenta> Detalles { get; set; } = [];
    public ICollection<PagoVenta> Pagos { get; set; } = [];
    public ICollection<Abono> Abonos { get; set; } = [];
    public ICollection<MovimientoCaja> MovimientosCaja { get; set; } = [];
    public ICollection<MovimientoInventario> Movimientos { get; set; } = [];
}
