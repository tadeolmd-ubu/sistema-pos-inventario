using System.Globalization;
using PosInventario.Models.Enums;

namespace PosInventario.Models;

public class Abono
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public int Id { get; set; }

    public int IdCliente { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public int IdVenta { get; set; }
    public Venta Venta { get; set; } = null!;

    public decimal Monto { get; set; }
    public MetodoPago Metodo { get; set; } = MetodoPago.Efectivo;
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Ticket => $"V-{IdVenta:000000}";

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string MontoTexto => Monto.ToString("0.00", Cultura);

    public string MetodoTexto => Metodo.ToString();

    public string ReferenciaTexto =>
        string.IsNullOrWhiteSpace(Referencia) ? "—" : Referencia.Trim();
}
