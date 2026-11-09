namespace PosInventario.Models;

public class Elemento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Activo { get; set; } = 1;
    public DateOnly? FechaArchivado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaModificacion { get; set; } = DateTime.Now;

    public int IdCategoria { get; set; }
    public Categoria Categoria { get; set; } = null!;
}
