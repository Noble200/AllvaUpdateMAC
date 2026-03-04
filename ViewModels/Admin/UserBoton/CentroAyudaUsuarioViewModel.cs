using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Allva.Desktop.ViewModels.Admin.UserBoton
{
    public partial class CentroAyudaUsuarioViewModel : ObservableObject
    {
        private readonly Action? _onClose;

        [ObservableProperty]
        private bool _tieneManual = true;

        [ObservableProperty]
        private string _telefonoSoporte = "+34 900 000 000";

        [ObservableProperty]
        private string _versionSistema = "1.0.0";

        public CentroAyudaUsuarioViewModel()
        {
        }

        public CentroAyudaUsuarioViewModel(Action onClose)
        {
            _onClose = onClose;
        }

        [RelayCommand]
        private void DescargarManual()
        {
            // TODO: Implementar descarga del manual de administrador
        }

        [RelayCommand]
        private void Cerrar()
        {
            _onClose?.Invoke();
        }
    }
}
