using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PosInventario.Views;

public partial class ConfirmacionWindow : Window
{
    public ConfirmacionWindow() => InitializeComponent();

    public ConfirmacionWindow(string titulo, string mensaje) : this()
    {
        Title = titulo;
        TituloTexto.Text = titulo;
        MensajeTexto.Text = mensaje;
    }

    private void Confirmar_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
