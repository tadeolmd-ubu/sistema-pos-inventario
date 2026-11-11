using Avalonia.Controls;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ProveedoresView : UserControl
{
    public ProveedoresView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Proveedores;
    }
}
