using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ProveedoresViewModel : OperacionViewModel
{
    private readonly ProveedorService? _proveedorService;
    private readonly List<Proveedor> _cargados = [];

    public ProveedoresViewModel() { }

    public ProveedoresViewModel(ProveedorService proveedorService)
        => _proveedorService = proveedorService;

    public ObservableCollection<Proveedor> ProveedoresItems { get; } = [];

    [ObservableProperty] private Proveedor? selectedProveedor;
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private bool verInactivos;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string telefono = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string direccion = string.Empty;

    public bool EsModoCreacion => SelectedProveedor is null;

    public bool PuedeActivar =>
        SelectedProveedor is not null && SelectedProveedor.Activo == 0;

    public bool PuedeDesactivar =>
        SelectedProveedor is not null && SelectedProveedor.Activo == 1;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_proveedorService is null) return;

        _cargados.Clear();
        _cargados.AddRange(VerInactivos
            ? await _proveedorService.ObtenerTodosAsync()
            : await _proveedorService.ObtenerActivosAsync());

        Filtrar();
    });

    partial void OnVerInactivosChanged(bool value) => _ = CargarAsync();

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnSelectedProveedorChanged(Proveedor? value)
    {
        Nombre = value?.Nombre ?? string.Empty;
        Telefono = value?.Telefono ?? string.Empty;
        Email = value?.Email ?? string.Empty;
        Direccion = value?.Direccion ?? string.Empty;

        OnPropertyChanged(nameof(EsModoCreacion));
        OnPropertyChanged(nameof(PuedeActivar));
        OnPropertyChanged(nameof(PuedeDesactivar));
    }

    [RelayCommand]
    private void LimpiarFormulario()
    {
        SelectedProveedor = null;
        Nombre = string.Empty;
        Telefono = string.Empty;
        Email = string.Empty;
        Direccion = string.Empty;
    }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_proveedorService is null) return;

        bool correcta;
        string mensaje;

        if (EsModoCreacion)
        {
            var resultado = await _proveedorService.AgregarAsync(
                Nombre, Telefono, Email, Direccion);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }
        else
        {
            var resultado = await _proveedorService.ActualizarAsync(
                SelectedProveedor!.Id, Nombre, Telefono, Email, Direccion);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }

        MensajeOperacion = Notificar(mensaje, correcta);

        if (correcta)
            await CargarAsync();
    });

    [RelayCommand]
    private Task ActivarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_proveedorService is null || SelectedProveedor is null) return;

        var resultado = await _proveedorService.ActivarAsync(SelectedProveedor.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
            await CargarAsync();
    });

    [RelayCommand]
    private Task DesactivarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_proveedorService is null || SelectedProveedor is null) return;

        var resultado = await _proveedorService.DesactivarAsync(SelectedProveedor.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
            await CargarAsync();
    });

    private void Filtrar()
    {
        var texto = (Busqueda ?? string.Empty).Trim();

        var filtrados = string.IsNullOrWhiteSpace(texto)
            ? _cargados
            : _cargados.Where(p =>
                p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Telefono.Contains(texto, StringComparison.OrdinalIgnoreCase));

        var seleccionado = SelectedProveedor?.Id;

        ReemplazarItems(ProveedoresItems, filtrados);
        SelectedProveedor = ProveedoresItems.FirstOrDefault(p => p.Id == seleccionado);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
