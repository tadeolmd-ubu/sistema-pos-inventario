namespace PosInventario.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Activo { get; set; } = 1;

    public ICollection<Elemento> Elementos { get; set; } = [];
}
