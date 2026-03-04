using Avalonia.Controls;
using Allva.Desktop.ViewModels.Admin;
using Allva.Desktop.Helpers;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa;

public partial class UsuariosAllvaView : UserControl
{
    public UsuariosAllvaView()
    {
        InitializeComponent();
        DataContext = new ManageAdministradoresAllvaViewModel();

        // Conectar eventos para formateo de teléfono
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtTelefono"));
    }
}
