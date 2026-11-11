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

    public bool PuedeCancelar =>
        SelectedVenta is not null &&
        SelectedVenta.Estado == EstadoVenta.Completada &&
        _sesion is not null &&
        (_sesion.EsAdministrador || SelectedVenta.IdUsuario == _sesion.IdUsuario);

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
        => OnPropertyChanged(nameof(PuedeCancelar));

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
