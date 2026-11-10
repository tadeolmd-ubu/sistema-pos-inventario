using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PosInventario.Views;

public partial class MovimientoCajaWindow : Window
{
    public MovimientoCajaWindow() => InitializeComponent();

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close();
}
