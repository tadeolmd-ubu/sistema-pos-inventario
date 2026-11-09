using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class InicioViewModel : OperacionViewModel
{
    private readonly ElementoService? _elementoService;
    private readonly CategoriaService? _categoriaService;

    public InicioViewModel() { }

    public InicioViewModel(
        ElementoService elementoService, CategoriaService categoriaService)
    {
        _elementoService = elementoService;
        _categoriaService = categoriaService;
    }

    [ObservableProperty] private int totalElementos;
    [ObservableProperty] private int totalCategorias;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_elementoService is null || _categoriaService is null) return;

        TotalElementos = (await _elementoService.ObtenerTodosAsync()).Count;
        TotalCategorias = (await _categoriaService.ObtenerTodosAsync()).Count;
    });
}
