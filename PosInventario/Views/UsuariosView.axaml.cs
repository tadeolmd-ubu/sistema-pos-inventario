using Avalonia.Controls;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class UsuariosView : UserControl
{
    private UsuariosViewModel? _viewModel;

    public UsuariosView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as UsuariosViewModel);

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Usuarios;
    }

    private void ConectarViewModel(UsuariosViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.RestablecerContrasenaSolicitado -= OnRestablecerContrasena;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.RestablecerContrasenaSolicitado += OnRestablecerContrasena;
    }

    private void OnRestablecerContrasena(RestablecerContrasenaViewModel viewModel)
    {
        new RestablecerContrasenaWindow { DataContext = viewModel }.Show();
    }
}
