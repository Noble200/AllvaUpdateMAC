using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;
using Allva.Desktop.Services;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa
{
    public partial class AltaInscripcionView : UserControl
    {
        public AltaInscripcionView()
        {
            InitializeComponent();
            DataContext = new AltaInscripcionViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ComercioItem_Click(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.Tag is ComercioInfo comercio && DataContext is AltaInscripcionViewModel vm)
            {
                vm.ComercioSeleccionado = comercio;
            }
        }

        private void LocalItem_Click(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.Tag is LocalInfo local && DataContext is AltaInscripcionViewModel vm)
            {
                vm.SeleccionarLocalCommand.Execute(local);
            }
        }
    }
}
