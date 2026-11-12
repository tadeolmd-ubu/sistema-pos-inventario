using Avalonia.Controls;
using PosInventario.Views.DesignTime;

namespace PosInventario.Views;

public partial class ReportesView : UserControl
{
    public ReportesView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            DataContext = DesignTimeData.Reportes;
    }
}
