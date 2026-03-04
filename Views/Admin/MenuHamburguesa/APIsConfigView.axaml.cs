using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa
{
    public partial class APIsConfigView : UserControl
    {
        public APIsConfigView()
        {
            InitializeComponent();
            DataContext = new APIsConfigViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}