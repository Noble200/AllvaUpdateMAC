using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views.PanelDivisas;

public partial class ResumenCompraView : UserControl
{
    public ResumenCompraView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}