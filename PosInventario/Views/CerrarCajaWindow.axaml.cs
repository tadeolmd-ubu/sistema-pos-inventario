using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PosInventario.ViewModels;

namespace PosInventario.Views;

public partial class CerrarCajaWindow : Window
{
    private CerrarCajaViewModel? _viewModel;

    public CerrarCajaWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            ConectarViewModel(DataContext as CerrarCajaViewModel);

        Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            MontoFinalBox.Focus();
            MontoFinalBox.SelectAll();
        });
    }

    private void ConectarViewModel(CerrarCajaViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.CajaCerrada -= OnCajaCerrada;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.CajaCerrada += OnCajaCerrada;
    }

    private async void Cerrar_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (!_viewModel.MontoContadoValido)
        {
            await _viewModel.CerrarCommand.ExecuteAsync(null);
            return;
        }

        if (_viewModel.HayDiferencia)
        {
            var diferencia = _viewModel.HayFaltante
                ? _viewModel.FaltanteTexto
                : _viewModel.SobranteTexto;

            var mensaje =
                $"El monto contado ({_viewModel.MontoContadoTexto}) no coincide " +
                $"con el esperado ({_viewModel.MontoEsperadoTexto}).\n\n" +
                $"{diferencia}\n\n" +
                "¿Cerrar la caja de todos modos?";

            var confirmar = await new ConfirmacionWindow(
                "Cerrar caja con diferencia", mensaje)
                .ShowDialog<bool>(this);

            if (!confirmar)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    MontoFinalBox.Focus();
                    MontoFinalBox.SelectAll();
                });
                return;
            }
        }

        await _viewModel.CerrarCommand.ExecuteAsync(null);
    }

    private void OnCajaCerrada() => Close(true);

    private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
}
