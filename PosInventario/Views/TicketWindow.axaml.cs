using Avalonia.Controls;
using Avalonia.Input;
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

        AddHandler(KeyDownEvent, Ticket_KeyDown, RoutingStrategies.Tunnel);
    }

    private async void Ticket_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }

        if (e.Key != Key.P || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        e.Handled = true;

        if (DataContext is TicketViewModel viewModel &&
            viewModel.ImprimirCommand.CanExecute(null))
        {
            await viewModel.ImprimirCommand.ExecuteAsync(null);
        }
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => Close();
}
