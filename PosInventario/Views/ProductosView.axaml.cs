using Avalonia.Controls;
using Avalonia.Input;
using PosInventario.ViewModels;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ProductosView : UserControl
{
    public ProductosView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Productos;
    }

    private async void Sku_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        if (DataContext is not ProductosViewModel viewModel)
            return;

        await viewModel.EscanearCodigoAsync(SkuBox.Text);

        // Código nuevo: el foco pasa a Nombre para capturar el resto.
        if (viewModel.EsModoCreacion && !string.IsNullOrWhiteSpace(viewModel.Sku))
            NombreBox.Focus();
    }
}
