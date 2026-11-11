using Avalonia.Controls;
using PosInventario.Models;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class InicioView : UserControl
{
    public InicioView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Inicio;
    }

    private async void Ventas_SelectionChanged(
        object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lista || lista.SelectedItem is not Venta venta)
            return;

        lista.SelectedIndex = -1;

        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await new TicketWindow
        {
            DataContext = new TicketViewModel(venta)
        }.ShowDialog(ventana);
    }
}
