using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views.PanelDivisas;

public partial class TransaccionDivisaView : UserControl
{
    public TransaccionDivisaView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}