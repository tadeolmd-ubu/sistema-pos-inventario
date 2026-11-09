using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PosInventario.ViewModels;

public partial class MainViewModel : OperacionViewModel
{
    private readonly InicioViewModel _inicioViewModel;
    private readonly ElementosViewModel _elementosViewModel;

    public MainViewModel(
        InicioViewModel inicioViewModel,
        ElementosViewModel elementosViewModel)
    {
        _inicioViewModel = inicioViewModel;
        _elementosViewModel = elementosViewModel;

        _inicioViewModel.MensajeSolicitado += SolicitarMensaje;
        _elementosViewModel.MensajeSolicitado += SolicitarMensaje;

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

    [RelayCommand]
    private async Task IrAElementosAsync()
    {
        VistaActual = _elementosViewModel;
        TituloVistaActual = "Elementos";
        await _elementosViewModel.CargarAsync();
    }
}
