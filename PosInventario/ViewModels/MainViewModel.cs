using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class MainViewModel : OperacionViewModel
{
    private readonly InicioViewModel _inicioViewModel;
    private readonly ProductosViewModel _productosViewModel;
    private readonly UsuariosViewModel _usuariosViewModel;
    private readonly SesionActual _sesion;

    public MainViewModel(
        InicioViewModel inicioViewModel,
        ProductosViewModel productosViewModel,
        UsuariosViewModel usuariosViewModel,
        SesionActual sesion)
    {
        _inicioViewModel = inicioViewModel;
        _productosViewModel = productosViewModel;
        _usuariosViewModel = usuariosViewModel;
        _sesion = sesion;

        _inicioViewModel.MensajeSolicitado += SolicitarMensaje;
        _productosViewModel.MensajeSolicitado += SolicitarMensaje;
        _usuariosViewModel.MensajeSolicitado += SolicitarMensaje;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        TextoVersionMenu = $"v{version?.ToString(3) ?? "1.0.0"} · Sistema de inventario";

        VistaActual = _inicioViewModel;
        TituloVistaActual = "Inicio";
    }

    [ObservableProperty] private object vistaActual = null!;
    [ObservableProperty] private string tituloVistaActual = string.Empty;
    [ObservableProperty] private string textoVersionMenu = string.Empty;
    [ObservableProperty] private bool esAdministrador;
    [ObservableProperty] private string nombreUsuarioActual = string.Empty;
    [ObservableProperty] private string rolUsuarioActual = string.Empty;

    /// <summary>
    /// Lo escucha App para cerrar la sesión y volver al login.
    /// </summary>
    public event Action? CerrarSesionSolicitado;

    public Task InicializarAsync() => IrAInicioAsync();

    public void AplicarSesion()
    {
        NombreUsuarioActual = _sesion.Usuario?.Nombre ?? string.Empty;
        RolUsuarioActual = _sesion.Usuario?.Rol.ToString() ?? string.Empty;
        EsAdministrador = _sesion.EsAdministrador;
    }

    [RelayCommand]
    private async Task IrAInicioAsync()
    {
        VistaActual = _inicioViewModel;
        TituloVistaActual = "Inicio";
        await _inicioViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrAProductosAsync()
    {
        if (!EsAdministrador) return;

        VistaActual = _productosViewModel;
        TituloVistaActual = "Productos";
        await _productosViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrAUsuariosAsync()
    {
        if (!EsAdministrador) return;

        VistaActual = _usuariosViewModel;
        TituloVistaActual = "Usuarios";
        await _usuariosViewModel.CargarAsync();
    }

    [RelayCommand]
    private void CerrarSesion() => CerrarSesionSolicitado?.Invoke();
}
