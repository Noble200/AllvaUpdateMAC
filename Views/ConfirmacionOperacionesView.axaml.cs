using Avalonia.Controls;
using Avalonia.Interactivity;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views;

public partial class ConfirmacionOperacionesView : Window
{
    public bool Confirmado { get; private set; } = false;
    
    public ConfirmacionOperacionesView()
    {
        InitializeComponent();
        DataContext = new ConfirmacionOperacionesViewModel();
    }
    
    public ConfirmacionOperacionesView(ConfirmacionOperacionesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    
    private void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        Confirmado = true;
        Close();
    }
    
    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Confirmado = false;
        Close();
    }
}