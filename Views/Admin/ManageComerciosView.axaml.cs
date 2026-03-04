using Avalonia.Controls;
using Allva.Desktop.ViewModels.Admin;
using Allva.Desktop.Helpers;

namespace Allva.Desktop.Views.Admin;

/// <summary>
/// Vista para el módulo de Gestión de Comercios
/// Code-behind mínimo siguiendo patrón MVVM
/// </summary>
public partial class ManageComerciosView : UserControl
{
    public ManageComerciosView()
    {
        InitializeComponent();
        DataContext = new ManageComerciosViewModel();

        // Conectar eventos para formateo de teléfono
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtMovilComercio"));
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtTelefonoSucursal"));
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtMovilSucursal"));
    }
}