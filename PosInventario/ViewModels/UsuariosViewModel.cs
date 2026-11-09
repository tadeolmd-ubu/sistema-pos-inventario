using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class UsuariosViewModel : OperacionViewModel
{
    private readonly UsuarioService? _usuarioService;
    private readonly SesionActual? _sesion;
    private readonly List<Usuario> _cargados = [];

    public UsuariosViewModel() { }

    public UsuariosViewModel(UsuarioService usuarioService, SesionActual sesion)
    {
        _usuarioService = usuarioService;
        _sesion = sesion;
    }

    public ObservableCollection<Usuario> UsuariosItems { get; } = [];
    public IReadOnlyList<RolUsuario> Roles { get; } = Enum.GetValues<RolUsuario>();

    [ObservableProperty] private Usuario? selectedUsuario;
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private string nombreUsuario = string.Empty;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private RolUsuario rol = RolUsuario.Cajero;
    [ObservableProperty] private string contrasena = string.Empty;
    [ObservableProperty] private string confirmarContrasena = string.Empty;

    public bool EsModoCreacion => SelectedUsuario is null;

    public bool PuedeActivar =>
        SelectedUsuario is not null && SelectedUsuario.Activo == 0;

    public bool PuedeDesactivar =>
        SelectedUsuario is not null &&
        SelectedUsuario.Activo == 1 &&
        _sesion is not null &&
        SelectedUsuario.Id != _sesion.IdUsuario;

    /// <summary>
    /// Lo escucha la vista para abrir el modal de restablecimiento.
    /// </summary>
    public event Action<RestablecerContrasenaViewModel>? RestablecerContrasenaSolicitado;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_usuarioService is null) return;

        _cargados.Clear();
        _cargados.AddRange(await _usuarioService.ObtenerTodosAsync());
        Filtrar();
    });

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnSelectedUsuarioChanged(Usuario? value)
    {
        NombreUsuario = value?.NombreUsuario ?? string.Empty;
        Nombre = value?.Nombre ?? string.Empty;
        Rol = value?.Rol ?? RolUsuario.Cajero;
        Contrasena = string.Empty;
        ConfirmarContrasena = string.Empty;

        OnPropertyChanged(nameof(EsModoCreacion));
        OnPropertyChanged(nameof(PuedeActivar));
        OnPropertyChanged(nameof(PuedeDesactivar));
    }

    [RelayCommand]
    private void LimpiarFormulario()
    {
        SelectedUsuario = null;
        Contrasena = string.Empty;
        ConfirmarContrasena = string.Empty;
    }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_usuarioService is null) return;

        if (EsModoCreacion && Contrasena != ConfirmarContrasena)
        {
            MensajeOperacion = Notificar("Las contraseñas no coinciden.");
            return;
        }

        bool correcta;
        string mensaje;

        if (EsModoCreacion)
        {
            var resultado = await _usuarioService.AgregarAsync(
                NombreUsuario, Nombre, Contrasena, Rol);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }
        else
        {
            var resultado = await _usuarioService.ActualizarAsync(
                SelectedUsuario!.Id, NombreUsuario, Nombre, Rol);

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
        if (_usuarioService is null || SelectedUsuario is null) return;

        var resultado = await _usuarioService.ActivarAsync(SelectedUsuario.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
            await CargarAsync();
    });

    [RelayCommand]
    private Task DesactivarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_usuarioService is null || SelectedUsuario is null) return;

        var resultado = await _usuarioService.DesactivarAsync(SelectedUsuario.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
            await CargarAsync();
    });

    [RelayCommand]
    private void RestablecerContrasena()
    {
        if (_usuarioService is null || SelectedUsuario is null) return;

        RestablecerContrasenaSolicitado?.Invoke(
            new RestablecerContrasenaViewModel(_usuarioService, SelectedUsuario));
    }

    private void Filtrar()
    {
        var texto = (Busqueda ?? string.Empty).Trim();

        var filtrados = string.IsNullOrWhiteSpace(texto)
            ? _cargados
            : _cargados.Where(u =>
                u.NombreUsuario.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                u.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase));

        var seleccionado = SelectedUsuario?.Id;

        ReemplazarItems(UsuariosItems, filtrados);
        SelectedUsuario = UsuariosItems.FirstOrDefault(u => u.Id == seleccionado);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
