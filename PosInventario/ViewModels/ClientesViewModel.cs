using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ClientesViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly ClienteService? _clienteService;
    private readonly List<Cliente> _cargados = [];
    private bool _limpiando;

    public ClientesViewModel() { }

    public ClientesViewModel(ClienteService clienteService)
        => _clienteService = clienteService;

    public ObservableCollection<Cliente> ClientesItems { get; } = [];
    public ObservableCollection<VentaCreditoResumen> VentasPendientes { get; } = [];
    public ObservableCollection<Abono> AbonosItems { get; } = [];

    [ObservableProperty] private Cliente? selectedCliente;
    [ObservableProperty] private VentaCreditoResumen? selectedVentaPendiente;
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private bool verInactivos;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string telefono = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string direccion = string.Empty;
    [ObservableProperty] private string limiteCreditoTexto = "0.00";

    public bool EsModoCreacion => SelectedCliente is null;

    public bool PuedeActivar =>
        SelectedCliente is not null && SelectedCliente.Activo == 0;

    public bool PuedeDesactivar =>
        SelectedCliente is not null && SelectedCliente.Activo == 1;

    public bool HayCliente => SelectedCliente is not null;

    public bool SinVentasPendientes => VentasPendientes.Count == 0;

    public bool PuedeAbonar =>
        SelectedCliente is not null &&
        SelectedVentaPendiente is not null &&
        SelectedVentaPendiente.Pendiente > 0;

    public string SaldoActualTexto =>
        (SelectedCliente?.SaldoActual ?? 0m).ToString("0.00", Cultura);

    public string LimiteCreditoActualTexto =>
        (SelectedCliente?.LimiteCredito ?? 0m).ToString("0.00", Cultura);

    public string CreditoDisponibleTexto =>
        (SelectedCliente?.CreditoDisponible ?? 0m).ToString("0.00", Cultura);

    public event Action<AbonoViewModel>? AbonoSolicitado;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(CargarDatosAsync);

    private async Task CargarDatosAsync()
    {
        if (_clienteService is null) return;

        var idSeleccionado = SelectedCliente?.Id;

        _cargados.Clear();
        _cargados.AddRange(VerInactivos
            ? await _clienteService.ObtenerTodosAsync()
            : await _clienteService.ObtenerActivosAsync());

        Filtrar();

        _limpiando = true;
        SelectedCliente = ClientesItems
            .FirstOrDefault(c => c.Id == idSeleccionado);
        _limpiando = false;

        await CargarCuentaAsync(SelectedCliente?.Id);
    }

    public async Task RecargarAsync()
    {
        var id = SelectedCliente?.Id;
        await CargarDatosAsync();

        _limpiando = true;
        SelectedCliente = ClientesItems.FirstOrDefault(c => c.Id == id);
        _limpiando = false;

        await CargarCuentaAsync(SelectedCliente?.Id);
    }

    private async Task CargarCuentaAsync(int? idCliente)
    {
        VentasPendientes.Clear();
        AbonosItems.Clear();
        SelectedVentaPendiente = null;

        if (_clienteService is null || idCliente is null)
        {
            NotificarCuenta();
            return;
        }

        var cuenta = await _clienteService.ObtenerCuentaAsync(idCliente.Value);

        if (cuenta is null)
        {
            NotificarCuenta();
            return;
        }

        if (SelectedCliente is not null)
            SelectedCliente.SaldoActual = cuenta.Saldo;

        ReemplazarItems(VentasPendientes, cuenta.Ventas);
        ReemplazarItems(AbonosItems, cuenta.Abonos);

        SelectedVentaPendiente = VentasPendientes
            .FirstOrDefault(v => v.Pendiente > 0);

        NotificarCuenta();
    }

    partial void OnVerInactivosChanged(bool value)
    {
        if (!_limpiando)
            _ = CargarAsync();
    }

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnSelectedClienteChanged(Cliente? value)
    {
        Nombre = value?.Nombre ?? string.Empty;
        Telefono = value?.Telefono ?? string.Empty;
        Email = value?.Email ?? string.Empty;
        Direccion = value?.Direccion ?? string.Empty;
        LimiteCreditoTexto = (value?.LimiteCredito ?? 0m)
            .ToString("0.00", Cultura);

        OnPropertyChanged(nameof(EsModoCreacion));
        OnPropertyChanged(nameof(PuedeActivar));
        OnPropertyChanged(nameof(PuedeDesactivar));
        OnPropertyChanged(nameof(HayCliente));
        NotificarCuenta();

        if (!_limpiando)
            _ = CargarCuentaAsync(value?.Id);
    }

    partial void OnSelectedVentaPendienteChanged(VentaCreditoResumen? value)
        => OnPropertyChanged(nameof(PuedeAbonar));

    [RelayCommand]
    private void LimpiarFormulario()
    {
        _limpiando = true;
        SelectedCliente = null;
        Nombre = string.Empty;
        Telefono = string.Empty;
        Email = string.Empty;
        Direccion = string.Empty;
        LimiteCreditoTexto = "0.00";
        VentasPendientes.Clear();
        AbonosItems.Clear();
        SelectedVentaPendiente = null;
        _limpiando = false;
        NotificarCuenta();
    }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_clienteService is null) return;

        if (!TryParseDecimal(LimiteCreditoTexto, out var limite) || limite < 0)
        {
            MensajeOperacion = Notificar(
                "El límite de crédito debe ser un número mayor o igual a 0.");
            return;
        }

        bool correcta;
        string mensaje;

        if (EsModoCreacion)
        {
            var resultado = await _clienteService.AgregarAsync(
                Nombre, Telefono, Email, Direccion, limite);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }
        else
        {
            var resultado = await _clienteService.ActualizarAsync(
                SelectedCliente!.Id, Nombre, Telefono, Email, Direccion, limite);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }

        MensajeOperacion = Notificar(mensaje, correcta);

        if (correcta)
        {
            await CargarDatosAsync();
            LimpiarFormulario();
        }
    });

    [RelayCommand]
    private Task ActivarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_clienteService is null || SelectedCliente is null) return;

        var resultado = await _clienteService.ActivarAsync(SelectedCliente.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
        {
            await CargarDatosAsync();
            LimpiarFormulario();
        }
    });

    [RelayCommand]
    private Task DesactivarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_clienteService is null || SelectedCliente is null) return;

        var resultado = await _clienteService.DesactivarAsync(SelectedCliente.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
        {
            await CargarDatosAsync();
            LimpiarFormulario();
        }
    });

    [RelayCommand]
    private void Abonar()
    {
        if (_clienteService is null ||
            SelectedCliente is null ||
            SelectedVentaPendiente is null)
        {
            return;
        }

        AbonoSolicitado?.Invoke(new AbonoViewModel(
            _clienteService, SelectedCliente, SelectedVentaPendiente));
    }

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        _limpiando = true;
        _cargados.Clear();
        ClientesItems.Clear();
        VentasPendientes.Clear();
        AbonosItems.Clear();
        LimpiarFormulario();
        Busqueda = string.Empty;
        VerInactivos = false;
        _limpiando = false;
    }

    private void Filtrar()
    {
        var texto = (Busqueda ?? string.Empty).Trim();

        var filtrados = string.IsNullOrWhiteSpace(texto)
            ? _cargados
            : _cargados.Where(c =>
                c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                (c.Telefono?.Contains(
                    texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.Email?.Contains(
                    texto, StringComparison.OrdinalIgnoreCase) ?? false));

        var seleccionado = SelectedCliente?.Id;

        ReemplazarItems(ClientesItems, filtrados);
        SelectedCliente = ClientesItems.FirstOrDefault(c => c.Id == seleccionado);
    }

    private void NotificarCuenta()
    {
        OnPropertyChanged(nameof(HayCliente));
        OnPropertyChanged(nameof(SaldoActualTexto));
        OnPropertyChanged(nameof(LimiteCreditoActualTexto));
        OnPropertyChanged(nameof(CreditoDisponibleTexto));
        OnPropertyChanged(nameof(SinVentasPendientes));
        OnPropertyChanged(nameof(PuedeAbonar));
    }

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(
            limpio, NumberStyles.Number, Cultura, out valor);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
