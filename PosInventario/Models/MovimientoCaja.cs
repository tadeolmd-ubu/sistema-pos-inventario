using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class MovimientoCaja
{
    public int Id { get; set; }

    public int IdCaja { get; set; }
    public Caja Caja { get; set; } = null!;

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public TipoMovimientoCaja Tipo { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }

    public int? IdVenta { get; set; }
    public Venta? Venta { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;
}
