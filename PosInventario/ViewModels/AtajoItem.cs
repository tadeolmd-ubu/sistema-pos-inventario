namespace PosInventario.ViewModels;

/// <summary>
/// Atajo mostrado en el panel de comandos del menú lateral. El contenido
/// cambia según la pestaña activa y el control que tiene el foco.
/// </summary>
public sealed record AtajoItem(string Tecla, string Descripcion);
