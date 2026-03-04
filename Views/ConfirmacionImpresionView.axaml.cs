using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views;

public partial class ConfirmacionImpresionView : Window
{
    public bool Confirmado { get; private set; } = false;
    
    public ConfirmacionImpresionView()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public ConfirmacionImpresionView(ConfirmacionImpresionViewModel viewModel)
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = viewModel;
    }
    
    private void OnCancelarClick(object? sender, RoutedEventArgs e)
    {
        Confirmado = false;
        Close();
    }
    
    private void OnGenerarClick(object? sender, RoutedEventArgs e)
    {
        Confirmado = true;
        Close();
    }
}