using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PosInventario.Views;

public partial class CajaHistorialWindow : Window
{
    public CajaHistorialWindow() => InitializeComponent();

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close();
}
