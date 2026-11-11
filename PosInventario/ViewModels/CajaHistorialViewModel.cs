using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Detalle de una caja cerrada: montos congelados al cierre y ventas netas
/// de devoluciones. Permite devolver ventas de la caja al vendedor original
/// o a un administrador.
/// </summary>
public partial class CajaHistorialViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly VentaService? _ventaService;
    private readonly SesionActual? _sesion;
    private readonly Caja _caja;

    public CajaHistorialViewModel(
        Caja caja, IReadOnlyList<Venta> ventas,
        VentaService ventaService, SesionActual sesion)
    {
        _caja = caja;
        _ventaService = ventaService;
        _sesion = sesion;

        ReemplazarVentas(ventas);
    }

    public ObservableCollection<VentaHistorialItem> VentasCompletadas { get; } = [];

    public ObservableCollection<VentaHistorialItem> VentasCanceladas { get; } = [];

    public Caja Caja => _caja;

    public string Numero => $"Caja #{_caja.Id}";

    public string UsuarioApertura => _caja.UsuarioApertura?.Nombre ?? "—";

    public string FechaApertura =>
        _caja.FechaApertura.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string FechaCierre =>
        _caja.FechaCierre?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—";

    public string MontoInicialTexto =>
        _caja.MontoInicial.ToString("0.00", Cultura);

    public string MontoEsperadoTexto =>
        (_caja.MontoEsperado ?? 0m).ToString("0.00", Cultura);

    public string MontoFinalTexto =>
        (_caja.MontoFinal ?? 0m).ToString("0.00", Cultura);

    public string DiferenciaTexto =>
        (_caja.Diferencia ?? 0m).ToString("0.00", Cultura);

    public string Observaciones =>
        string.IsNullOrWhiteSpace(_caja.Observaciones) ? "—" : _caja.Observaciones;

    public int CantidadVentas => VentasCompletadas.Count;

    public bool HayVentasCanceladas => VentasCanceladas.Count > 0;

    public decimal TotalEfectivo => VentasCompletadas
        .Where(i => i.Venta.MetodoPago == MetodoPago.Efectivo)
        .Sum(i => i.Venta.TotalNeto);

    public decimal TotalTarjeta => VentasCompletadas
        .Where(i => i.Venta.MetodoPago == MetodoPago.Tarjeta)
        .Sum(i => i.Venta.TotalNeto);

    public decimal TotalTransferencia => VentasCompletadas
        .Where(i => i.Venta.MetodoPago == MetodoPago.Transferencia)
        .Sum(i => i.Venta.TotalNeto);

    public string TotalEfectivoTexto => TotalEfectivo.ToString("0.00", Cultura);

    public string TotalTarjetaTexto => TotalTarjeta.ToString("0.00", Cultura);

    public string TotalTransferenciaTexto =>
        TotalTransferencia.ToString("0.00", Cultura);

    public string TotalVentasTexto =>
        VentasCompletadas.Sum(i => i.Venta.TotalNeto).ToString("0.00", Cultura);

    /// <summary>
    /// Crea el ViewModel de la ventana de devolución de una venta de esta caja.
    /// </summary>
    public DevolverVentaViewModel? CrearDevolucionViewModel(Venta venta)
    {
        if (_ventaService is null) return null;

        return new DevolverVentaViewModel(_ventaService, venta);
    }

    /// <summary>
    /// Vuelve a leer las ventas de la caja; la ventana lo llama después de
    /// registrar una devolución.
    /// </summary>
    [RelayCommand]
    public Task RecargarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_ventaService is null) return;

        var ventas = await _ventaService.ObtenerPorCajaAsync(_caja.Id);

        ReemplazarVentas(ventas);
        NotificarResumen();
    });

    private void ReemplazarVentas(IReadOnlyList<Venta> ventas)
    {
        VentasCompletadas.Clear();
        VentasCanceladas.Clear();

        foreach (var venta in ventas)
        {
            var item = new VentaHistorialItem(venta, _sesion);

            if (venta.Estado == EstadoVenta.Completada)
                VentasCompletadas.Add(item);
            else
                VentasCanceladas.Add(item);
        }
    }

    private void NotificarResumen()
    {
        OnPropertyChanged(nameof(CantidadVentas));
        OnPropertyChanged(nameof(HayVentasCanceladas));
        OnPropertyChanged(nameof(TotalEfectivo));
        OnPropertyChanged(nameof(TotalTarjeta));
        OnPropertyChanged(nameof(TotalTransferencia));
        OnPropertyChanged(nameof(TotalEfectivoTexto));
        OnPropertyChanged(nameof(TotalTarjetaTexto));
        OnPropertyChanged(nameof(TotalTransferenciaTexto));
        OnPropertyChanged(nameof(TotalVentasTexto));
    }
}
