using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class CancelarVentaViewModel : OperacionViewModel
{
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

    /// <summary>
    /// Solo se puede devolver una venta completada con unidades pendientes,
    /// por el vendedor original o un administrador.
    /// </summary>
    public bool PuedeDevolver =>
        SelectedVenta is not null &&
        SelectedVenta.Estado == EstadoVenta.Completada &&
        SelectedVenta.Detalles.Any(d => d.CantidadPendiente > 0) &&
        _sesion is not null &&
        (_sesion.EsAdministrador || SelectedVenta.IdUsuario == _sesion.IdUsuario);

    /// <summary>
    /// Crea el ViewModel del checklist de devolución de la venta
    /// seleccionada.
    /// </summary>
    public DevolverVentaViewModel? CrearDevolucionViewModel()
        => _ventaService is null || SelectedVenta is null
            ? null
            : new DevolverVentaViewModel(_ventaService, SelectedVenta);

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
        => OnPropertyChanged(nameof(PuedeDevolver));

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
