using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views.PanelPackAlimentos
{
    public partial class FoodPacksView : UserControl
    {
        public FoodPacksView()
        {
            InitializeComponent();
            // DataContext se asigna desde MainDashboardViewModel
        }

        public FoodPacksView(int idComercio, int idLocal)
        {
            InitializeComponent();
            DataContext = new FoodPacksViewModel(idComercio, idLocal);
        }

        public FoodPacksView(int idComercio, int idLocal, int idUsuario, 
                             string nombreUsuario, string numeroUsuario, string codigoLocal)
        {
            InitializeComponent();
            DataContext = new FoodPacksViewModel(idComercio, idLocal, idUsuario, 
                                                  nombreUsuario, numeroUsuario, codigoLocal);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}