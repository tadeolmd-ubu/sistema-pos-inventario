using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class LoginViewModel : OperacionViewModel
{
    private readonly UsuarioService? _usuarioService;

    public LoginViewModel() { }

    public LoginViewModel(UsuarioService usuarioService)
        => _usuarioService = usuarioService;

    [ObservableProperty] private string nombreUsuario = string.Empty;
    [ObservableProperty] private string contrasena = string.Empty;
    [ObservableProperty] private Usuario? usuarioAutenticado;

    /// <summary>
    /// Lo escucha la ventana para cerrarse devolviendo el usuario validado.
    /// </summary>
    public event Action<Usuario>? AccesoConcedido;

    [RelayCommand]
    private Task IngresarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_usuarioService is null) return;

        var resultado = await _usuarioService.ValidarCredencialesAsync(
            NombreUsuario, Contrasena);

        if (!resultado.Correcta || resultado.Valor is null)
        {
            MensajeOperacion = resultado.Mensaje;
            return;
        }

        UsuarioAutenticado = resultado.Valor;
        AccesoConcedido?.Invoke(resultado.Valor);
    });
}
