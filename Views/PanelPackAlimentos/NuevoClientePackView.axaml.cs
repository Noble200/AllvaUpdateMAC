using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.Helpers;

namespace Allva.Desktop.Views.PanelPackAlimentos;

public partial class NuevoClientePackView : UserControl
{
    public NuevoClientePackView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        // Conectar eventos para formateo de teléfono
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtTelefono"));
    }
}