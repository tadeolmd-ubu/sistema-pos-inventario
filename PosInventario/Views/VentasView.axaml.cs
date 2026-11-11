using Avalonia.Controls;
using Avalonia.Input;
using PosInventario.Models;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class VentasView : UserControl
{
    private VentasViewModel? _viewModel;

    public VentasView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as VentasViewModel);

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Ventas;
    }

    private void ConectarViewModel(VentasViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.TicketSolicitado -= OnTicket;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.TicketSolicitado += OnTicket;
    }

    private void Ventas_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel?.VerTicketCommand.CanExecute(null) == true)
            _viewModel.VerTicketCommand.Execute(null);
    }

    private async void OnTicket(Venta venta)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        await new TicketWindow
        {
            DataContext = new TicketViewModel(venta)
        }.ShowDialog(ventana);
    }
}
