namespace PosInventario.Service;

/// <summary>
/// Resultado de una consulta paginada: solo trae las filas de la página
/// solicitada y el total de filas que cumplen el filtro, para que la vista
/// pueda mostrar la navegación sin cargar todo el historial.
/// </summary>
public sealed record PaginaResultado<T>(
    IReadOnlyList<T> Items,
    int TotalRegistros,
    decimal Total = 0m);
