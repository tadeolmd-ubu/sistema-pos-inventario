using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class SetupViewModel : OperacionViewModel
{
    private readonly UsuarioService? _usuarioService;

    public SetupViewModel() { }

    public SetupViewModel(UsuarioService usuarioService)
        => _usuarioService = usuarioService;

    [ObservableProperty] private string nombreUsuario = string.Empty;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string contrasena = string.Empty;
    [ObservableProperty] private string confirmarContrasena = string.Empty;

    /// <summary>
    /// Lo escucha la ventana para cerrarse confirmando la creación.
    /// </summary>
    public event Action? AdministradorCreado;

    [RelayCommand]
    private Task CrearAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_usuarioService is null) return;

        if (Contrasena != ConfirmarContrasena)
        {
            MensajeOperacion = "Las contraseñas no coinciden.";
            return;
        }

        var resultado = await _usuarioService.CrearPrimerAdministradorAsync(
            NombreUsuario, Nombre, Contrasena);

        MensajeOperacion = resultado.Mensaje;

        if (resultado.Correcta)
            AdministradorCreado?.Invoke();
    });
}
