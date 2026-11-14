using Avalonia.Controls;
using Avalonia.Interactivity;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class ImportarDatosWindow : Window
{
    private ImportarDatosViewModel? _viewModel;

    public ImportarDatosWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
            _viewModel = DataContext as ImportarDatosViewModel;
    }

    private async void Importar_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
            return;

        await _viewModel.ImportarAsync();

        if (_viewModel.Importada)
            Close(true);
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
        => Close(false);
}
