using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class MainViewModel : OperacionViewModel
{
    private readonly InicioViewModel _inicioViewModel;
    private readonly CajaViewModel _cajaViewModel;
    private readonly ProductosViewModel _productosViewModel;
    private readonly ProveedoresViewModel _proveedoresViewModel;
    private readonly InventarioViewModel _inventarioViewModel;
    private readonly ComprasViewModel _comprasViewModel;
    private readonly UsuariosViewModel _usuariosViewModel;
    private readonly ConfiguracionViewModel _configuracionViewModel;
    private readonly VersionService _versionService;
    private readonly SesionActual _sesion;

    public MainViewModel(
        InicioViewModel inicioViewModel,
        CajaViewModel cajaViewModel,
        ProductosViewModel productosViewModel,
        ProveedoresViewModel proveedoresViewModel,
        InventarioViewModel inventarioViewModel,
        ComprasViewModel comprasViewModel,
        UsuariosViewModel usuariosViewModel,
        ConfiguracionViewModel configuracionViewModel,
        VersionService versionService,
        SesionActual sesion)
    {
        _inicioViewModel = inicioViewModel;
        _cajaViewModel = cajaViewModel;
        _productosViewModel = productosViewModel;
        _proveedoresViewModel = proveedoresViewModel;
        _inventarioViewModel = inventarioViewModel;
        _comprasViewModel = comprasViewModel;
        _usuariosViewModel = usuariosViewModel;
        _configuracionViewModel = configuracionViewModel;
        _versionService = versionService;
        _sesion = sesion;

        _inicioViewModel.MensajeSolicitado += SolicitarMensaje;
        _cajaViewModel.MensajeSolicitado += SolicitarMensaje;
        _productosViewModel.MensajeSolicitado += SolicitarMensaje;
        _proveedoresViewModel.MensajeSolicitado += SolicitarMensaje;
        _inventarioViewModel.MensajeSolicitado += SolicitarMensaje;
        _comprasViewModel.MensajeSolicitado += SolicitarMensaje;
        _usuariosViewModel.MensajeSolicitado += SolicitarMensaje;
        _configuracionViewModel.MensajeSolicitado += SolicitarMensaje;

        var version = _versionService.VersionLocal;
        TextoVersionMenu = $"v{version} · Sistema de inventario";

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

    public async Task ActualizarVersionAsync()
    {
        var version = await _versionService.ObtenerVersionAsync();
        TextoVersionMenu = $"v{version} · Sistema de inventario";
    }

    private void CambiarVista(OperacionViewModel vista, string titulo)
    {
        if (ReferenceEquals(VistaActual, vista))
            return;

        if (VistaActual is OperacionViewModel anterior)
            anterior.LimpiarPantalla();

        VistaActual = vista;
        TituloVistaActual = titulo;
    }

    [RelayCommand]
    private async Task IrAInicioAsync()
    {
        CambiarVista(_inicioViewModel, "Inicio");
        await _inicioViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrACajaAsync()
    {
        CambiarVista(_cajaViewModel, "Caja");
        await _cajaViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrAProductosAsync()
    {
        if (!EsAdministrador) return;

        CambiarVista(_productosViewModel, "Productos");
        await _productosViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrAProveedoresAsync()
    {
        if (!EsAdministrador) return;

        CambiarVista(_proveedoresViewModel, "Proveedores");
        await _proveedoresViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrAInventarioAsync()
    {
        if (!EsAdministrador) return;

        CambiarVista(_inventarioViewModel, "Inventario");
        await _inventarioViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrAComprasAsync()
    {
        if (!EsAdministrador) return;

        CambiarVista(_comprasViewModel, "Compras");
        await _comprasViewModel.CargarAsync();
    }

    [RelayCommand]
    private async Task IrAUsuariosAsync()
    {
        if (!EsAdministrador) return;

        CambiarVista(_usuariosViewModel, "Usuarios");
        await _usuariosViewModel.CargarAsync();
    }

    [RelayCommand]
    private void IrAConfiguracion()
    {
        if (!EsAdministrador) return;

        CambiarVista(_configuracionViewModel, "Configuración");
    }

    [RelayCommand]
    private void CerrarSesion() => CerrarSesionSolicitado?.Invoke();
}
