using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa
{
    public partial class ContrasenaAdminFrontView : UserControl
    {
        public ContrasenaAdminFrontView()
        {
            InitializeComponent();
            DataContext = new ContrasenaAdminFrontViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
