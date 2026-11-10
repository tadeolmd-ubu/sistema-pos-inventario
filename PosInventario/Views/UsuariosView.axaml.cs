using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
        var ventana = new RestablecerContrasenaWindow { DataContext = viewModel };

        var owner = TopLevel.GetTopLevel(this) as Window;

        if (owner is { IsVisible: true })
        {
            _ = owner.ShowDialog(ventana);
            return;
        }

        var mainWindow = (Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow is { IsVisible: true })
            _ = mainWindow.ShowDialog(ventana);
        else
            ventana.Show();
    }
}
