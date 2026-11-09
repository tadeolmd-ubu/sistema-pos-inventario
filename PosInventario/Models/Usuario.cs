using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ContrasenaHash { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.Cajero;
    public int Activo { get; set; } = 1;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaUltimoAcceso { get; set; }

    public ICollection<Venta> Ventas { get; set; } = [];
    public ICollection<Compra> Compras { get; set; } = [];
    public ICollection<Caja> Cajas { get; set; } = [];
    public ICollection<MovimientoInventario> MovimientosInventario { get; set; } = [];
    public ICollection<MovimientoCaja> MovimientosCaja { get; set; } = [];
}
