using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class RestablecerContrasenaViewModel : OperacionViewModel
{
    private readonly UsuarioService? _usuarioService;
    private readonly Usuario? _usuario;

    public RestablecerContrasenaViewModel() { }

    public RestablecerContrasenaViewModel(UsuarioService usuarioService, Usuario usuario)
    {
        _usuarioService = usuarioService;
        _usuario = usuario;
        Titulo = $"Restablecer contraseña de {_usuario.NombreUsuario}";
    }

    [ObservableProperty] private string titulo = string.Empty;
    [ObservableProperty] private string nuevaContrasena = string.Empty;
    [ObservableProperty] private string confirmarContrasena = string.Empty;

    /// <summary>
    /// Lo escucha la ventana para cerrarse confirmando el cambio.
    /// </summary>
    public event Action? ContrasenaRestablecida;

    [RelayCommand]
    private Task GuardarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_usuarioService is null || _usuario is null) return;

        if (NuevaContrasena != ConfirmarContrasena)
        {
            MensajeOperacion = "Las contraseñas no coinciden.";
            return;
        }

        var resultado = await _usuarioService.RestablecerContrasenaAsync(
            _usuario.Id, NuevaContrasena);

        MensajeOperacion = resultado.Mensaje;

        if (resultado.Correcta)
            ContrasenaRestablecida?.Invoke();
    });
}
