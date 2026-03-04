using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin;

public partial class ComisionesView : UserControl
{
    public ComisionesView()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ComisionesViewModel();
    }
}