using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

/// <summary>
/// Checklist de devolución de una venta: marca producto por producto (o
/// unidad por unidad cuando hay varias iguales) y muestra el reembolso
/// estimado antes de confirmar.
/// </summary>
public partial class DevolverVentaViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly VentaService? _ventaService;
    private readonly Venta? _venta;
    private bool _actualizandoSeleccionGlobal;

    public DevolverVentaViewModel() { }

    public DevolverVentaViewModel(VentaService ventaService, Venta venta)
    {
        _ventaService = ventaService;
        _venta = venta;

        foreach (var detalle in venta.Detalles.Where(d => d.CantidadPendiente > 0))
            Items.Add(new ProductoDevolucionItem(detalle, NotificarCambio));
    }

    public ObservableCollection<ProductoDevolucionItem> Items { get; } = [];

    [ObservableProperty] private bool seleccionarTodos;
    [ObservableProperty] private string reembolsoTexto = "0.00";
    [ObservableProperty] private string unidadesTexto = "0";

    public string Titulo =>
        _venta is null ? "Devolver venta" : $"Devolver venta V-{_venta.Id:000000}";

    public bool HayPendientes => Items.Count > 0;

    public bool SinPendientes => !HayPendientes;

    public string SinPendientesTexto =>
        "Esta venta ya no tiene unidades pendientes de devolver.";

    /// <summary>
    /// Aclara por dónde sale el reembolso según el método de pago original.
    /// </summary>
    public string NotaReembolsoTexto =>
        _venta?.MetodoPago == MetodoPago.Efectivo
            ? "El reembolso en efectivo se descuenta de la caja abierta."
            : "El reembolso de este método de pago se hace fuera de la app; aquí solo se regresa el stock.";

    public bool PuedeConfirmar => ConstruirLineas().Count > 0;

    /// <summary>
    /// Lo escucha la ventana para cerrarse cuando la devolución se registra.
    /// </summary>
    public event Action? DevolucionRegistrada;

    partial void OnSeleccionarTodosChanged(bool value)
    {
        if (_actualizandoSeleccionGlobal) return;

        _actualizandoSeleccionGlobal = true;

        foreach (var item in Items)
            item.DefinirSeleccion(value, expandir: value);

        _actualizandoSeleccionGlobal = false;

        NotificarCambio();
    }

    public Task ConfirmarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_ventaService is null || _venta is null) return;

        var lineas = ConstruirLineas();

        if (lineas.Count == 0) return;

        var resultado = await _ventaService.DevolverAsync(_venta.Id, lineas);

        MensajeOperacion = resultado.Mensaje;

        if (!resultado.Correcta) return;

        DevolucionRegistrada?.Invoke();
    });

    private List<LineaDevolucion> ConstruirLineas()
        => Items
            .Where(i => i.CantidadSeleccionada > 0)
            .Select(i => new LineaDevolucion(i.IdDetalleVenta, i.CantidadSeleccionada))
            .ToList();

    private void NotificarCambio()
    {
        var lineas = ConstruirLineas();

        UnidadesTexto = lineas.Sum(l => l.Cantidad).ToString();
        ReembolsoTexto = _ventaService is not null && _venta is not null
            ? _ventaService.CalcularReembolso(_venta, lineas).ToString("0.00", Cultura)
            : 0m.ToString("0.00", Cultura);

        _actualizandoSeleccionGlobal = true;
        SeleccionarTodos = Items.Count > 0 && Items.All(i => i.TodasSeleccionadas);
        _actualizandoSeleccionGlobal = false;

        OnPropertyChanged(nameof(PuedeConfirmar));
    }
}
