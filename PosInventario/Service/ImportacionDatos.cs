using PosInventario.Models.Enums;

namespace PosInventario.Service;

public sealed record ErrorImportacion(int Fila, string Mensaje);

public sealed record FilaImportacion(
    int Fila,
    string Sku,
    string Nombre,
    UnidadMedida UnidadMedida,
    string? Descripcion,
    decimal PrecioVenta,
    decimal Costo,
    decimal StockMinimo,
    decimal StockInicial,
    string? ProveedorNombre,
    string? ProveedorTelefono,
    string? ProveedorEmail,
    string? ProveedorDireccion);

public sealed class ImportacionVistaPrevia
{
    public required string Ruta { get; init; }
    public required IReadOnlyList<FilaImportacion> Filas { get; init; }
    public required IReadOnlyList<ErrorImportacion> Errores { get; init; }
    public required int ProductosNuevos { get; init; }
    public required int ProductosActualizados { get; init; }
    public required int ProveedoresNuevos { get; init; }
    public required int ProveedoresReutilizados { get; init; }
    public required decimal UnidadesStock { get; init; }

    public bool PuedeImportar => Errores.Count == 0 && Filas.Count > 0;
}

public sealed record ImportacionResultado(
    int ProductosNuevos,
    int ProductosActualizados,
    int ProveedoresNuevos,
    decimal UnidadesStock,
    string Mensaje);
