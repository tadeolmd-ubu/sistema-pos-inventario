using Avalonia.Controls;
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
}
