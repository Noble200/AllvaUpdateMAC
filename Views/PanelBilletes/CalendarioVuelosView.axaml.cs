using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views.PanelBilletes;

public partial class CalendarioVuelosView : UserControl
{
    public CalendarioVuelosView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
