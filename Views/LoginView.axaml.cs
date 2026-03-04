using Avalonia.Controls;
using Avalonia.Input;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            // Suscribirse al evento KeyDown del UserControl
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Si se presiona Enter y no está cargando, ejecutar login
            if (e.Key == Key.Enter)
            {
                if (DataContext is LoginViewModel viewModel && !viewModel.IsLoading)
                {
                    viewModel.LoginCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
