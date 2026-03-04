using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin
{
    public partial class PagoSuscripcionesView : UserControl
    {
        public PagoSuscripcionesView()
        {
            InitializeComponent();
            DataContext = new PagoSuscripcionesViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
