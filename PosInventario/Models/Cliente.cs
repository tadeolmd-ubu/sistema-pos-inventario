using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace PosInventario.Models;

public class Cliente
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public decimal LimiteCredito { get; set; }
    public int Activo { get; set; } = 1;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaModificacion { get; set; } = DateTime.Now;

    public ICollection<Venta> Ventas { get; set; } = [];
    public ICollection<Abono> Abonos { get; set; } = [];

    /// <summary>
    /// Saldo pendiente calculado por ClienteService; no se guarda en la base.
    /// </summary>
    [NotMapped]
    public decimal SaldoActual { get; set; }

    [NotMapped]
    public decimal CreditoDisponible =>
        Math.Max(0m, LimiteCredito - SaldoActual);

    [NotMapped]
    public string SaldoTexto => SaldoActual.ToString("0.00", Cultura);

    [NotMapped]
    public string LimiteCreditoTexto =>
        LimiteCredito.ToString("0.00", Cultura);

    [NotMapped]
    public string CreditoDisponibleTexto =>
        CreditoDisponible.ToString("0.00", Cultura);
}
