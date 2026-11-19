using Avalonia.Controls;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ClientesView : UserControl
{
    private ClientesViewModel? _viewModel;

    public ClientesView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as ClientesViewModel);

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Clientes;
    }

    private void ConectarViewModel(ClientesViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.AbonoSolicitado -= OnAbonoSolicitado;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.AbonoSolicitado += OnAbonoSolicitado;
    }

    private async void OnAbonoSolicitado(AbonoViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this) is not Window ventana)
            return;

        var registrado = await new AbonoWindow { DataContext = viewModel }
            .ShowDialog<bool>(ventana);

        if (registrado && _viewModel is not null)
            await _viewModel.RecargarAsync();
    }
}
