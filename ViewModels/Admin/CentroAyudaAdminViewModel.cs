using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels.Admin;

public partial class CentroAyudaAdminViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TelefonoContacto> _telefonos = new();

    [ObservableProperty]
    private string _nombreArchivoPdf = "Ningun archivo seleccionado";

    [ObservableProperty]
    private bool _tienePdf;

    // Preguntas frecuentes
    [ObservableProperty]
    private ObservableCollection<PreguntaFrecuenteEditable> _preguntasFrecuentes = new();

    private readonly FrontOfficeConfigService _configService;

    // Contador para generar IDs temporales negativos para preguntas nuevas
    private int _tempIdCounter = -1;

    public CentroAyudaAdminViewModel()
    {
        _configService = FrontOfficeConfigService.Instance;
        CargarConfiguracion();
    }

    private void CargarConfiguracion()
    {
        var config = _configService.ObtenerConfiguracion();

        Telefonos.Clear();
        foreach (var tel in config.Telefonos)
        {
            Telefonos.Add(tel);
        }

        // Verificar si existe el PDF
        var rutaPdf = _configService.ObtenerRutaManualPdf();
        if (!string.IsNullOrEmpty(rutaPdf) && File.Exists(rutaPdf))
        {
            NombreArchivoPdf = Path.GetFileName(rutaPdf);
            TienePdf = true;
        }
        else
        {
            NombreArchivoPdf = "Ningun archivo seleccionado";
            TienePdf = false;
        }

        // Cargar preguntas frecuentes
        CargarPreguntasFrecuentes();
    }

    private void CargarPreguntasFrecuentes()
    {
        PreguntasFrecuentes.Clear();
        var preguntas = _configService.ObtenerPreguntasFrecuentes();
        foreach (var p in preguntas)
        {
            var preguntaEditable = new PreguntaFrecuenteEditable
            {
                Id = p.Id,
                Pregunta = p.Pregunta,
                Orden = p.Orden,
                EstaExpandida = false
            };

            foreach (var r in p.Respuestas)
            {
                preguntaEditable.Respuestas.Add(new RespuestaEditable
                {
                    Id = r.Id,
                    IdPregunta = r.IdPregunta,
                    Respuesta = r.Respuesta,
                    Orden = r.Orden
                });
            }

            PreguntasFrecuentes.Add(preguntaEditable);
        }
    }

    #region PDF

    [RelayCommand]
    private async Task SeleccionarPdf()
    {
        try
        {
            var window = GetMainWindow();
            if (window == null) return;

            var storageProvider = window.StorageProvider;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar Manual de Usuario",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Archivos PDF")
                    {
                        Patterns = new[] { "*.pdf" },
                        MimeTypes = new[] { "application/pdf" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                var sourcePath = file.Path.LocalPath;

                // Copiar el archivo a la carpeta de configuracion
                _configService.GuardarManualPdf(sourcePath);

                NombreArchivoPdf = file.Name;
                TienePdf = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al seleccionar PDF: {ex.Message}");
        }
    }

    [RelayCommand]
    private void EliminarPdf()
    {
        _configService.EliminarManualPdf();
        NombreArchivoPdf = "Ningun archivo seleccionado";
        TienePdf = false;
    }

    #endregion

    #region Telefonos

    [RelayCommand]
    private void AgregarTelefono()
    {
        var nuevoTelefono = new TelefonoContacto
        {
            Nombre = "",
            Numero = ""
        };
        Telefonos.Add(nuevoTelefono);
    }

    [RelayCommand]
    private void EliminarTelefono(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;

        var telefono = Telefonos.FirstOrDefault(t => t.Id == id);
        if (telefono != null)
        {
            Telefonos.Remove(telefono);
        }
    }

    #endregion

    #region Preguntas Frecuentes

    [RelayCommand]
    private void AgregarPregunta()
    {
        var nuevaPregunta = new PreguntaFrecuenteEditable
        {
            Id = _tempIdCounter--, // ID temporal negativo unico, se asignara el real al guardar
            Pregunta = "",
            Orden = PreguntasFrecuentes.Count,
            EsNueva = true,
            EstaExpandida = true
        };
        PreguntasFrecuentes.Add(nuevaPregunta);
    }

    [RelayCommand]
    private void EliminarPregunta(int idPregunta)
    {
        var pregunta = PreguntasFrecuentes.FirstOrDefault(p => p.Id == idPregunta);
        if (pregunta != null)
        {
            if (!pregunta.EsNueva && pregunta.Id > 0)
            {
                // Marcar para eliminar en BD (solo si tiene ID real positivo)
                _configService.EliminarPreguntaFrecuente(pregunta.Id);
            }
            PreguntasFrecuentes.Remove(pregunta);
        }
    }

    [RelayCommand]
    private void ToggleExpandirPregunta(int idPregunta)
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
    private void AgregarRespuesta(int idPregunta)
    {
        var pregunta = PreguntasFrecuentes.FirstOrDefault(p => p.Id == idPregunta);
        if (pregunta != null)
        {
            var nuevaRespuesta = new RespuestaEditable
            {
                Id = 0,
                IdPregunta = idPregunta,
                Respuesta = "",
                Orden = pregunta.Respuestas.Count,
                EsNueva = true
            };
            pregunta.Respuestas.Add(nuevaRespuesta);

            // Asegurar que la pregunta este expandida
            if (!pregunta.EstaExpandida)
            {
                pregunta.EstaExpandida = true;
                var index = PreguntasFrecuentes.IndexOf(pregunta);
                if (index >= 0)
                {
                    PreguntasFrecuentes.RemoveAt(index);
                    PreguntasFrecuentes.Insert(index, pregunta);
                }
            }
        }
    }

    [RelayCommand]
    private void EliminarRespuesta(RespuestaEditable? respuesta)
    {
        if (respuesta == null) return;

        var pregunta = PreguntasFrecuentes.FirstOrDefault(p => p.Respuestas.Contains(respuesta));
        if (pregunta != null)
        {
            if (!respuesta.EsNueva && respuesta.Id > 0)
            {
                _configService.EliminarRespuesta(respuesta.Id);
            }
            pregunta.Respuestas.Remove(respuesta);
        }
    }

    #endregion

    #region Guardar

    [RelayCommand]
    private void Guardar()
    {
        try
        {
            // Guardar telefonos
            _configService.GuardarTelefonos(Telefonos.ToList());

            // Guardar preguntas frecuentes
            foreach (var pregunta in PreguntasFrecuentes)
            {
                // Una pregunta es nueva si tiene ID negativo (temporal) o EsNueva = true
                if (pregunta.EsNueva || pregunta.Id < 0)
                {
                    // Crear nueva pregunta
                    var nuevoId = _configService.AgregarPreguntaFrecuente(pregunta.Pregunta, pregunta.Orden);
                    if (nuevoId > 0)
                    {
                        pregunta.Id = nuevoId;
                        pregunta.EsNueva = false;

                        // Guardar respuestas de esta pregunta
                        foreach (var respuesta in pregunta.Respuestas)
                        {
                            var idRespuesta = _configService.AgregarRespuesta(nuevoId, respuesta.Respuesta, respuesta.Orden);
                            if (idRespuesta > 0)
                            {
                                respuesta.Id = idRespuesta;
                                respuesta.IdPregunta = nuevoId;
                                respuesta.EsNueva = false;
                            }
                        }
                    }
                }
                else
                {
                    // Actualizar pregunta existente
                    _configService.ActualizarPreguntaFrecuente(pregunta.Id, pregunta.Pregunta, pregunta.Orden);

                    // Guardar respuestas
                    foreach (var respuesta in pregunta.Respuestas)
                    {
                        if (respuesta.EsNueva || respuesta.Id <= 0)
                        {
                            var idRespuesta = _configService.AgregarRespuesta(pregunta.Id, respuesta.Respuesta, respuesta.Orden);
                            if (idRespuesta > 0)
                            {
                                respuesta.Id = idRespuesta;
                                respuesta.IdPregunta = pregunta.Id;
                                respuesta.EsNueva = false;
                            }
                        }
                        else
                        {
                            _configService.ActualizarRespuesta(respuesta.Id, respuesta.Respuesta, respuesta.Orden);
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("Centro de Ayuda guardado correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar Centro de Ayuda: {ex.Message}");
        }
    }

    #endregion

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }
}

/// <summary>
/// Clase auxiliar para edicion de preguntas en el formulario
/// </summary>
public partial class PreguntaFrecuenteEditable : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _pregunta = "";

    [ObservableProperty]
    private int _orden;

    [ObservableProperty]
    private bool _estaExpandida;

    [ObservableProperty]
    private bool _esNueva;

    [ObservableProperty]
    private ObservableCollection<RespuestaEditable> _respuestas = new();
}

/// <summary>
/// Clase auxiliar para edicion de respuestas en el formulario
/// </summary>
public partial class RespuestaEditable : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private int _idPregunta;

    [ObservableProperty]
    private string _respuesta = "";

    [ObservableProperty]
    private int _orden;

    [ObservableProperty]
    private bool _esNueva;
}
