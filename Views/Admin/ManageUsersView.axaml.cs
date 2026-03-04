using Avalonia.Controls;
using Allva.Desktop.ViewModels.Admin;
using Allva.Desktop.Helpers;

namespace Allva.Desktop.Views.Admin;

public partial class ManageUsersView : UserControl
{
    public ManageUsersView()
    {
        InitializeComponent();
        DataContext = new ManageUsersViewModel();

        // Conectar eventos para formateo de teléfono
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtTelefono"));
    }
}
