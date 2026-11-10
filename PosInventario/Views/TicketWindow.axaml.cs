using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class TicketWindow : Window
{
    public TicketWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is TicketViewModel viewModel)
                Title = $"Ticket {viewModel.Numero}";
        };
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close();
}
