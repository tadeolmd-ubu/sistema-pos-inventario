namespace PosInventario.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public int Activo { get; set; } = 1;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public ICollection<Compra> Compras { get; set; } = [];
}
