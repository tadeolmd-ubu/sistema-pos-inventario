using Avalonia.Controls;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class InicioView : UserControl
{
    public InicioView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Inicio;
    }
}
