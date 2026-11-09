using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PosInventario.ViewModels;

public partial class MainViewModel : OperacionViewModel
{
    private readonly InicioViewModel _inicioViewModel;

    public MainViewModel(InicioViewModel inicioViewModel)
    {
        _inicioViewModel = inicioViewModel;
        _inicioViewModel.MensajeSolicitado += SolicitarMensaje;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        TextoVersionMenu = $"v{version?.ToString(3) ?? "1.0.0"} · Sistema de inventario";

        VistaActual = _inicioViewModel;
        TituloVistaActual = "Inicio";
    }

    [ObservableProperty] private object vistaActual = null!;
    [ObservableProperty] private string tituloVistaActual = string.Empty;
    [ObservableProperty] private string textoVersionMenu = string.Empty;

    public Task InicializarAsync() => IrAInicioAsync();

    [RelayCommand]
    private async Task IrAInicioAsync()
    {
        VistaActual = _inicioViewModel;
        TituloVistaActual = "Inicio";
        await _inicioViewModel.CargarAsync();
    }
}
