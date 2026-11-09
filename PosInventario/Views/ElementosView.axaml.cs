using Avalonia.Controls;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ElementosView : UserControl
{
    public ElementosView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Elementos;
    }
}
