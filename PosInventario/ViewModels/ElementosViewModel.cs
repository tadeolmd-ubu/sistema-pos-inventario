using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ElementosViewModel : OperacionViewModel
{
    private readonly ElementoService? _elementoService;
    private readonly CategoriaService? _categoriaService;
    private readonly List<Elemento> _cargados = [];

    public ElementosViewModel() { }

    public ElementosViewModel(
        ElementoService elementoService, CategoriaService categoriaService)
    {
        _elementoService = elementoService;
        _categoriaService = categoriaService;
    }

    public ObservableCollection<Elemento> ElementosItems { get; } = [];
    public ObservableCollection<Categoria> CategoriasItems { get; } = [];

    [ObservableProperty] private Elemento? selectedElemento;
    [ObservableProperty] private Categoria? selectedCategoria;
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string descripcion = string.Empty;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_elementoService is null || _categoriaService is null) return;

        _cargados.Clear();
        _cargados.AddRange(await _elementoService.ObtenerTodosAsync());

        ReemplazarItems(CategoriasItems, await _categoriaService.ObtenerActivasAsync());
        Filtrar();
    });

    partial void OnBusquedaChanged(string value) => Filtrar();

    partial void OnSelectedElementoChanged(Elemento? value)
    {
        Nombre = value?.Nombre ?? string.Empty;
        Descripcion = value?.Descripcion ?? string.Empty;
        SelectedCategoria = value is null
            ? null
            : CategoriasItems.FirstOrDefault(c => c.Id == value.IdCategoria);
    }

    [RelayCommand]
    private void LimpiarFormulario()
    {
        SelectedElemento = null;
        Nombre = string.Empty;
        Descripcion = string.Empty;
        SelectedCategoria = null;
    }

    [RelayCommand]
    private Task GuardarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_elementoService is null) return;

        bool correcta;
        string mensaje;

        if (SelectedElemento is null)
        {
            var resultado = await _elementoService.AgregarAsync(
                Nombre, Descripcion, SelectedCategoria?.Id ?? 0);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }
        else
        {
            var resultado = await _elementoService.ActualizarAsync(
                SelectedElemento.Id, Nombre, Descripcion, SelectedCategoria?.Id ?? 0);

            correcta = resultado.Correcta;
            mensaje = resultado.Mensaje;
        }

        MensajeOperacion = Notificar(mensaje, correcta);

        if (correcta)
            await CargarAsync();
    });

    [RelayCommand]
    private Task ArchivarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_elementoService is null || SelectedElemento is null) return;

        var resultado =
            await _elementoService.ArchivarAsync(SelectedElemento.Id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (resultado.Correcta)
        {
            LimpiarFormulario();
            await CargarAsync();
        }
    });

    private void Filtrar()
    {
        var texto = (Busqueda ?? string.Empty).Trim();

        var filtrados = string.IsNullOrWhiteSpace(texto)
            ? _cargados
            : _cargados.Where(e =>
                e.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                (e.Descripcion?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false));

        var seleccionado = SelectedElemento?.Id;

        ReemplazarItems(ElementosItems, filtrados);
        SelectedElemento = ElementosItems.FirstOrDefault(e => e.Id == seleccionado);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
