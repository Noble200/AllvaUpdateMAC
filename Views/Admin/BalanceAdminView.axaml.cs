using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin
{
    public partial class BalanceAdminView : UserControl
    {
        public BalanceAdminView()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = new BalanceAdminViewModel();
        }
    }
}