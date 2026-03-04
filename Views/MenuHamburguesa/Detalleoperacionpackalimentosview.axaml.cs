using Avalonia.Controls;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class DetalleOperacionPackAlimentosView : Window
{
    public DetalleOperacionPackAlimentosView()
    {
        InitializeComponent();
    }

    public DetalleOperacionPackAlimentosView(string numeroOperacion, string codigoLocal, string nombreUsuario, int numeroUsuario)
    {
        InitializeComponent();
        var vm = new DetalleOperacionPackAlimentosViewModel(numeroOperacion, codigoLocal, nombreUsuario, numeroUsuario);
        vm.SetVentana(this);
        DataContext = vm;
        
        // Cargar datos cuando se abra la ventana
        Opened += async (_, _) => await vm.CargarDatosAsync();
    }
}