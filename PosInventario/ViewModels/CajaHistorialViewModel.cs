using System.Globalization;
using PosInventario.Models;

namespace PosInventario.ViewModels;

public class CajaHistorialViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public CajaHistorialViewModel(Caja caja, IReadOnlyList<Venta> ventas)
    {
        Caja = caja;
        Ventas = ventas;
    }

    public Caja Caja { get; }

    public IReadOnlyList<Venta> Ventas { get; }

    public string Numero => $"Caja #{Caja.Id}";

    public string UsuarioApertura => Caja.UsuarioApertura?.Nombre ?? "—";

    public string FechaApertura =>
        Caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string FechaCierre => Caja.FechaCierre?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—";

    public string MontoInicialTexto =>
        Caja.MontoInicial.ToString("0.00", Cultura);

    public string MontoEsperadoTexto =>
        (Caja.MontoEsperado ?? 0m).ToString("0.00", Cultura);

    public string MontoFinalTexto =>
        (Caja.MontoFinal ?? 0m).ToString("0.00", Cultura);

    public string DiferenciaTexto =>
        (Caja.Diferencia ?? 0m).ToString("0.00", Cultura);

    public string Observaciones =>
        string.IsNullOrWhiteSpace(Caja.Observaciones) ? "—" : Caja.Observaciones;

    public int CantidadVentas => Ventas.Count;

    public string TotalVentasTexto =>
        Ventas.Sum(v => v.Total).ToString("0.00", Cultura);
}
