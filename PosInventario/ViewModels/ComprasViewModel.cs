using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosInventario.Models;
using PosInventario.Models.Enums;
using PosInventario.Service;

namespace PosInventario.ViewModels;

public partial class ComprasViewModel : OperacionViewModel
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    private readonly CompraService? _compraService;
    private readonly ProveedorService? _proveedorService;
    private readonly ProductoService? _productoService;

    public ComprasViewModel()
    {
        Paginacion.CambioSolicitado += CargarPaginaActual;
    }

    public ComprasViewModel(
        CompraService compraService,
        ProveedorService proveedorService,
        ProductoService productoService)
        : this()
    {
        _compraService = compraService;
        _proveedorService = proveedorService;
        _productoService = productoService;
    }

    public ObservableCollection<Compra> ComprasItems { get; } = [];

    public PaginacionViewModel Paginacion { get; } = new();
    public ObservableCollection<Proveedor> ProveedoresItems { get; } = [];
    public ObservableCollection<Producto> ProductosItems { get; } = [];
    public ObservableCollection<LineaCompraItem> LineasItems { get; } = [];

    [ObservableProperty] private Compra? selectedCompra;
    [ObservableProperty] private Proveedor? selectedProveedor;
    [ObservableProperty] private Producto? selectedProducto;
    [ObservableProperty] private LineaCompraItem? selectedLinea;
    [ObservableProperty] private string numeroComprobante = string.Empty;
    [ObservableProperty] private string cantidadTexto = string.Empty;
    [ObservableProperty] private string costoTexto = string.Empty;

    [ObservableProperty] private DateTimeOffset? desde =
        new(DateTime.Today.AddDays(-7));

    [ObservableProperty] private DateTimeOffset? hasta =
        new(DateTime.Today);

    public decimal Total => LineasItems.Sum(l => l.Subtotal);

    public string TotalTexto => Total.ToString("0.00", Cultura);

    public bool HayLineas => LineasItems.Count > 0;

    public bool EsPorPeso => SelectedProducto?.EsPorPeso == true;

    public string EtiquetaCantidad =>
        EsPorPeso ? "Cantidad (kg)" : "Cantidad";

    public string EtiquetaCosto =>
        EsPorPeso ? "Costo por kg" : "Costo unitario";

    public bool PuedeAnular =>
        SelectedCompra is not null && SelectedCompra.Estado == EstadoCompra.Completada;

    public int CantidadExportable => ComprasItems.Count;

    public string NombreArchivoExportacion =>
        ExportadorCsv.NombreArchivo(
            "compras", DesdeFecha, HastaFecha, Paginacion.PaginaActual);

    public string GenerarCsv() => ExportadorCsv.Compras(ComprasItems);

    private DateTime DesdeFecha =>
        (Desde ?? DateTimeOffset.Now.AddDays(-7)).Date;

    private DateTime HastaFecha =>
        (Hasta ?? DateTimeOffset.Now).Date;

    [RelayCommand]
    public Task CargarAsync() => EjecutarSeguroAsync(
        () => CargarDatosAsync(reiniciarPagina: true));

    private void CargarPaginaActual()
        => _ = EjecutarSeguroAsync(
            () => CargarComprasAsync(reiniciarPagina: false));

    private async Task CargarDatosAsync(bool reiniciarPagina = true)
    {
        if (_compraService is null || _proveedorService is null || _productoService is null)
            return;

        var desdeFecha = DesdeFecha;
        var hastaFecha = HastaFecha;

        if (hastaFecha < desdeFecha)
        {
            MensajeOperacion = Notificar(
                "La fecha hasta no puede ser anterior a la fecha desde.");
            return;
        }

        if (reiniciarPagina)
            Paginacion.Reiniciar();

        var idProveedor = SelectedProveedor?.Id;
        var idProducto = SelectedProducto?.Id;

        var proveedores = await _proveedorService.ObtenerActivosAsync();
        var productos = await _productoService.ObtenerTodosAsync();

        ReemplazarItems(ProveedoresItems, proveedores);
        ReemplazarItems(ProductosItems, productos);

        SelectedProveedor = ProveedoresItems.FirstOrDefault(p => p.Id == idProveedor);
        SelectedProducto = ProductosItems.FirstOrDefault(p => p.Id == idProducto);

        await CargarComprasAsync(reiniciarPagina: false);
    }

    private async Task CargarComprasAsync(bool reiniciarPagina = true)
    {
        if (_compraService is null) return;

        if (reiniciarPagina)
            Paginacion.Reiniciar();

        var desdeFecha = DesdeFecha;
        var hastaFecha = HastaFecha;

        if (hastaFecha < desdeFecha)
        {
            MensajeOperacion = Notificar(
                "La fecha hasta no puede ser anterior a la fecha desde.");
            return;
        }

        var idCompra = SelectedCompra?.Id;

        var pagina = await _compraService.ObtenerPorRangoPaginadoAsync(
            desdeFecha,
            hastaFecha,
            Paginacion.PaginaActual,
            Paginacion.TamanoPagina);

        ReemplazarItems(ComprasItems, pagina.Items);
        Paginacion.EstablecerTotal(pagina.TotalRegistros);
        SelectedCompra = ComprasItems.FirstOrDefault(c => c.Id == idCompra);
    }

    partial void OnSelectedCompraChanged(Compra? value)
        => OnPropertyChanged(nameof(PuedeAnular));

    partial void OnSelectedProductoChanged(Producto? value)
    {
        OnPropertyChanged(nameof(EsPorPeso));
        OnPropertyChanged(nameof(EtiquetaCantidad));
        OnPropertyChanged(nameof(EtiquetaCosto));

        if (value is null) return;

        CostoTexto = value.Costo.ToString("0.00", Cultura);
    }

    [RelayCommand]
    private void AgregarLinea()
    {
        if (SelectedProducto is null)
        {
            MensajeOperacion = Notificar("Selecciona un producto.");
            return;
        }

        if (!TryParseDecimal(CantidadTexto, out var cantidad) || cantidad <= 0)
        {
            MensajeOperacion = Notificar("La cantidad debe ser un número mayor que 0.");
            return;
        }

        if (SelectedProducto.UnidadMedida == UnidadMedida.Pieza &&
            !Cantidades.EsEntera(cantidad))
        {
            MensajeOperacion = Notificar(
                "Para productos por pieza, la cantidad debe ser un número entero.");
            return;
        }

        if (Cantidades.TieneDemasiadosDecimales(cantidad))
        {
            MensajeOperacion = Notificar(
                "La cantidad no puede tener más de 3 decimales.");
            return;
        }

        if (!TryParseDecimal(CostoTexto, out var costo) || costo < 0)
        {
            MensajeOperacion = Notificar("El costo debe ser un número mayor o igual a 0.");
            return;
        }

        var existente = LineasItems.FirstOrDefault(l => l.Producto.Id == SelectedProducto.Id);

        if (existente is not null)
            existente.Fusionar(cantidad, costo);
        else
            LineasItems.Add(new LineaCompraItem(SelectedProducto, cantidad, costo));

        NotificarTotal();
        MensajeOperacion = string.Empty;
    }

    [RelayCommand]
    private void QuitarLinea()
    {
        if (SelectedLinea is null) return;

        LineasItems.Remove(SelectedLinea);
        SelectedLinea = null;
        NotificarTotal();
    }

    [RelayCommand]
    private Task RegistrarAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_compraService is null) return;

        if (SelectedProveedor is null)
        {
            MensajeOperacion = Notificar("Selecciona un proveedor.");
            return;
        }

        if (LineasItems.Count == 0)
        {
            MensajeOperacion = Notificar("Agrega al menos un producto.");
            return;
        }

        var lineas = LineasItems
            .Select(l => new LineaCompra(l.Producto.Id, l.Cantidad, l.CostoUnitario))
            .ToList();

        var resultado = await _compraService.RegistrarAsync(
            SelectedProveedor.Id, NumeroComprobante, lineas);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (!resultado.Correcta) return;

        var idCompra = resultado.Valor?.Id;

        LimpiarFormulario();

        await CargarDatosAsync();
        SelectedCompra = ComprasItems.FirstOrDefault(c => c.Id == idCompra);
    });

    [RelayCommand]
    private void LimpiarFormulario()
    {
        LineasItems.Clear();
        SelectedLinea = null;
        SelectedProveedor = null;
        SelectedProducto = null;
        SelectedCompra = null;
        NumeroComprobante = string.Empty;
        CantidadTexto = string.Empty;
        CostoTexto = string.Empty;
        NotificarTotal();
    }

    /// <summary>
    /// La llama la vista después de confirmar la anulación.
    /// </summary>
    public Task AnularSeleccionadaAsync() => EjecutarSeguroAsync(async () =>
    {
        if (_compraService is null || SelectedCompra is null) return;

        var id = SelectedCompra.Id;
        var resultado = await _compraService.AnularAsync(id);

        MensajeOperacion = Notificar(resultado.Mensaje, resultado.Correcta);

        if (!resultado.Correcta) return;

        LimpiarFormulario();
        await CargarDatosAsync();
        SelectedCompra = ComprasItems.FirstOrDefault(c => c.Id == id);
    });

    public override void LimpiarPantalla()
    {
        base.LimpiarPantalla();

        ComprasItems.Clear();
        ProveedoresItems.Clear();
        ProductosItems.Clear();
        Paginacion.Reiniciar();
        Desde = new DateTimeOffset(DateTime.Today.AddDays(-7));
        Hasta = new DateTimeOffset(DateTime.Today);
        LimpiarFormulario();
    }

    private void NotificarTotal()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(HayLineas));
    }

    private static bool TryParseDecimal(string? texto, out decimal valor)
    {
        var limpio = (texto ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, Cultura, out valor);
    }

    private static void ReemplazarItems<T>(
        ObservableCollection<T> destino, IEnumerable<T> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }
}
