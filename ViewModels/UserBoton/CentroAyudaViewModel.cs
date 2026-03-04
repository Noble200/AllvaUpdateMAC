using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels.UserBoton;

public partial class CentroAyudaViewModel : ObservableObject
{
    [ObservableProperty]
    private string _busquedaTexto = "";

    [ObservableProperty]
    private ObservableCollection<TelefonoContacto> _telefonos = new();

    [ObservableProperty]
    private ObservableCollection<PreguntaFrecuenteUI> _preguntasFrecuentes = new();

    [ObservableProperty]
    private string _rutaManualPdf = "";

    [ObservableProperty]
    private bool _tieneManual;

    private Action? _cerrarAction;

    public CentroAyudaViewModel()
    {
        CargarConfiguracion();
    }

    public CentroAyudaViewModel(Action? cerrarAction)
    {
        _cerrarAction = cerrarAction;
        CargarConfiguracion();
    }

    public void SetCerrarAction(Action cerrarAction)
    {
        _cerrarAction = cerrarAction;
    }

    private void CargarConfiguracion()
    {
        var configService = FrontOfficeConfigService.Instance;
        // Forzar recarga para obtener datos actualizados
        configService.RecargarConfiguracion();
        var config = configService.ObtenerConfiguracion();

        Telefonos.Clear();
        foreach (var tel in config.Telefonos)
        {
            Telefonos.Add(tel);
        }

        // Verificar si existe el PDF del manual
        var rutaPdf = configService.ObtenerRutaManualPdf();
        if (!string.IsNullOrEmpty(rutaPdf) && File.Exists(rutaPdf))
        {
            RutaManualPdf = rutaPdf;
            TieneManual = true;
        }
        else
        {
            RutaManualPdf = "";
            TieneManual = false;
        }

        // Cargar preguntas frecuentes
        CargarPreguntasFrecuentes();
    }

    private void CargarPreguntasFrecuentes()
    {
        PreguntasFrecuentes.Clear();
        var configService = FrontOfficeConfigService.Instance;
        var preguntas = configService.ObtenerPreguntasFrecuentes();

        foreach (var p in preguntas)
        {
            var preguntaUI = new PreguntaFrecuenteUI
            {
                Id = p.Id,
                Pregunta = p.Pregunta,
                EstaExpandida = false
            };

            foreach (var r in p.Respuestas)
            {
                preguntaUI.Respuestas.Add(r.Respuesta);
            }

            PreguntasFrecuentes.Add(preguntaUI);
        }
    }

    [RelayCommand]
    private void TogglePregunta(int idPregunta)
    {
        var pregunta = PreguntasFrecuentes.FirstOrDefault(p => p.Id == idPregunta);
        if (pregunta != null)
        {
            pregunta.EstaExpandida = !pregunta.EstaExpandida;
            // Forzar actualizacion de UI
            var index = PreguntasFrecuentes.IndexOf(pregunta);
            if (index >= 0)
            {
                PreguntasFrecuentes.RemoveAt(index);
                PreguntasFrecuentes.Insert(index, pregunta);
            }
        }
    }

    [RelayCommand]
    private void Cerrar()
    {
        _cerrarAction?.Invoke();
    }

    [RelayCommand]
    private void BuscarAyuda()
    {
        System.Diagnostics.Debug.WriteLine($"Buscando: {BusquedaTexto}");
    }

    [RelayCommand]
    private void DescargarManual()
    {
        if (!string.IsNullOrWhiteSpace(RutaManualPdf) && File.Exists(RutaManualPdf))
        {
            try
            {
                // Abrir el PDF con la aplicacion predeterminada
                Process.Start(new ProcessStartInfo
                {
                    FileName = RutaManualPdf,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir manual: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void Recargar()
    {
        FrontOfficeConfigService.Instance.RecargarConfiguracion();
        CargarConfiguracion();
    }
}

/// <summary>
/// Clase para mostrar preguntas en el UI del FrontOffice
/// </summary>
public partial class PreguntaFrecuenteUI : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _pregunta = "";

    [ObservableProperty]
    private bool _estaExpandida;

    [ObservableProperty]
    private ObservableCollection<string> _respuestas = new();

    // Propiedad para mostrar el numero de respuestas
    public string TextoRespuestas => Respuestas.Count == 1
        ? "1 respuesta"
        : $"{Respuestas.Count} respuestas";
}
