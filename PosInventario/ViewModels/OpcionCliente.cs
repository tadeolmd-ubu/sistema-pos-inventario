using System.Globalization;

namespace PosInventario.ViewModels;

/// <summary>
/// Opción del selector de cliente. Id nulo representa la venta a público.
/// </summary>
public sealed record OpcionCliente(
    int? Id,
    string Nombre,
    decimal LimiteCredito = 0m,
    decimal Saldo = 0m)
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public decimal Disponible =>
        Math.Max(0m, LimiteCredito - Saldo);

    public string DisponibleTexto =>
        Disponible.ToString("0.00", Cultura);

    public string DetalleCredito =>
        Id is null
            ? "Sin cliente asignado"
            : $"Límite {LimiteCredito.ToString("0.00", Cultura)} · " +
              $"Saldo {Saldo.ToString("0.00", Cultura)}";
}
