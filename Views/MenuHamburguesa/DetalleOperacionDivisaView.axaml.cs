using Avalonia.Controls;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class DetalleOperacionDivisaView : Window
{
    public DetalleOperacionDivisaView()
    {
        InitializeComponent();
    }
    
    public DetalleOperacionDivisaView(DetalleOperacionDivisaViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.SolicitarCierre += () => Close();
    }
}