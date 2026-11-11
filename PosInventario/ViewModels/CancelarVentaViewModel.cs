using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class CancelarVentaViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly VentaService? _ventaService;
    private readonly SesionActual? _sesion;
    private readonly int _idCaja;

    public CancelarVentaViewModel() { }

    public CancelarVentaViewModel(
        VentaService ventaService, SesionActual sesion, int idCaja)
    {
        _ventaService = ventaService;
        _sesion = sesion;
        _idCaja = idCaja;
    }

    public ObservableCollection<Venta> VentasItems { get; } = [];

    [ObservableProperty] private Venta? selectedVenta;

    public bool PuedeCancelar =>
        SelectedVenta is not null &&
        SelectedVenta.Estado == EstadoVenta.Completada &&
        _sesion is not null &&
        (_sesion.EsAdministrador || SelectedVenta.IdUsuario == _sesion.IdUsuario);

    /// <summary>
    /// Unidades pendientes de la venta seleccionada, una por línea, para
    /// mostrarlas en la confirmación antes de cancelar.
    /// </summary>
    public string ProductosSeleccionadaTexto =>
        SelectedVenta is null
            ? ""
            : string.Join("\n", SelectedVenta.Detalles
                .Where(d => d.CantidadPendiente > 0)
                .Select(d => $"{d.CantidadPendiente} × {d.Producto?.Nombre ?? "Producto"}"));

    /// <summary>
    /// Cómo saldría el reembolso de cancelar la venta seleccionada.
    /// </summary>
    public string ReembolsoSeleccionadaTexto
    {
        get
        {
            if (SelectedVenta is null)
                return "";

            var monto = SelectedVenta.TotalNeto.ToString("0.00", Cultura);

            return SelectedVenta.MetodoPago == MetodoPago.Efectivo
                ? $"Se reembolsarán {monto} en efectivo de la caja."
                : $"El reembolso de {monto} se hace fuera de la app; aquí solo se regresa el stock.";
        }
    }

    /// <summary>
    /// Lo escucha la ventana para cerrarse después de cancelar la venta.
    /// </summary>
    public event Action? VentaCancelada;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(CargarDatosAsync);

    private async Task CargarDatosAsync()
    {
        if (_ventaService is null) return;

        var ventas = await _ventaService.ObtenerPorCajaAsync(_idCaja);
        ReemplazarItems(VentasItems, ventas);
        SelectedVenta = null;
    }

    partial void OnSelectedVentaChanged(Venta? value)
    {
        OnPropertyChanged(nameof(PuedeCancelar));
        OnPropertyChanged(nameof(ProductosSeleccionadaTexto));
        OnPropertyChanged(nameof(ReembolsoSeleccionadaTexto));
    }

    public Task CancelarSeleccionadaAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_ventaService is null || SelectedVenta is null) return;

        var resultado = await _ventaService.AnularAsync(SelectedVenta.Id);

        MensajeOperacion = resultado.Mensaje;

        if (!resultado.Correcta) return;

        await CargarDatosAsync();
        VentaCancelada?.Invoke();
    });

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
