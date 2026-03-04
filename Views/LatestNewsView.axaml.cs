using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views;

public partial class LatestNewsView : UserControl
{
    public LatestNewsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EnlaceNoticia_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.Tag is NoticiaItem noticia)
        {
            noticia.AbrirEnlaceCommand.Execute(null);
        }
    }

    private void DotIndicator_Tapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Avalonia.Controls.Shapes.Ellipse ellipse &&
            ellipse.DataContext is NoticiaItem noticia &&
            DataContext is LatestNewsViewModel viewModel)
        {
            var index = viewModel.NoticiasCarrusel.IndexOf(noticia);
            if (index >= 0)
            {
                viewModel.IrASlide(index);
            }
        }
    }
}