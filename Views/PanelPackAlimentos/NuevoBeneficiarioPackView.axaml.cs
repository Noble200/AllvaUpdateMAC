using Avalonia.Controls;
using Allva.Desktop.Helpers;

namespace Allva.Desktop.Views.PanelPackAlimentos;

public partial class NuevoBeneficiarioPackView : UserControl
{
    public NuevoBeneficiarioPackView()
    {
        InitializeComponent();

        // Conectar eventos para formateo de teléfono
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtTelefono"));
    }
}