using Avalonia.Controls;

namespace PosInventario.Controls;

/// <summary>
/// Controles de navegación de página reutilizables. Espera como DataContext
/// un <see cref="ViewModels.PaginacionViewModel"/>.
/// </summary>
public partial class Paginador : UserControl
{
    public Paginador() => InitializeComponent();
}
