using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PosInventario.ViewModels;

/// <summary>
/// Estado reutilizable de paginación para los listados que consultan la base
/// por páginas. Cada módulo lo expone como propiedad, lo enlaza al control
/// <c>Paginador</c> y recarga sus datos cuando cambia la página o el tamaño.
/// </summary>
public partial class PaginacionViewModel : ObservableObject
{
    public static readonly IReadOnlyList<int> Tamanos = [10, 25, 50];

    private bool _suprimirCambio;

    /// <summary>
    /// Lo escucha el ViewModel del módulo para volver a consultar la página
    /// actual. No se dispara al reiniciar ni al establecer el total.
    /// </summary>
    public event Action? CambioSolicitado;

    [ObservableProperty] private int paginaActual = 1;
    [ObservableProperty] private int tamanoPagina = 10;
    [ObservableProperty] private int totalRegistros;

    public IReadOnlyList<int> TamanosDisponibles => Tamanos;

    public int TotalPaginas =>
        Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina));

    public bool HayRegistros => TotalRegistros > 0;

    public bool PuedeAnterior => PaginaActual > 1;

    public bool PuedeSiguiente => PaginaActual < TotalPaginas;

    public int Desde => TotalRegistros == 0
        ? 0
        : (PaginaActual - 1) * TamanoPagina + 1;

    public int Hasta => Math.Min(PaginaActual * TamanoPagina, TotalRegistros);

    public string Resumen => TotalRegistros == 0
        ? "Sin registros"
        : $"{Desde}-{Hasta} de {TotalRegistros}";

    public string PaginaTexto => $"Página {PaginaActual} de {TotalPaginas}";

    public int Desplazamiento => (PaginaActual - 1) * TamanoPagina;

    /// <summary>
    /// Vuelve a la primera página sin disparar una recarga. Se usa al cambiar
    /// filtros, fechas o al entrar de nuevo al módulo.
    /// </summary>
    public void Reiniciar()
    {
        _suprimirCambio = true;

        PaginaActual = 1;
        TotalRegistros = 0;

        _suprimirCambio = false;
        NotificarPropiedades();
    }

    /// <summary>
    /// Actualiza el total de filas después de una consulta y ajusta la página
    /// si el filtro dejó menos resultados de los que se estaban viendo.
    /// </summary>
    public void EstablecerTotal(int total)
    {
        TotalRegistros = Math.Max(0, total);

        if (PaginaActual > TotalPaginas)
            PaginaActual = TotalPaginas;

        NotificarPropiedades();
    }

    [RelayCommand(CanExecute = nameof(PuedeAnterior))]
    private void IrPrimera() => IrA(1);

    [RelayCommand(CanExecute = nameof(PuedeAnterior))]
    private void IrAnterior() => IrA(PaginaActual - 1);

    [RelayCommand(CanExecute = nameof(PuedeSiguiente))]
    private void IrSiguiente() => IrA(PaginaActual + 1);

    [RelayCommand(CanExecute = nameof(PuedeSiguiente))]
    private void IrUltima() => IrA(TotalPaginas);

    partial void OnPaginaActualChanged(int value) => NotificarPropiedades();

    partial void OnTamanoPaginaChanged(int value)
    {
        if (_suprimirCambio)
            return;

        if (value <= 0)
        {
            TamanoPagina = 10;
            return;
        }

        PaginaActual = 1;
        NotificarPropiedades();
        CambioSolicitado?.Invoke();
    }

    partial void OnTotalRegistrosChanged(int value) => NotificarPropiedades();

    private void IrA(int pagina)
    {
        var destino = Math.Clamp(pagina, 1, TotalPaginas);

        if (destino == PaginaActual)
            return;

        PaginaActual = destino;
        CambioSolicitado?.Invoke();
    }

    private void NotificarPropiedades()
    {
        OnPropertyChanged(nameof(TotalPaginas));
        OnPropertyChanged(nameof(HayRegistros));
        OnPropertyChanged(nameof(PuedeAnterior));
        OnPropertyChanged(nameof(PuedeSiguiente));
        OnPropertyChanged(nameof(Desde));
        OnPropertyChanged(nameof(Hasta));
        OnPropertyChanged(nameof(Resumen));
        OnPropertyChanged(nameof(PaginaTexto));
        OnPropertyChanged(nameof(Desplazamiento));

        IrPrimeraCommand.NotifyCanExecuteChanged();
        IrAnteriorCommand.NotifyCanExecuteChanged();
        IrSiguienteCommand.NotifyCanExecuteChanged();
        IrUltimaCommand.NotifyCanExecuteChanged();
    }
}
