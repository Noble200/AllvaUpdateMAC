using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Allva.Desktop.Views.PanelBilletes;

public partial class DatosPasajerosView : UserControl
{
    public DatosPasajerosView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
