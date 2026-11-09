using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class Caja
{
    public int Id { get; set; }

    public int IdUsuarioApertura { get; set; }
    public Usuario UsuarioApertura { get; set; } = null!;

    public DateTime FechaApertura { get; set; } = DateTime.Now;
    public decimal MontoInicial { get; set; }

    public int? IdUsuarioCierre { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? MontoEsperado { get; set; }
    public decimal? MontoFinal { get; set; }
    public decimal? Diferencia { get; set; }

    public EstadoCaja Estado { get; set; } = EstadoCaja.Abierta;
    public string? Observaciones { get; set; }

    public ICollection<Venta> Ventas { get; set; } = [];
    public ICollection<MovimientoCaja> Movimientos { get; set; } = [];
}
