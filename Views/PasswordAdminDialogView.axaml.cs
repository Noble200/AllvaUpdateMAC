using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views;

public partial class PasswordAdminDialogView : UserControl
{
    public PasswordAdminDialogView()
    {
        InitializeComponent();
    }

    public event EventHandler<bool>? DialogClosed;

    private async void OnVerificar(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordAdminDialogViewModel viewModel)
        {
            bool resultado = await viewModel.VerificarPasswordAsync();
            DialogClosed?.Invoke(this, resultado);
        }
    }

    private void OnCancelar(object? sender, RoutedEventArgs e)
    {
        DialogClosed?.Invoke(this, false);
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Enfocar el TextBox de contraseña cuando se muestra el diálogo
        var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
        if (passwordTextBox != null)
        {
            passwordTextBox.Focus();
        }
    }
}
