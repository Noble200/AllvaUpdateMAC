using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Views.Admin.MenuHamburguesa;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class EdicionFrontOfficeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabSeleccionada = "cuentas";

        [ObservableProperty]
        private bool _esTabCuentasBancarias = true;

        [ObservableProperty]
        private bool _esTabCentroAyuda;

        [ObservableProperty]
        private bool _esTabUltimasNoticias;

        [ObservableProperty]
        private bool _esTabPackAlimentos;

        [ObservableProperty]
        private bool _esTabPackViajes;

        [ObservableProperty]
        private UserControl? _vistaActual;

        public EdicionFrontOfficeViewModel()
        {
            CambiarTab("cuentas");
        }

        [RelayCommand]
        private void CambiarTab(string tab)
        {
            TabSeleccionada = tab;

            EsTabCuentasBancarias = tab == "cuentas";
            EsTabCentroAyuda = tab == "ayuda";
            EsTabUltimasNoticias = tab == "noticias";
            EsTabPackAlimentos = tab == "alimentos";
            EsTabPackViajes = tab == "viajes";

            VistaActual = tab switch
            {
                "cuentas" => new CuentasBancariasAdminView(),
                "ayuda" => new CentroAyudaAdminView(),
                "noticias" => new UltimasNoticiasAdminView(),
                "alimentos" => new PacksAlimentosView(),
                "viajes" => new PackViajesAdminView(),
                _ => new CuentasBancariasAdminView()
            };
        }
    }
}
