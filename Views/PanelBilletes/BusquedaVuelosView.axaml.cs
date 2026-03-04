using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views.PanelBilletes;

public partial class BusquedaVuelosView : UserControl
{
    public BusquedaVuelosView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
