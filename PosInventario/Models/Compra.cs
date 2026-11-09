using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class Compra
{
    public int Id { get; set; }

    public int IdProveedor { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.Now;
    public string? NumeroComprobante { get; set; }
    public decimal Total { get; set; }
    public EstadoCompra Estado { get; set; } = EstadoCompra.Completada;

    public DateTime? FechaAnulacion { get; set; }
    public int? IdUsuarioAnulacion { get; set; }

    public ICollection<DetalleCompra> Detalles { get; set; } = [];
    public ICollection<MovimientoInventario> Movimientos { get; set; } = [];
}
