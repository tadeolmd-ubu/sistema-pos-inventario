using System.Globalization;
using PosInventario.Models;
using PosInventario.Models.Enums;

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

    public int CantidadVentas => VentasCompletadas.Count;

    public IReadOnlyList<Venta> VentasCompletadas =>
        Ventas.Where(v => v.Estado == EstadoVenta.Completada).ToList();

    public IReadOnlyList<Venta> VentasCanceladas =>
        Ventas.Where(v => v.Estado == EstadoVenta.Anulada).ToList();

    public bool HayVentasCanceladas =>
        Ventas.Any(v => v.Estado == EstadoVenta.Anulada);

    public decimal TotalEfectivo =>
        VentasCompletadas
            .Where(v => v.MetodoPago == MetodoPago.Efectivo)
            .Sum(v => v.Total);

    public decimal TotalTarjeta =>
        VentasCompletadas
            .Where(v => v.MetodoPago == MetodoPago.Tarjeta)
            .Sum(v => v.Total);

    public decimal TotalTransferencia =>
        VentasCompletadas
            .Where(v => v.MetodoPago == MetodoPago.Transferencia)
            .Sum(v => v.Total);

    public string TotalEfectivoTexto => TotalEfectivo.ToString("0.00", Cultura);

    public string TotalTarjetaTexto => TotalTarjeta.ToString("0.00", Cultura);

    public string TotalTransferenciaTexto =>
        TotalTransferencia.ToString("0.00", Cultura);

    public string TotalVentasTexto =>
        VentasCompletadas.Sum(v => v.Total).ToString("0.00", Cultura);
}
