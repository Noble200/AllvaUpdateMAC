using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Npgsql;
using Allva.Desktop.Models;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class UltimasNoticiasAdminViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        [ObservableProperty]
        private ObservableCollection<NoticiaItem> _noticias = new();

        [ObservableProperty]
        private NoticiaItem? _noticiaSeleccionada;

        [ObservableProperty]
        private bool _estaCargando;

        [ObservableProperty]
        private string _mensajeEstado = string.Empty;

        [ObservableProperty]
        private bool _hayMensaje;

        [ObservableProperty]
        private bool _mostrarPanelEdicion;

        [ObservableProperty]
        private bool _modoEdicion;

        [ObservableProperty]
        private string _tituloPanel = "Nueva Noticia";

        [ObservableProperty]
        private string _textoBotonGuardar = "Crear Noticia";

        // Campos del formulario
        [ObservableProperty]
        private string _tituloNoticia = string.Empty;

        [ObservableProperty]
        private string _categoriaNoticia = string.Empty;

        [ObservableProperty]
        private string _descripcionNoticia = string.Empty;

        [ObservableProperty]
        private string _contenidoNoticia = string.Empty;

        [ObservableProperty]
        private string _imagenUrl = string.Empty;

        [ObservableProperty]
        private string _enlaceUrl = string.Empty;

        [ObservableProperty]
        private Bitmap? _imagenPrevia;

        [ObservableProperty]
        private bool _tieneImagenPrevia;

        // Propiedades para el panel de encuadre
        [ObservableProperty]
        private bool _mostrarPanelEncuadre;

        [ObservableProperty]
        private double _cropAreaX = 0;

        [ObservableProperty]
        private double _cropAreaY = 26;

        [ObservableProperty]
        private double _cropAreaWidth = 600;

        [ObservableProperty]
        private double _cropAreaHeight = 348;

        [ObservableProperty]
        private double _imagenOriginalWidth = 400;

        [ObservableProperty]
        private double _imagenOriginalHeight = 300;

        // Zoom de la imagen (1.0 = 100%)
        [ObservableProperty]
        private double _zoomLevel = 1.0;

        // Posición de la imagen dentro del canvas (para mover con zoom)
        [ObservableProperty]
        private double _imagenOffsetX = 0;

        [ObservableProperty]
        private double _imagenOffsetY = 0;

        // Formatos disponibles para el encuadre
        [ObservableProperty]
        private ObservableCollection<string> _formatosDisponibles = new() { "Rectangular (345:200)", "Horizontal (1.91:1)" };

        [ObservableProperty]
        private string _formatoSeleccionado = "Rectangular (345:200)";

        // Proporciones del formato seleccionado
        // Rectangular es 1.725:1 para las noticias rectangulares (345x200) en el dashboard
        // Horizontal es 0.75:1 (3:4 vertical) para el carrusel vertical del dashboard
        public double ProporcionFormato => FormatoSeleccionado == "Rectangular (345:200)" ? 1.725 : 0.75;

        // Tamaño final de exportación optimizado
        // Rectangular: 690x400 (proporción 1.725:1 para tarjetas 345x200 del dashboard)
        // Horizontal: 600x800 (proporción 0.75, vertical)
        public int AnchoExportacion => FormatoSeleccionado == "Rectangular (345:200)" ? 690 : 600;
        public int AltoExportacion => FormatoSeleccionado == "Rectangular (345:200)" ? 400 : 800;

        // Margenes para el recuadro de recorte (calculados)
        public Avalonia.Thickness CropAreaMargin => new Avalonia.Thickness(CropAreaX, CropAreaY, 0, 0);

        // Margen para posicionar el Canvas de la imagen del crop
        // El canvas de 600x400 debe desplazarse para que su origen coincida con (0,0) del canvas principal
        public Avalonia.Thickness CropImageMargin => new Avalonia.Thickness(-CropAreaX, -CropAreaY, 0, 0);

        // Ambas imágenes (fondo y crop) usan RenderTransformOrigin="0,0"
        // Esto significa que la imagen se escala desde la esquina superior izquierda
        // Para mantener el centro visual fijo durante el zoom, desplazamos la imagen
        // Con zoom Z, el centro (300, 200) debe verse en el mismo lugar, entonces:
        // - Offset X = 300 * (1 - Z)
        // - Offset Y = 200 * (1 - Z)
        public double ImagenBackgroundLeftConZoom => 300 * (1 - ZoomLevel);
        public double ImagenBackgroundTopConZoom => 200 * (1 - ZoomLevel);

        // Para el crop SIN zoom (propiedades antiguas)
        public double CropImageOffsetX => -CropAreaX;
        public double CropImageOffsetY => -CropAreaY;

        // Para el crop CON zoom - la imagen del crop usa RenderTransformOrigin="0,0"
        // Necesitamos que coincida con la imagen de fondo que usa RenderTransformOrigin="0.5,0.5"
        // La imagen de fondo después del scale Z tiene su borde izquierdo en: 300*(1-Z)
        // La imagen del crop debe empezar en la misma posición relativa al área de crop
        public double CropImageOffsetXConZoom => 300 * (1 - ZoomLevel) - CropAreaX;
        public double CropImageOffsetYConZoom => 200 * (1 - ZoomLevel) - CropAreaY;

        partial void OnFormatoSeleccionadoChanged(string value)
        {
            // Actualizar el tamaño del área de recorte manteniendo la proporción
            ActualizarTamanioCrop();
            OnPropertyChanged(nameof(ProporcionFormato));
            OnPropertyChanged(nameof(AnchoExportacion));
            OnPropertyChanged(nameof(AltoExportacion));
        }

        private void ActualizarTamanioCrop()
        {
            var proporcion = ProporcionFormato;

            // El área de crop tiene tamaño FIJO basado en el canvas (no en la imagen)
            // Esto representa exactamente el área final de la noticia
            double newWidth, newHeight;

            // Calcular el tamaño máximo que cabe en el canvas manteniendo la proporción
            if (CANVAS_WIDTH / proporcion <= CANVAS_HEIGHT)
            {
                // Limitado por el ancho del canvas
                newWidth = CANVAS_WIDTH;
                newHeight = CANVAS_WIDTH / proporcion;
            }
            else
            {
                // Limitado por el alto del canvas
                newHeight = CANVAS_HEIGHT;
                newWidth = CANVAS_HEIGHT * proporcion;
            }

            CropAreaWidth = newWidth;
            CropAreaHeight = newHeight;

            // Centrar el crop en el canvas
            CropAreaX = (CANVAS_WIDTH - CropAreaWidth) / 2;
            CropAreaY = (CANVAS_HEIGHT - CropAreaHeight) / 2;

            NotificarCambioCrop();
        }

        [ObservableProperty]
        private string _estadoSeleccionado = "Activa";

        [ObservableProperty]
        private ObservableCollection<string> _estadosDisponibles = new() { "Activa", "Inactiva", "Borrador" };

        [ObservableProperty]
        private string _tipoNoticiaSeleccionado = "cuadrada";

        [ObservableProperty]
        private ObservableCollection<string> _tiposNoticiaDisponibles = new() { "cuadrada", "carrusel", "comunicacion" };

        // Icono y Color para comunicaciones del sistema
        [ObservableProperty]
        private string _iconoComunicacionSeleccionado = "ICONOS allva-05.png";

        [ObservableProperty]
        private string _colorComunicacionSeleccionado = "naranja";

        [ObservableProperty]
        private ObservableCollection<IconoComunicacionItem> _iconosComunicacionDisponibles = new()
        {
            new IconoComunicacionItem { NombreArchivo = "ICONOS allva-03.png", Descripcion = "Campana", RutaAsset = "/Assets/Noticias/ICONOS allva-03.png" },
            new IconoComunicacionItem { NombreArchivo = "ICONOS allva-04.png", Descripcion = "Información", RutaAsset = "/Assets/Noticias/ICONOS allva-04.png" },
            new IconoComunicacionItem { NombreArchivo = "ICONOS allva-05.png", Descripcion = "Alerta", RutaAsset = "/Assets/Noticias/ICONOS allva-05.png" },
            new IconoComunicacionItem { NombreArchivo = "ICONOS allva-06.png", Descripcion = "Sistema", RutaAsset = "/Assets/Noticias/ICONOS allva-06.png" },
            new IconoComunicacionItem { NombreArchivo = "ICONOS allva-07.png", Descripcion = "Advertencia", RutaAsset = "/Assets/Noticias/ICONOS allva-07.png" },
            new IconoComunicacionItem { NombreArchivo = "ICONOS allva-08.png", Descripcion = "Enlace", RutaAsset = "/Assets/Noticias/ICONOS allva-08.png" },
            new IconoComunicacionItem { NombreArchivo = "ICONOS allva-09.png", Descripcion = "Pregunta", RutaAsset = "/Assets/Noticias/ICONOS allva-09.png" },
        };

        [ObservableProperty]
        private ObservableCollection<ColorComunicacionItem> _coloresComunicacionDisponibles = new()
        {
            new ColorComunicacionItem { Clave = "rojo", Nombre = "Rojo - Alerta", ColorFondo = "#FFCDD2", ColorTexto = "#C62828", ColorIcono = "#EF5350" },
            new ColorComunicacionItem { Clave = "naranja", Nombre = "Naranja - Importante", ColorFondo = "#FFE0B2", ColorTexto = "#E65100", ColorIcono = "#FF7043" },
            new ColorComunicacionItem { Clave = "amarillo", Nombre = "Amarillo - Aviso", ColorFondo = "#FFF9C4", ColorTexto = "#F57F17", ColorIcono = "#FFD54F" },
            new ColorComunicacionItem { Clave = "azul", Nombre = "Azul - Información", ColorFondo = "#BBDEFB", ColorTexto = "#0D47A1", ColorIcono = "#42A5F5" },
            new ColorComunicacionItem { Clave = "verde", Nombre = "Verde - Novedad", ColorFondo = "#C8E6C9", ColorTexto = "#1B5E20", ColorIcono = "#66BB6A" },
            new ColorComunicacionItem { Clave = "morado", Nombre = "Morado - Evento", ColorFondo = "#E1BEE7", ColorTexto = "#4A148C", ColorIcono = "#AB47BC" },
        };

        // Propiedades calculadas para el icono seleccionado
        public IconoComunicacionItem? IconoSeleccionadoItem =>
            IconosComunicacionDisponibles.FirstOrDefault(i => i.NombreArchivo == IconoComunicacionSeleccionado);

        public ColorComunicacionItem? ColorSeleccionadoItem =>
            ColoresComunicacionDisponibles.FirstOrDefault(c => c.Clave == ColorComunicacionSeleccionado);

        // Visibilidad del panel de icono/color (solo para comunicaciones)
        public bool MostrarOpcionesIconoColor => TipoNoticiaSeleccionado == "comunicacion";

        partial void OnIconoComunicacionSeleccionadoChanged(string value)
        {
            OnPropertyChanged(nameof(IconoSeleccionadoItem));
        }

        partial void OnColorComunicacionSeleccionadoChanged(string value)
        {
            OnPropertyChanged(nameof(ColorSeleccionadoItem));
        }

        // Descripción y colores según el tipo de noticia seleccionado
        public string TipoNoticiaDescripcion => TipoNoticiaSeleccionado switch
        {
            "cuadrada" => "📰 Grid 2x2 en el dashboard. Máximo 4 noticias destacadas. Imagen cuadrada (1:1).",
            "carrusel" => "🎠 Rotativo vertical en el panel derecho. Imagen rectangular horizontal.",
            "comunicacion" => "📢 Alertas importantes en la parte inferior. No requiere imagen.",
            _ => ""
        };

        // Visibilidad de vistas previas según tipo de noticia
        public bool MostrarPreviewCuadrada => TipoNoticiaSeleccionado == "cuadrada";
        public bool MostrarPreviewCarrusel => TipoNoticiaSeleccionado == "carrusel";
        public bool MostrarPreviewComunicacion => TipoNoticiaSeleccionado == "comunicacion";

        public IBrush TipoNoticiaBackground => TipoNoticiaSeleccionado switch
        {
            "cuadrada" => new SolidColorBrush(Color.Parse("#E8F4FD")),
            "carrusel" => new SolidColorBrush(Color.Parse("#FFF8E1")),
            "comunicacion" => new SolidColorBrush(Color.Parse("#FFEBEE")),
            _ => new SolidColorBrush(Color.Parse("#F5F5F5"))
        };

        public IBrush TipoNoticiaForeground => TipoNoticiaSeleccionado switch
        {
            "cuadrada" => new SolidColorBrush(Color.Parse("#0b5394")),
            "carrusel" => new SolidColorBrush(Color.Parse("#E65100")),
            "comunicacion" => new SolidColorBrush(Color.Parse("#d35400")),
            _ => new SolidColorBrush(Color.Parse("#333333"))
        };

        partial void OnTipoNoticiaSeleccionadoChanged(string value)
        {
            OnPropertyChanged(nameof(TipoNoticiaDescripcion));
            OnPropertyChanged(nameof(TipoNoticiaBackground));
            OnPropertyChanged(nameof(TipoNoticiaForeground));
            OnPropertyChanged(nameof(MostrarPreviewCuadrada));
            OnPropertyChanged(nameof(MostrarPreviewCarrusel));
            OnPropertyChanged(nameof(MostrarPreviewComunicacion));
            OnPropertyChanged(nameof(MostrarOpcionVideo));
            OnPropertyChanged(nameof(MostrarOpcionesIconoColor));
            // Actualizar formato de encuadre automáticamente
            ActualizarFormatoSegunTipo();
        }

        private void ActualizarFormatoSegunTipo()
        {
            FormatoSeleccionado = TipoNoticiaSeleccionado switch
            {
                "cuadrada" => "Rectangular (345:200)",
                "carrusel" => "Horizontal (1.91:1)",
                _ => "Rectangular (345:200)"
            };
        }

        [ObservableProperty]
        private bool _mostrarImagenesEnLista = false;

        // Noticias ordenadas: destacadas primero (por orden), luego no destacadas (por orden)
        public IEnumerable<NoticiaItem> NoticiasOrdenadas =>
            Noticias.OrderByDescending(n => n.EsDestacada).ThenBy(n => n.Orden).ThenByDescending(n => n.FechaCreacion);

        // Noticias destacadas (cuadradas) - las del grid 2x2
        public IEnumerable<NoticiaItem> NoticiasDestacadas =>
            Noticias.Where(n => n.TipoNoticia == "cuadrada")
                    .OrderBy(n => n.Orden)
                    .Select((n, index) => { n.PosicionDestacada = index + 1; return n; });

        // Noticias de carrusel
        public IEnumerable<NoticiaItem> NoticiasCarrusel =>
            Noticias.Where(n => n.TipoNoticia == "carrusel")
                    .OrderBy(n => n.Orden);

        // Comunicaciones del sistema
        public IEnumerable<NoticiaItem> NoticiasComunicaciones =>
            Noticias.Where(n => n.TipoNoticia == "comunicacion")
                    .OrderBy(n => n.Orden);

        // Contadores para mostrar/ocultar secciones
        public bool TieneNoticiasDestacadas => NoticiasDestacadas.Any();
        public bool TieneNoticiasCarrusel => NoticiasCarrusel.Any();
        public bool TieneNoticiasComunicaciones => NoticiasComunicaciones.Any();

        partial void OnNoticiasChanged(ObservableCollection<NoticiaItem> value)
        {
            OnPropertyChanged(nameof(NoticiasOrdenadas));
            OnPropertyChanged(nameof(NoticiasDestacadas));
            OnPropertyChanged(nameof(NoticiasCarrusel));
            OnPropertyChanged(nameof(NoticiasComunicaciones));
            OnPropertyChanged(nameof(TieneNoticiasDestacadas));
            OnPropertyChanged(nameof(TieneNoticiasCarrusel));
            OnPropertyChanged(nameof(TieneNoticiasComunicaciones));
        }

        public string TextoBotonImagenes => MostrarImagenesEnLista ? "Ocultar Imagenes" : "Mostrar Imagenes";

        partial void OnMostrarImagenesEnListaChanged(bool value)
        {
            OnPropertyChanged(nameof(TextoBotonImagenes));
        }

        [RelayCommand]
        private void ToggleMostrarImagenes()
        {
            MostrarImagenesEnLista = !MostrarImagenesEnLista;
            // Guardar preferencia
            FrontOfficeConfigService.Instance.GuardarPreferenciaMostrarImagenesNoticias(MostrarImagenesEnLista);
        }

        // Metodo para subir una noticia (moverla hacia arriba dentro de su grupo por tipo)
        public async Task SubirNoticiaAsync(NoticiaItem noticia)
        {
            if (noticia == null) return;

            // Obtener solo las noticias del mismo tipo
            var listaGrupo = Noticias
                .Where(n => n.TipoNoticia == noticia.TipoNoticia)
                .OrderBy(n => n.Orden)
                .ToList();

            var index = listaGrupo.IndexOf(noticia);

            if (index <= 0) return; // Ya está en primer lugar de su grupo

            var noticiaAnterior = listaGrupo[index - 1];
            await IntercambiarOrdenAsync(noticia, noticiaAnterior);
        }

        // Metodo para bajar una noticia (moverla hacia abajo dentro de su grupo por tipo)
        public async Task BajarNoticiaAsync(NoticiaItem noticia)
        {
            if (noticia == null) return;

            // Obtener solo las noticias del mismo tipo
            var listaGrupo = Noticias
                .Where(n => n.TipoNoticia == noticia.TipoNoticia)
                .OrderBy(n => n.Orden)
                .ToList();

            var index = listaGrupo.IndexOf(noticia);

            if (index >= listaGrupo.Count - 1) return; // Ya está en último lugar de su grupo

            var noticiaSiguiente = listaGrupo[index + 1];
            await IntercambiarOrdenAsync(noticia, noticiaSiguiente);
        }

        // Intercambiar el orden de dos noticias
        private async Task IntercambiarOrdenAsync(NoticiaItem noticia1, NoticiaItem noticia2)
        {
            try
            {
                // Intercambiar órdenes en memoria
                var ordenTemp = noticia1.Orden;
                noticia1.Orden = noticia2.Orden;
                noticia2.Orden = ordenTemp;

                // Actualizar en BD
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var cmd1 = new NpgsqlCommand("UPDATE noticias SET orden = @orden WHERE id = @id", connection);
                cmd1.Parameters.AddWithValue("@orden", noticia1.Orden);
                cmd1.Parameters.AddWithValue("@id", noticia1.Id);
                await cmd1.ExecuteNonQueryAsync();

                var cmd2 = new NpgsqlCommand("UPDATE noticias SET orden = @orden WHERE id = @id", connection);
                cmd2.Parameters.AddWithValue("@orden", noticia2.Orden);
                cmd2.Parameters.AddWithValue("@id", noticia2.Id);
                await cmd2.ExecuteNonQueryAsync();

                // Actualizar estado de las flechas y refrescar la vista
                ActualizarEstadoFlechas();
                OnPropertyChanged(nameof(NoticiasOrdenadas));
                OnPropertyChanged(nameof(NoticiasDestacadas));
                OnPropertyChanged(nameof(NoticiasCarrusel));
                OnPropertyChanged(nameof(NoticiasComunicaciones));
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al mover noticia: {ex.Message}", true);
            }
        }

        // Actualizar el estado de las flechas (habilitar/deshabilitar según posición dentro de cada grupo por tipo)
        private void ActualizarEstadoFlechas()
        {
            if (Noticias == null || Noticias.Count == 0) return;

            try
            {
                // Grupo de noticias cuadradas (destacadas)
                var cuadradas = Noticias.Where(n => n.TipoNoticia == "cuadrada").OrderBy(n => n.Orden).ToList();
                for (int i = 0; i < cuadradas.Count; i++)
                {
                    cuadradas[i].PuedeSubir = i > 0;
                    cuadradas[i].PuedeBajar = i < cuadradas.Count - 1;
                    cuadradas[i].PosicionDestacada = i + 1;
                }

                // Grupo de noticias de carrusel
                var carrusel = Noticias.Where(n => n.TipoNoticia == "carrusel").OrderBy(n => n.Orden).ToList();
                for (int i = 0; i < carrusel.Count; i++)
                {
                    carrusel[i].PuedeSubir = i > 0;
                    carrusel[i].PuedeBajar = i < carrusel.Count - 1;
                }

                // Grupo de comunicaciones del sistema
                var comunicaciones = Noticias.Where(n => n.TipoNoticia == "comunicacion").OrderBy(n => n.Orden).ToList();
                for (int i = 0; i < comunicaciones.Count; i++)
                {
                    comunicaciones[i].PuedeSubir = i > 0;
                    comunicaciones[i].PuedeBajar = i < comunicaciones.Count - 1;
                }
            }
            catch (Exception)
            {
                // Ignorar errores al actualizar estado de flechas
            }
        }

        private int? _noticiaEditandoId;
        private byte[]? _imagenData;
        private byte[]? _imagenOriginalData; // Imagen original sin recortar para poder reencuadrar

        // Imagen original para mostrar en el panel de encuadre (siempre la original, no la recortada)
        [ObservableProperty]
        private Bitmap? _imagenOriginalParaEncuadre;

        // Propiedades para video (solo carrusel)
        private byte[]? _videoData;
        private bool _videoModificado = false;

        [ObservableProperty]
        private bool _tieneVideo;

        [ObservableProperty]
        private string _nombreVideo = string.Empty;

        // Crop del video (valores normalizados 0-1 relativos al frame del video)
        private double _videoCropX, _videoCropY, _videoCropW, _videoCropH;
        private bool _encuadrandoVideo = false; // Flag: el crop tool está encuadrando video (no imagen)

        [ObservableProperty]
        private bool _tieneFrameVideo; // Indica si se extrajo un frame para encuadrar

        public bool MostrarOpcionVideo => TipoNoticiaSeleccionado == "carrusel";

        public IBrush MensajeBackground => HayMensaje
            ? (MensajeEsError ? Brushes.Red : new SolidColorBrush(Color.Parse("#28a745")))
            : Brushes.Transparent;

        private bool _mensajeEsError;
        public bool MensajeEsError
        {
            get => _mensajeEsError;
            set => SetProperty(ref _mensajeEsError, value);
        }

        public UltimasNoticiasAdminViewModel()
        {
            try
            {
                // Cargar preferencia de mostrar imagenes
                MostrarImagenesEnLista = FrontOfficeConfigService.Instance.ObtenerPreferenciaMostrarImagenesNoticias();
                _ = InicializarAsync();
            }
            catch (Exception)
            {
                // Ignorar errores de inicialización
            }
        }

        private async Task InicializarAsync()
        {
            await AsegurarColumnaVideoAsync();
            await CargarNoticiasAsync();
        }

        private async Task AsegurarColumnaVideoAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                var cmd = new NpgsqlCommand(@"
                    ALTER TABLE noticias ADD COLUMN IF NOT EXISTS video_data bytea;
                    ALTER TABLE noticias ADD COLUMN IF NOT EXISTS video_crop_x double precision DEFAULT 0;
                    ALTER TABLE noticias ADD COLUMN IF NOT EXISTS video_crop_y double precision DEFAULT 0;
                    ALTER TABLE noticias ADD COLUMN IF NOT EXISTS video_crop_w double precision DEFAULT 0;
                    ALTER TABLE noticias ADD COLUMN IF NOT EXISTS video_crop_h double precision DEFAULT 0;
                    ALTER TABLE noticias ADD COLUMN IF NOT EXISTS icono_comunicacion text DEFAULT 'ICONOS allva-05.png';
                    ALTER TABLE noticias ADD COLUMN IF NOT EXISTS color_comunicacion text DEFAULT 'naranja';
                ", connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }

        private async Task CargarNoticiasAsync()
        {
            EstaCargando = true;
            Noticias.Clear();

            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    SELECT id, titulo, categoria, descripcion, contenido, imagen_url, estado, es_destacada, orden,
                           fecha_publicacion, fecha_creacion, imagen_data, imagen_crop_x, imagen_crop_y, imagen_crop_width, imagen_crop_height,
                           enlace_url, tipo_noticia, imagen_original_data, (video_data IS NOT NULL) as tiene_video,
                           video_crop_x, video_crop_y, video_crop_w, video_crop_h,
                           icono_comunicacion, color_comunicacion
                    FROM noticias
                    ORDER BY orden ASC, fecha_creacion DESC", connection);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Noticias.Add(new NoticiaItem
                    {
                        Id = reader.GetInt32(0),
                        Titulo = reader.GetString(1),
                        Categoria = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Descripcion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Contenido = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        ImagenUrl = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        Estado = reader.IsDBNull(6) ? "Activa" : reader.GetString(6),
                        EsDestacada = !reader.IsDBNull(7) && reader.GetBoolean(7),
                        Orden = reader.IsDBNull(8) ? 1 : reader.GetInt32(8),
                        FechaPublicacion = reader.IsDBNull(9) ? DateTime.Now : reader.GetDateTime(9),
                        FechaCreacion = reader.IsDBNull(10) ? DateTime.Now : reader.GetDateTime(10),
                        ImagenData = reader.IsDBNull(11) ? null : (byte[])reader[11],
                        ImagenCropX = reader.IsDBNull(12) ? 0 : reader.GetDouble(12),
                        ImagenCropY = reader.IsDBNull(13) ? 0 : reader.GetDouble(13),
                        ImagenCropWidth = reader.IsDBNull(14) ? 0 : reader.GetDouble(14),
                        ImagenCropHeight = reader.IsDBNull(15) ? 0 : reader.GetDouble(15),
                        EnlaceUrl = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                        TipoNoticia = reader.IsDBNull(17) ? "cuadrada" : reader.GetString(17),
                        ImagenOriginalData = reader.IsDBNull(18) ? null : (byte[])reader[18],
                        TieneVideoGuardado = !reader.IsDBNull(19) && reader.GetBoolean(19),
                        VideoCropX = reader.IsDBNull(20) ? 0 : reader.GetDouble(20),
                        VideoCropY = reader.IsDBNull(21) ? 0 : reader.GetDouble(21),
                        VideoCropW = reader.IsDBNull(22) ? 0 : reader.GetDouble(22),
                        VideoCropH = reader.IsDBNull(23) ? 0 : reader.GetDouble(23),
                        IconoComunicacion = reader.IsDBNull(24) ? "ICONOS allva-05.png" : reader.GetString(24),
                        ColorComunicacion = reader.IsDBNull(25) ? "naranja" : reader.GetString(25)
                    });
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar noticias: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
                ActualizarEstadoFlechas();
                OnPropertyChanged(nameof(NoticiasOrdenadas));
                OnPropertyChanged(nameof(NoticiasDestacadas));
                OnPropertyChanged(nameof(NoticiasCarrusel));
                OnPropertyChanged(nameof(NoticiasComunicaciones));
                OnPropertyChanged(nameof(TieneNoticiasDestacadas));
                OnPropertyChanged(nameof(TieneNoticiasCarrusel));
                OnPropertyChanged(nameof(TieneNoticiasComunicaciones));
            }
        }

        [RelayCommand]
        private void NuevaNoticia()
        {
            ModoEdicion = false;
            TituloPanel = "Nueva Noticia";
            TextoBotonGuardar = "Crear Noticia";
            LimpiarFormulario();
            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private async Task SeleccionarImagen()
        {
            try
            {
                var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Seleccionar Imagen",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Imagenes")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    var file = files[0];
                    ImagenUrl = file.Path.LocalPath;

                    await using var stream = await file.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    _imagenData = memoryStream.ToArray();
                    _imagenOriginalData = memoryStream.ToArray(); // Guardar copia de la original

                    memoryStream.Position = 0;
                    ImagenPrevia = new Bitmap(memoryStream);
                    TieneImagenPrevia = true;
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al seleccionar imagen: {ex.Message}", true);
            }
        }

        [RelayCommand]
        private async Task SeleccionarVideo()
        {
            try
            {
                var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Seleccionar Video",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Videos")
                        {
                            Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.webm" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    var file = files[0];
                    NombreVideo = file.Name;

                    await using var stream = await file.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var datos = memoryStream.ToArray();

                    // Límite de 30MB - la DB está online y cada dispositivo descarga el video
                    const long LIMITE_MB = 30;
                    if (datos.Length > LIMITE_MB * 1024 * 1024)
                    {
                        MostrarMensaje($"El video pesa {datos.Length / 1024 / 1024}MB. El máximo permitido es {LIMITE_MB}MB para no afectar la velocidad de carga.", true);
                        return;
                    }

                    _videoData = datos;
                    _videoModificado = true;
                    TieneVideo = true;

                    MostrarMensaje($"Video cargado: {NombreVideo} ({_videoData.Length / 1024 / 1024}MB)", false);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al seleccionar video: {ex.Message}", true);
            }
        }

        [RelayCommand]
        private void EliminarVideo()
        {
            _videoData = null;
            _videoModificado = true;
            TieneVideo = false;
            TieneFrameVideo = false;
            NombreVideo = string.Empty;
            _videoCropX = _videoCropY = _videoCropW = _videoCropH = 0;
        }

        [RelayCommand]
        private async Task EncuadrarVideo()
        {
            // Si no tenemos el video en memoria pero existe en DB, descargarlo
            if ((_videoData == null || _videoData.Length == 0) && ModoEdicion && _noticiaEditandoId.HasValue)
            {
                MostrarMensaje("Descargando video para encuadrar...", false);
                try
                {
                    await using var conn = new NpgsqlConnection(ConnectionString);
                    await conn.OpenAsync();
                    var cmdVideo = new NpgsqlCommand("SELECT video_data FROM noticias WHERE id = @id", conn);
                    cmdVideo.Parameters.AddWithValue("@id", _noticiaEditandoId.Value);
                    var result = await cmdVideo.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                        _videoData = (byte[])result;
                }
                catch (Exception ex)
                {
                    MostrarMensaje($"Error al descargar video: {ex.Message}", true);
                    return;
                }
            }

            if (_videoData == null || _videoData.Length == 0)
            {
                MostrarMensaje("Primero selecciona un video", true);
                return;
            }

            MostrarMensaje("Extrayendo frame del video...", false);

            var frame = await ExtraerFrameVideoAsync(_videoData);
            if (frame == null)
            {
                MostrarMensaje("No se pudo extraer un frame del video", true);
                return;
            }

            // Cargar el frame en el panel de encuadre (igual que una imagen)
            _encuadrandoVideo = true;
            ImagenOriginalParaEncuadre = frame;
            ImagenOriginalWidth = frame.PixelSize.Width;
            ImagenOriginalHeight = frame.PixelSize.Height;

            // Resetear zoom y offset
            ZoomLevel = 1.0;
            ImagenOffsetX = 0;
            ImagenOffsetY = 0;

            // Para video de carrusel, usar formato Horizontal (1.91:1) que en realidad es portrait 0.75
            FormatoSeleccionado = "Horizontal (1.91:1)";
            ActualizarTamanioCrop();

            // Auto-zoom para que el frame del video LLENE el área de crop
            // Sin esto, para videos portrait el crop area es más grande que la imagen
            // y mover el rectángulo no cambia qué parte del video se selecciona
            double baseScaleX = CANVAS_WIDTH / (double)frame.PixelSize.Width;
            double baseScaleY = CANVAS_HEIGHT / (double)frame.PixelSize.Height;
            double baseScale = Math.Min(baseScaleX, baseScaleY);
            double scaledWidthBase = frame.PixelSize.Width * baseScale;
            double scaledHeightBase = frame.PixelSize.Height * baseScale;
            double zoomParaAncho = CropAreaWidth / scaledWidthBase;
            double zoomParaAlto = CropAreaHeight / scaledHeightBase;
            ZoomLevel = Math.Max(zoomParaAncho, zoomParaAlto);
            ZoomLevel = Math.Max(0.5, Math.Min(3.0, ZoomLevel));

            NotificarCambioZoom();
            NotificarCambioCrop();
            MostrarPanelEncuadre = true;
            TieneFrameVideo = true;
        }

        private async Task<Bitmap?> ExtraerFrameVideoAsync(byte[] videoData)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"allva_vthumb_{Guid.NewGuid():N}.mp4");
            try
            {
                await File.WriteAllBytesAsync(tempPath, videoData);

                Core.Initialize();
                using var libVLC = new LibVLC("--no-audio");

                // Parsear media para obtener dimensiones reales del video
                uint W = 1280, H = 720;
                using (var mediaParse = new Media(libVLC, tempPath, FromType.FromPath))
                {
                    await mediaParse.Parse(MediaParseOptions.ParseLocal, timeout: 5000);
                    foreach (var track in mediaParse.Tracks)
                    {
                        if (track.TrackType == TrackType.Video)
                        {
                            W = track.Data.Video.Width;
                            H = track.Data.Video.Height;
                            break;
                        }
                    }
                }

                // Limitar dimensiones para rendimiento
                if (W > 1920 || H > 1920)
                {
                    double scale = 1920.0 / Math.Max(W, H);
                    W = (uint)(W * scale);
                    H = (uint)(H * scale);
                }
                // Asegurar dimensiones pares (requerido por VLC)
                W = (W / 2) * 2;
                H = (H / 2) * 2;
                if (W < 2) W = 2;
                if (H < 2) H = 2;

                uint P = W * 4;
                int bufSize = (int)(P * H);

                using var mediaPlayer = new MediaPlayer(libVLC);
                var buffer = Marshal.AllocHGlobal(bufSize);
                var frameReady = new TaskCompletionSource<byte[]>();

                try
                {
                    mediaPlayer.SetVideoFormat("RV32", W, H, P);
                    mediaPlayer.SetVideoCallbacks(
                        (opaque, planes) =>
                        {
                            Marshal.WriteIntPtr(planes, buffer);
                            return IntPtr.Zero;
                        },
                        (opaque, picture, planes) => { },
                        (opaque, picture) =>
                        {
                            if (!frameReady.Task.IsCompleted)
                            {
                                var data = new byte[bufSize];
                                Marshal.Copy(buffer, data, 0, bufSize);
                                frameReady.TrySetResult(data);
                            }
                        }
                    );

                    using var media = new Media(libVLC, tempPath, FromType.FromPath);
                    mediaPlayer.Play(media);

                    // Esperar máximo 5 segundos por el primer frame
                    var completedTask = await Task.WhenAny(frameReady.Task, Task.Delay(5000));

                    mediaPlayer.Stop();

                    if (completedTask == frameReady.Task)
                    {
                        var frameData = frameReady.Task.Result;

                        // Crear WriteableBitmap con las dimensiones REALES del video
                        var wb = new WriteableBitmap(
                            new Avalonia.PixelSize((int)W, (int)H),
                            new Avalonia.Vector(96, 96),
                            Avalonia.Platform.PixelFormat.Bgra8888,
                            Avalonia.Platform.AlphaFormat.Premul);

                        using var locked = wb.Lock();
                        Marshal.Copy(frameData, 0, locked.Address, frameData.Length);

                        // Convertir a Bitmap normal
                        using var ms = new MemoryStream();
                        wb.Save(ms);
                        ms.Position = 0;
                        return new Bitmap(ms);
                    }

                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al extraer frame: {ex.Message}", true);
                return null;
            }
            finally
            {
                try { File.Delete(tempPath); } catch { }
            }
        }

        // Constantes del canvas de encuadre
        private const double CANVAS_WIDTH = 600;
        private const double CANVAS_HEIGHT = 400;
        private const double MIN_CROP_SIZE = 50;

        [RelayCommand]
        private void AbrirEncuadre()
        {
            if (!TieneImagenPrevia) return;

            // Cargar la imagen ORIGINAL para el encuadre (no la recortada)
            if (_imagenOriginalData != null)
            {
                using var ms = new MemoryStream(_imagenOriginalData);
                ImagenOriginalParaEncuadre = new Bitmap(ms);
            }
            else if (_imagenData != null)
            {
                using var ms = new MemoryStream(_imagenData);
                ImagenOriginalParaEncuadre = new Bitmap(ms);
            }
            else if (ImagenPrevia != null)
            {
                ImagenOriginalParaEncuadre = ImagenPrevia;
            }
            else
            {
                return;
            }

            // Configurar dimensiones de la imagen ORIGINAL (no la recortada)
            ImagenOriginalWidth = ImagenOriginalParaEncuadre.PixelSize.Width;
            ImagenOriginalHeight = ImagenOriginalParaEncuadre.PixelSize.Height;

            // Resetear zoom y offset
            ZoomLevel = 1.0;
            ImagenOffsetX = 0;
            ImagenOffsetY = 0;

            // Configurar formato automáticamente según el tipo de noticia
            ActualizarFormatoSegunTipo();

            // Inicializar el área de recorte según el formato seleccionado
            ActualizarTamanioCrop();

            NotificarCambioZoom();
            NotificarCambioCrop();
            MostrarPanelEncuadre = true;
        }

        [RelayCommand]
        private void AumentarZoom()
        {
            // Zoom de la imagen (no del área de crop)
            if (ZoomLevel < 3.0)
            {
                ZoomLevel = Math.Min(3.0, ZoomLevel + 0.1);
                NotificarCambioZoom();
            }
        }

        [RelayCommand]
        private void DisminuirZoom()
        {
            // Zoom de la imagen (no del área de crop)
            if (ZoomLevel > 0.5)
            {
                ZoomLevel = Math.Max(0.5, ZoomLevel - 0.1);
                NotificarCambioZoom();
            }
        }

        // Método para ajustar zoom desde la rueda del ratón
        public void AjustarZoom(double delta)
        {
            var newZoom = ZoomLevel + delta;
            newZoom = Math.Max(0.5, Math.Min(3.0, newZoom));
            if (Math.Abs(newZoom - ZoomLevel) > 0.001)
            {
                ZoomLevel = newZoom;
                NotificarCambioZoom();
            }
        }

        // Notificar cambios relacionados con el zoom
        private void NotificarCambioZoom()
        {
            OnPropertyChanged(nameof(ZoomLevel));
            OnPropertyChanged(nameof(ImagenBackgroundLeftConZoom));
            OnPropertyChanged(nameof(ImagenBackgroundTopConZoom));
            OnPropertyChanged(nameof(CropImageOffsetXConZoom));
            OnPropertyChanged(nameof(CropImageOffsetYConZoom));
        }

        // Notificar cambios del área de crop
        private void NotificarCambioCrop()
        {
            OnPropertyChanged(nameof(CropAreaMargin));
            OnPropertyChanged(nameof(CropImageMargin));
            OnPropertyChanged(nameof(CropImageOffsetX));
            OnPropertyChanged(nameof(CropImageOffsetY));
            OnPropertyChanged(nameof(CropImageOffsetXConZoom));
            OnPropertyChanged(nameof(CropImageOffsetYConZoom));
        }

        [RelayCommand]
        private void EncuadrarAutomatico()
        {
            if (ImagenPrevia == null) return;

            // Calcular el zoom necesario para que la imagen llene el área de recorte
            var proporcionImagen = ImagenOriginalWidth / ImagenOriginalHeight;
            var proporcionCrop = CropAreaWidth / CropAreaHeight;

            double nuevoZoom;

            if (proporcionImagen > proporcionCrop)
            {
                // La imagen es más ancha que el crop, ajustar por altura
                nuevoZoom = CropAreaHeight / (CANVAS_HEIGHT * (ImagenOriginalHeight / Math.Max(ImagenOriginalWidth, ImagenOriginalHeight)));
            }
            else
            {
                // La imagen es más alta que el crop, ajustar por ancho
                nuevoZoom = CropAreaWidth / (CANVAS_WIDTH * (ImagenOriginalWidth / Math.Max(ImagenOriginalWidth, ImagenOriginalHeight)));
            }

            // Limitar zoom entre 0.5 y 3.0
            ZoomLevel = Math.Max(0.5, Math.Min(3.0, nuevoZoom));

            // Centrar la imagen en el crop
            ImagenOffsetX = 0;
            ImagenOffsetY = 0;

            // Centrar el crop en el canvas
            CropAreaX = (CANVAS_WIDTH - CropAreaWidth) / 2;
            CropAreaY = (CANVAS_HEIGHT - CropAreaHeight) / 2;

            NotificarCambioZoom();
            NotificarCambioCrop();
        }

        [RelayCommand]
        private void PosicionArriba()
        {
            // Posicionar el recuadro en la parte superior de la imagen (centrado horizontalmente)
            CropAreaX = (CANVAS_WIDTH - CropAreaWidth) / 2;
            CropAreaY = 0;
            NotificarCambioCrop();
        }

        [RelayCommand]
        private void PosicionCentro()
        {
            // Centrar el recuadro tanto horizontal como verticalmente
            CropAreaX = (CANVAS_WIDTH - CropAreaWidth) / 2;
            CropAreaY = (CANVAS_HEIGHT - CropAreaHeight) / 2;
            NotificarCambioCrop();
        }

        [RelayCommand]
        private void PosicionAbajo()
        {
            // Posicionar el recuadro en la parte inferior de la imagen (centrado horizontalmente)
            CropAreaX = (CANVAS_WIDTH - CropAreaWidth) / 2;
            CropAreaY = CANVAS_HEIGHT - CropAreaHeight;
            NotificarCambioCrop();
        }

        [RelayCommand]
        private void PosicionIzquierda()
        {
            // Posicionar el recuadro a la izquierda (mantiene posicion vertical)
            CropAreaX = 0;
            NotificarCambioCrop();
        }

        [RelayCommand]
        private void PosicionCentroHorizontal()
        {
            // Centrar el recuadro solo horizontalmente (mantiene posicion vertical)
            CropAreaX = (CANVAS_WIDTH - CropAreaWidth) / 2;
            NotificarCambioCrop();
        }

        [RelayCommand]
        private void PosicionDerecha()
        {
            // Posicionar el recuadro a la derecha (mantiene posicion vertical)
            CropAreaX = CANVAS_WIDTH - CropAreaWidth;
            NotificarCambioCrop();
        }

        [RelayCommand]
        private void CerrarEncuadre()
        {
            MostrarPanelEncuadre = false;
            _encuadrandoVideo = false;
        }

        [RelayCommand]
        private void ConfirmarEncuadre()
        {
            if (_encuadrandoVideo)
            {
                // Estamos encuadrando un VIDEO: calcular crop normalizado (0-1)
                if (ImagenOriginalParaEncuadre != null)
                {
                    CalcularCropVideoNormalizado();
                    MostrarMensaje($"Video encuadrado: X={_videoCropX:F2} Y={_videoCropY:F2} W={_videoCropW:F2} H={_videoCropH:F2}", false);
                }
                _encuadrandoVideo = false;
            }
            else
            {
                // Estamos encuadrando una IMAGEN: procesar y aplicar el crop
                if (ImagenOriginalParaEncuadre != null && (_imagenOriginalData != null || _imagenData != null))
                {
                    var imagenProcesada = ProcesarCropYRedimensionar();
                    if (imagenProcesada.HasValue)
                    {
                        ImagenPrevia = imagenProcesada.Value.Item1;
                        _imagenData = imagenProcesada.Value.Item2;
                        CropAreaX = 0;
                        CropAreaY = 0;
                        CropAreaWidth = 0;
                        CropAreaHeight = 0;
                    }
                }
                MostrarMensaje("Imagen recortada y optimizada correctamente", false);
            }

            MostrarPanelEncuadre = false;
        }

        /// <summary>
        /// Calcula las coordenadas de crop normalizadas (0-1) para el video,
        /// usando la misma lógica que ProcesarCropYRedimensionar pero sin generar imagen.
        /// </summary>
        private void CalcularCropVideoNormalizado()
        {
            double imgWidth = ImagenOriginalParaEncuadre!.PixelSize.Width;
            double imgHeight = ImagenOriginalParaEncuadre!.PixelSize.Height;

            // La imagen se escala con Stretch=Uniform para caber en 600x400
            double baseScaleX = CANVAS_WIDTH / imgWidth;
            double baseScaleY = CANVAS_HEIGHT / imgHeight;
            double baseScale = Math.Min(baseScaleX, baseScaleY);

            double scaledWidthBase = imgWidth * baseScale;
            double scaledHeightBase = imgHeight * baseScale;

            double offsetInContainerX = (CANVAS_WIDTH - scaledWidthBase) / 2;
            double offsetInContainerY = (CANVAS_HEIGHT - scaledHeightBase) / 2;

            double totalScale = baseScale * ZoomLevel;

            // Posición de la imagen en el canvas después del zoom
            double imgLeft = CANVAS_WIDTH / 2 + (offsetInContainerX - CANVAS_WIDTH / 2) * ZoomLevel;
            double imgTop = CANVAS_HEIGHT / 2 + (offsetInContainerY - CANVAS_HEIGHT / 2) * ZoomLevel;
            double scaledWidth = scaledWidthBase * ZoomLevel;
            double scaledHeight = scaledHeightBase * ZoomLevel;

            // Intersección del crop con la imagen
            double intersectLeft = Math.Max(CropAreaX, imgLeft);
            double intersectTop = Math.Max(CropAreaY, imgTop);
            double intersectRight = Math.Min(CropAreaX + CropAreaWidth, imgLeft + scaledWidth);
            double intersectBottom = Math.Min(CropAreaY + CropAreaHeight, imgTop + scaledHeight);

            if (intersectRight <= intersectLeft || intersectBottom <= intersectTop)
            {
                // Sin intersección, usar frame completo
                _videoCropX = 0; _videoCropY = 0; _videoCropW = 1; _videoCropH = 1;
                return;
            }

            // Coordenadas relativas a la imagen escalada
            double relX = intersectLeft - imgLeft;
            double relY = intersectTop - imgTop;
            double relW = intersectRight - intersectLeft;
            double relH = intersectBottom - intersectTop;

            // Convertir a coordenadas de la imagen original (en píxeles)
            double srcX = relX / totalScale;
            double srcY = relY / totalScale;
            double srcW = relW / totalScale;
            double srcH = relH / totalScale;

            // Clamp
            srcX = Math.Max(0, Math.Min(srcX, imgWidth));
            srcY = Math.Max(0, Math.Min(srcY, imgHeight));
            srcW = Math.Max(1, Math.Min(srcW, imgWidth - srcX));
            srcH = Math.Max(1, Math.Min(srcH, imgHeight - srcY));

            // Normalizar a 0-1
            _videoCropX = srcX / imgWidth;
            _videoCropY = srcY / imgHeight;
            _videoCropW = srcW / imgWidth;
            _videoCropH = srcH / imgHeight;
        }

        private (Bitmap, byte[])? ProcesarCropYRedimensionar()
        {
            try
            {
                // Usar la imagen original si existe, sino usar _imagenData
                var imagenParaProcesar = _imagenOriginalData ?? _imagenData;
                if (imagenParaProcesar == null) return null;

                // Cargar la imagen original
                using var ms = new MemoryStream(imagenParaProcesar);
                var imagenOriginal = new Bitmap(ms);

                double imgWidth = imagenOriginal.PixelSize.Width;
                double imgHeight = imagenOriginal.PixelSize.Height;

                // PASO 1: La imagen se escala con Stretch="Uniform" para caber en 600x400
                double baseScaleX = CANVAS_WIDTH / imgWidth;
                double baseScaleY = CANVAS_HEIGHT / imgHeight;
                double baseScale = Math.Min(baseScaleX, baseScaleY);

                // Tamaño de la imagen escalada SIN zoom
                double scaledWidthBase = imgWidth * baseScale;
                double scaledHeightBase = imgHeight * baseScale;

                // Posición de la imagen centrada en el contenedor de 600x400 (antes del zoom)
                double offsetInContainerX = (CANVAS_WIDTH - scaledWidthBase) / 2;
                double offsetInContainerY = (CANVAS_HEIGHT - scaledHeightBase) / 2;

                // PASO 2: El ScaleTransform escala todo desde el centro del canvas (300, 200)
                // La imagen escalada y su posición se multiplican por ZoomLevel
                double totalScale = baseScale * ZoomLevel;
                double scaledWidth = scaledWidthBase * ZoomLevel;
                double scaledHeight = scaledHeightBase * ZoomLevel;

                // La posición de la imagen en el canvas después del zoom (desde el centro)
                // Centro del canvas = (300, 200)
                // Offset desde el centro = offsetInContainer - (CANVAS/2)
                // Nueva posición = centro + offset * ZoomLevel
                double imgLeft = CANVAS_WIDTH / 2 + (offsetInContainerX - CANVAS_WIDTH / 2) * ZoomLevel;
                double imgTop = CANVAS_HEIGHT / 2 + (offsetInContainerY - CANVAS_HEIGHT / 2) * ZoomLevel;

                // Calcular la intersección entre el área de crop y la imagen en coordenadas del canvas
                double cropRight = CropAreaX + CropAreaWidth;
                double cropBottom = CropAreaY + CropAreaHeight;
                double imgRight = imgLeft + scaledWidth;
                double imgBottom = imgTop + scaledHeight;

                // Calcular el área de intersección (parte del crop que está sobre la imagen)
                double intersectLeft = Math.Max(CropAreaX, imgLeft);
                double intersectTop = Math.Max(CropAreaY, imgTop);
                double intersectRight = Math.Min(cropRight, imgRight);
                double intersectBottom = Math.Min(cropBottom, imgBottom);

                // Crear imagen recortada con el tamaño de exportación
                var anchoFinal = AnchoExportacion;
                var altoFinal = AltoExportacion;

                using var renderTarget = new Avalonia.Media.Imaging.RenderTargetBitmap(
                    new Avalonia.PixelSize(anchoFinal, altoFinal));

                using (var ctx = renderTarget.CreateDrawingContext())
                {
                    // Primero rellenar todo con blanco
                    ctx.FillRectangle(Avalonia.Media.Brushes.White, new Avalonia.Rect(0, 0, anchoFinal, altoFinal));

                    // Si hay intersección con la imagen, dibujar esa parte
                    if (intersectRight > intersectLeft && intersectBottom > intersectTop)
                    {
                        // Calcular la posición de la intersección relativa a la imagen (en coordenadas del canvas)
                        double relX = intersectLeft - imgLeft;
                        double relY = intersectTop - imgTop;
                        double intersectWidth = intersectRight - intersectLeft;
                        double intersectHeight = intersectBottom - intersectTop;

                        // Convertir a coordenadas de la imagen original (dividir por el totalScale)
                        int sourceX = (int)Math.Round(relX / totalScale);
                        int sourceY = (int)Math.Round(relY / totalScale);
                        int sourceWidth = (int)Math.Round(intersectWidth / totalScale);
                        int sourceHeight = (int)Math.Round(intersectHeight / totalScale);

                        // Asegurar límites válidos para la imagen origen
                        sourceX = Math.Max(0, Math.Min(sourceX, (int)imgWidth - 1));
                        sourceY = Math.Max(0, Math.Min(sourceY, (int)imgHeight - 1));
                        sourceWidth = Math.Max(1, Math.Min(sourceWidth, (int)imgWidth - sourceX));
                        sourceHeight = Math.Max(1, Math.Min(sourceHeight, (int)imgHeight - sourceY));

                        // Calcular dónde dibujar en el destino (la intersección puede no empezar en 0,0)
                        double destOffsetX = (intersectLeft - CropAreaX) / CropAreaWidth * anchoFinal;
                        double destOffsetY = (intersectTop - CropAreaY) / CropAreaHeight * altoFinal;
                        double destWidth = intersectWidth / CropAreaWidth * anchoFinal;
                        double destHeight = intersectHeight / CropAreaHeight * altoFinal;

                        var sourceRect = new Avalonia.Rect(sourceX, sourceY, sourceWidth, sourceHeight);
                        var destRect = new Avalonia.Rect(destOffsetX, destOffsetY, destWidth, destHeight);
                        ctx.DrawImage(imagenOriginal, sourceRect, destRect);
                    }
                    // Si no hay intersección, la imagen queda completamente blanca
                }

                // Convertir a bytes
                using var outputStream = new MemoryStream();
                renderTarget.Save(outputStream);
                var imagenBytes = outputStream.ToArray();

                // Crear Bitmap para vista previa
                outputStream.Position = 0;
                var imagenBitmap = new Bitmap(outputStream);

                return (imagenBitmap, imagenBytes);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al procesar imagen: {ex.Message}", true);
                return null;
            }
        }

        public void MoverCropArea(double deltaX, double deltaY)
        {
            // Movimiento libre pero manteniendo al menos 50px visibles dentro del canvas
            var newX = CropAreaX + deltaX;
            var newY = CropAreaY + deltaY;

            // Limitar para que al menos 50px del crop sean visibles en el canvas
            newX = Math.Max(-CropAreaWidth + 50, Math.Min(newX, CANVAS_WIDTH - 50));
            newY = Math.Max(-CropAreaHeight + 50, Math.Min(newY, CANVAS_HEIGHT - 50));

            CropAreaX = newX;
            CropAreaY = newY;
            NotificarCambioCrop();
        }

        // Método para redimensionar el área de recorte manteniendo la proporción
        public void RedimensionarCropArea(string esquina, double deltaX, double deltaY)
        {
            // Calcular el delta basado en la diagonal (promedio de X e Y) para mantener proporción
            double delta = (Math.Abs(deltaX) + Math.Abs(deltaY)) / 2;

            // Determinar la dirección según la esquina
            double signoX = esquina.Contains("Right") || esquina == "SE" || esquina == "NE" ? 1 : -1;
            double signoY = esquina.Contains("Bottom") || esquina == "SE" || esquina == "SW" ? 1 : -1;

            // Si el movimiento es hacia afuera (agrandar), usar delta positivo
            // Si es hacia adentro (achicar), usar delta negativo
            bool agrandar = (deltaX * signoX > 0) || (deltaY * signoY > 0);
            delta = agrandar ? delta : -delta;

            // Calcular nuevo tamaño manteniendo la proporción
            double proporcion = ProporcionFormato;
            double nuevoAncho = CropAreaWidth + delta * 2; // *2 porque crece desde el centro
            double nuevoAlto = nuevoAncho / proporcion;

            // Limitar tamaño mínimo y máximo
            nuevoAncho = Math.Max(MIN_CROP_SIZE, Math.Min(nuevoAncho, CANVAS_WIDTH));
            nuevoAlto = nuevoAncho / proporcion;

            // Si el alto excede el canvas, ajustar por el alto
            if (nuevoAlto > CANVAS_HEIGHT)
            {
                nuevoAlto = CANVAS_HEIGHT;
                nuevoAncho = nuevoAlto * proporcion;
            }

            // Calcular el cambio de tamaño para ajustar la posición (mantener centrado el redimensionamiento)
            double deltaAncho = nuevoAncho - CropAreaWidth;
            double deltaAlto = nuevoAlto - CropAreaHeight;

            // Ajustar posición según la esquina que se está arrastrando
            if (esquina == "TopLeft" || esquina == "NW")
            {
                CropAreaX -= deltaAncho;
                CropAreaY -= deltaAlto;
            }
            else if (esquina == "TopRight" || esquina == "NE")
            {
                CropAreaY -= deltaAlto;
            }
            else if (esquina == "BottomLeft" || esquina == "SW")
            {
                CropAreaX -= deltaAncho;
            }
            // BottomRight/SE no necesita ajustar posición

            CropAreaWidth = nuevoAncho;
            CropAreaHeight = nuevoAlto;

            NotificarCambioCrop();
            OnPropertyChanged(nameof(CropAreaWidth));
            OnPropertyChanged(nameof(CropAreaHeight));
        }

        [RelayCommand]
        private void EditarNoticia(NoticiaItem noticia)
        {
            if (noticia == null) return;

            ModoEdicion = true;
            TituloPanel = "Editar Noticia";
            TextoBotonGuardar = "Guardar Cambios";
            _noticiaEditandoId = noticia.Id;

            TituloNoticia = noticia.Titulo;
            CategoriaNoticia = noticia.Categoria;
            DescripcionNoticia = noticia.Descripcion;
            ContenidoNoticia = noticia.Contenido;
            ImagenUrl = noticia.ImagenUrl;
            EnlaceUrl = noticia.EnlaceUrl;
            EstadoSeleccionado = noticia.Estado;
            TipoNoticiaSeleccionado = noticia.TipoNoticia;
            IconoComunicacionSeleccionado = noticia.IconoComunicacion ?? "ICONOS allva-05.png";
            ColorComunicacionSeleccionado = noticia.ColorComunicacion ?? "naranja";

            // Cargar valores de encuadre guardados
            if (noticia.ImagenCropWidth > 0 && noticia.ImagenCropHeight > 0)
            {
                CropAreaX = noticia.ImagenCropX;
                CropAreaY = noticia.ImagenCropY;
                CropAreaWidth = noticia.ImagenCropWidth;
                CropAreaHeight = noticia.ImagenCropHeight;
            }

            // Cargar imagen desde base de datos
            // Priorizar la imagen original para poder reencuadrar
            if (noticia.ImagenOriginalData != null && noticia.ImagenOriginalData.Length > 0)
            {
                try
                {
                    // Cargar la imagen ORIGINAL para mostrar en el editor de encuadre
                    using var msOriginal = new MemoryStream(noticia.ImagenOriginalData);
                    ImagenPrevia = new Bitmap(msOriginal);
                    TieneImagenPrevia = true;
                    _imagenOriginalData = noticia.ImagenOriginalData;
                    _imagenData = noticia.ImagenData; // Guardar también la procesada
                }
                catch { }
            }
            else if (noticia.ImagenData != null && noticia.ImagenData.Length > 0)
            {
                try
                {
                    // Si no hay original, usar la imagen procesada (compatibilidad con noticias viejas)
                    using var ms = new MemoryStream(noticia.ImagenData);
                    ImagenPrevia = new Bitmap(ms);
                    TieneImagenPrevia = true;
                    _imagenData = noticia.ImagenData;
                    _imagenOriginalData = noticia.ImagenData; // Usar como original también
                }
                catch { }
            }
            else if (!string.IsNullOrEmpty(ImagenUrl))
            {
                try
                {
                    if (File.Exists(ImagenUrl))
                    {
                        ImagenPrevia = new Bitmap(ImagenUrl);
                        TieneImagenPrevia = true;
                    }
                }
                catch { }
            }

            // Cargar info de video
            if (noticia.TieneVideoGuardado)
            {
                TieneVideo = true;
                NombreVideo = "Video guardado";
                _videoModificado = false;
                _videoCropX = noticia.VideoCropX;
                _videoCropY = noticia.VideoCropY;
                _videoCropW = noticia.VideoCropW;
                _videoCropH = noticia.VideoCropH;
                TieneFrameVideo = true; // Permite re-encuadrar
            }
            else
            {
                TieneVideo = false;
                NombreVideo = string.Empty;
                _videoData = null;
                _videoModificado = false;
                _videoCropX = _videoCropY = _videoCropW = _videoCropH = 0;
                TieneFrameVideo = false;
            }

            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private async Task EliminarNoticia(NoticiaItem noticia)
        {
            if (noticia == null) return;

            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var cmd = new NpgsqlCommand("DELETE FROM noticias WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("@id", noticia.Id);
                await cmd.ExecuteNonQueryAsync();

                Noticias.Remove(noticia);
                ActualizarEstadoFlechas();
                OnPropertyChanged(nameof(NoticiasOrdenadas));
                OnPropertyChanged(nameof(NoticiasDestacadas));
                OnPropertyChanged(nameof(NoticiasCarrusel));
                OnPropertyChanged(nameof(NoticiasComunicaciones));
                OnPropertyChanged(nameof(TieneNoticiasDestacadas));
                OnPropertyChanged(nameof(TieneNoticiasCarrusel));
                OnPropertyChanged(nameof(TieneNoticiasComunicaciones));
                MostrarMensaje("Noticia eliminada correctamente", false);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al eliminar: {ex.Message}", true);
            }
        }

        [RelayCommand]
        private void CancelarEdicion()
        {
            MostrarPanelEdicion = false;
            LimpiarFormulario();
        }

        [RelayCommand]
        private async Task GuardarNoticia()
        {
            if (string.IsNullOrWhiteSpace(TituloNoticia))
            {
                MostrarMensaje("El titulo es obligatorio", true);
                return;
            }

            EstaCargando = true;

            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                if (ModoEdicion && _noticiaEditandoId.HasValue)
                {
                    // Al editar, mantener el orden existente
                    var noticiaExistente = Noticias.FirstOrDefault(n => n.Id == _noticiaEditandoId.Value);
                    var orden = noticiaExistente?.Orden ?? 1;

                    var videoSql = _videoModificado ? ", video_data = @video_data" : "";
                    var cmd = new NpgsqlCommand($@"
                        UPDATE noticias
                        SET titulo = @titulo, categoria = @categoria, descripcion = @descripcion,
                            contenido = @contenido, imagen_url = @imagen_url, imagen_data = @imagen_data,
                            estado = @estado, es_destacada = @destacada,
                            imagen_crop_x = @crop_x, imagen_crop_y = @crop_y,
                            imagen_crop_width = @crop_width, imagen_crop_height = @crop_height,
                            enlace_url = @enlace_url, tipo_noticia = @tipo_noticia,
                            imagen_original_data = @imagen_original,
                            video_crop_x = @vcrop_x, video_crop_y = @vcrop_y,
                            video_crop_w = @vcrop_w, video_crop_h = @vcrop_h,
                            icono_comunicacion = @icono_com, color_comunicacion = @color_com{videoSql}
                        WHERE id = @id", connection);

                    cmd.Parameters.AddWithValue("@titulo", TituloNoticia);
                    cmd.Parameters.AddWithValue("@categoria", CategoriaNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@descripcion", DescripcionNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@contenido", ContenidoNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@imagen_url", ImagenUrl ?? string.Empty);
                    cmd.Parameters.AddWithValue("@imagen_data", _imagenData != null ? (object)_imagenData : DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagen_original", _imagenOriginalData != null ? (object)_imagenOriginalData : DBNull.Value);
                    cmd.Parameters.AddWithValue("@estado", EstadoSeleccionado);
                    cmd.Parameters.AddWithValue("@destacada", TipoNoticiaSeleccionado == "cuadrada");
                    cmd.Parameters.AddWithValue("@crop_x", CropAreaX);
                    cmd.Parameters.AddWithValue("@crop_y", CropAreaY);
                    cmd.Parameters.AddWithValue("@crop_width", CropAreaWidth);
                    cmd.Parameters.AddWithValue("@crop_height", CropAreaHeight);
                    cmd.Parameters.AddWithValue("@enlace_url", EnlaceUrl ?? string.Empty);
                    cmd.Parameters.AddWithValue("@tipo_noticia", TipoNoticiaSeleccionado);
                    cmd.Parameters.AddWithValue("@vcrop_x", _videoCropX);
                    cmd.Parameters.AddWithValue("@vcrop_y", _videoCropY);
                    cmd.Parameters.AddWithValue("@vcrop_w", _videoCropW);
                    cmd.Parameters.AddWithValue("@vcrop_h", _videoCropH);
                    cmd.Parameters.AddWithValue("@icono_com", IconoComunicacionSeleccionado ?? "ICONOS allva-05.png");
                    cmd.Parameters.AddWithValue("@color_com", ColorComunicacionSeleccionado ?? "naranja");
                    cmd.Parameters.AddWithValue("@id", _noticiaEditandoId.Value);
                    if (_videoModificado)
                        cmd.Parameters.AddWithValue("@video_data", _videoData != null ? (object)_videoData : DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    MostrarMensaje("Noticia actualizada correctamente", false);
                }
                else
                {
                    // Para nueva noticia, asignar el siguiente orden (al final de la lista)
                    var maxOrden = Noticias.Any() ? Noticias.Max(n => n.Orden) : 0;
                    var nuevoOrden = maxOrden + 1;

                    var cmd = new NpgsqlCommand(@"
                        INSERT INTO noticias (titulo, categoria, descripcion, contenido, imagen_url, imagen_data,
                                            estado, es_destacada, orden, fecha_publicacion, fecha_creacion,
                                            imagen_crop_x, imagen_crop_y, imagen_crop_width, imagen_crop_height,
                                            enlace_url, tipo_noticia, imagen_original_data, video_data,
                                            video_crop_x, video_crop_y, video_crop_w, video_crop_h,
                                            icono_comunicacion, color_comunicacion)
                        VALUES (@titulo, @categoria, @descripcion, @contenido, @imagen_url, @imagen_data,
                                @estado, @destacada, @orden, @fecha_pub, @fecha,
                                @crop_x, @crop_y, @crop_width, @crop_height,
                                @enlace_url, @tipo_noticia, @imagen_original, @video_data,
                                @vcrop_x, @vcrop_y, @vcrop_w, @vcrop_h,
                                @icono_com, @color_com)", connection);

                    cmd.Parameters.AddWithValue("@titulo", TituloNoticia);
                    cmd.Parameters.AddWithValue("@categoria", CategoriaNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@descripcion", DescripcionNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@contenido", ContenidoNoticia ?? string.Empty);
                    cmd.Parameters.AddWithValue("@imagen_url", ImagenUrl ?? string.Empty);
                    cmd.Parameters.AddWithValue("@imagen_data", _imagenData != null ? (object)_imagenData : DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagen_original", _imagenOriginalData != null ? (object)_imagenOriginalData : DBNull.Value);
                    cmd.Parameters.AddWithValue("@estado", EstadoSeleccionado);
                    cmd.Parameters.AddWithValue("@destacada", TipoNoticiaSeleccionado == "cuadrada");
                    cmd.Parameters.AddWithValue("@orden", nuevoOrden);
                    cmd.Parameters.AddWithValue("@fecha_pub", DateTime.Now);
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                    cmd.Parameters.AddWithValue("@crop_x", CropAreaX);
                    cmd.Parameters.AddWithValue("@crop_y", CropAreaY);
                    cmd.Parameters.AddWithValue("@crop_width", CropAreaWidth);
                    cmd.Parameters.AddWithValue("@crop_height", CropAreaHeight);
                    cmd.Parameters.AddWithValue("@enlace_url", EnlaceUrl ?? string.Empty);
                    cmd.Parameters.AddWithValue("@tipo_noticia", TipoNoticiaSeleccionado);
                    cmd.Parameters.AddWithValue("@video_data", _videoData != null ? (object)_videoData : DBNull.Value);
                    cmd.Parameters.AddWithValue("@vcrop_x", _videoCropX);
                    cmd.Parameters.AddWithValue("@vcrop_y", _videoCropY);
                    cmd.Parameters.AddWithValue("@vcrop_w", _videoCropW);
                    cmd.Parameters.AddWithValue("@vcrop_h", _videoCropH);
                    cmd.Parameters.AddWithValue("@icono_com", IconoComunicacionSeleccionado ?? "ICONOS allva-05.png");
                    cmd.Parameters.AddWithValue("@color_com", ColorComunicacionSeleccionado ?? "naranja");

                    await cmd.ExecuteNonQueryAsync();
                    MostrarMensaje("Noticia creada correctamente", false);
                }

                MostrarPanelEdicion = false;
                LimpiarFormulario();
                await CargarNoticiasAsync();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al guardar: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void LimpiarFormulario()
        {
            TituloNoticia = string.Empty;
            CategoriaNoticia = string.Empty;
            DescripcionNoticia = string.Empty;
            ContenidoNoticia = string.Empty;
            ImagenUrl = string.Empty;
            EnlaceUrl = string.Empty;
            ImagenPrevia = null;
            ImagenOriginalParaEncuadre = null;
            TieneImagenPrevia = false;
            EstadoSeleccionado = "Activa";
            TipoNoticiaSeleccionado = "cuadrada";
            IconoComunicacionSeleccionado = "ICONOS allva-05.png";
            ColorComunicacionSeleccionado = "naranja";
            _noticiaEditandoId = null;
            _imagenData = null;
            _imagenOriginalData = null;
            _videoData = null;
            _videoModificado = false;
            TieneVideo = false;
            TieneFrameVideo = false;
            NombreVideo = string.Empty;
            _videoCropX = _videoCropY = _videoCropW = _videoCropH = 0;
            _encuadrandoVideo = false;

            // Limpiar campos de encuadre
            FormatoSeleccionado = "Rectangular (345:200)";
            ZoomLevel = 1.0;
            ImagenOffsetX = 0;
            ImagenOffsetY = 0;
            CropAreaX = 0;
            CropAreaY = 26;
            CropAreaWidth = 600;
            CropAreaHeight = 348;
            MostrarPanelEncuadre = false;
        }

        private async void MostrarMensaje(string mensaje, bool esError)
        {
            MensajeEstado = mensaje;
            MensajeEsError = esError;
            HayMensaje = true;
            OnPropertyChanged(nameof(MensajeBackground));

            await Task.Delay(4000);
            HayMensaje = false;
        }
    }

    public class NoticiaItem : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string ImagenUrl { get; set; } = string.Empty;
        public string EnlaceUrl { get; set; } = string.Empty;
        public byte[]? ImagenData { get; set; }
        public byte[]? ImagenOriginalData { get; set; } // Imagen original sin recortar
        public bool TieneVideoGuardado { get; set; } // Si tiene video asociado
        public double VideoCropX { get; set; }
        public double VideoCropY { get; set; }
        public double VideoCropW { get; set; }
        public double VideoCropH { get; set; }

        // Campos para el recorte/encuadre de imagen
        public double ImagenCropX { get; set; } = 0;
        public double ImagenCropY { get; set; } = 0;
        public double ImagenCropWidth { get; set; } = 0;
        public double ImagenCropHeight { get; set; } = 0;

        public string Estado { get; set; } = "Activa";
        public bool EsDestacada { get; set; } = false;
        public string TipoNoticia { get; set; } = "cuadrada";

        // Posición en la lista de destacadas (1-4)
        public int PosicionDestacada { get; set; } = 0;

        private int _orden = 1;
        public int Orden
        {
            get => _orden;
            set
            {
                if (_orden != value)
                {
                    _orden = value;
                    OnPropertyChanged(nameof(Orden));
                }
            }
        }

        // Propiedades para habilitar/deshabilitar flechas de mover
        private bool _puedeSubir = true;
        public bool PuedeSubir
        {
            get => _puedeSubir;
            set
            {
                if (_puedeSubir != value)
                {
                    _puedeSubir = value;
                    OnPropertyChanged(nameof(PuedeSubir));
                }
            }
        }

        private bool _puedeBajar = true;
        public bool PuedeBajar
        {
            get => _puedeBajar;
            set
            {
                if (_puedeBajar != value)
                {
                    _puedeBajar = value;
                    OnPropertyChanged(nameof(PuedeBajar));
                }
            }
        }

        public DateTime FechaPublicacion { get; set; }
        public DateTime FechaCreacion { get; set; }

        public string FechaCreacionTexto => FechaCreacion.ToString("dd/MM/yyyy HH:mm");

        public string EstadoTexto => Estado;

        // Propiedad para verificar si tiene imagen
        public bool TieneImagen => ImagenData != null && ImagenData.Length > 0;

        // Propiedad para obtener la imagen como Bitmap
        private Bitmap? _imagenBitmap;
        public Bitmap? ImagenBitmap
        {
            get
            {
                if (_imagenBitmap == null && ImagenData != null && ImagenData.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(ImagenData);
                        _imagenBitmap = new Bitmap(ms);
                    }
                    catch { }
                }
                return _imagenBitmap;
            }
        }

        // Icono y Color para comunicaciones
        public string IconoComunicacion { get; set; } = "ICONOS allva-05.png";
        public string ColorComunicacion { get; set; } = "naranja";

        // Ruta del asset del icono
        public string IconoComunicacionRuta => $"/Assets/Noticias/{IconoComunicacion}";

        // Colores derivados de la clave de color
        public IBrush ColorComunicacionFondo => new SolidColorBrush(Color.Parse(ColorComunicacion switch
        {
            "rojo" => "#FFCDD2",
            "naranja" => "#FFE0B2",
            "amarillo" => "#FFF9C4",
            "azul" => "#BBDEFB",
            "verde" => "#C8E6C9",
            "morado" => "#E1BEE7",
            _ => "#FFE0B2"
        }));

        public IBrush ColorComunicacionTexto => new SolidColorBrush(Color.Parse(ColorComunicacion switch
        {
            "rojo" => "#C62828",
            "naranja" => "#E65100",
            "amarillo" => "#F57F17",
            "azul" => "#0D47A1",
            "verde" => "#1B5E20",
            "morado" => "#4A148C",
            _ => "#E65100"
        }));

        public IBrush ColorComunicacionIcono => new SolidColorBrush(Color.Parse(ColorComunicacion switch
        {
            "rojo" => "#EF5350",
            "naranja" => "#FF7043",
            "amarillo" => "#FFD54F",
            "azul" => "#42A5F5",
            "verde" => "#66BB6A",
            "morado" => "#AB47BC",
            _ => "#FF7043"
        }));

        // Color del borde de la ficha (azul corporativo para destacadas, gris claro para normales)
        public IBrush BorderColor => EsDestacada
            ? new SolidColorBrush(Color.Parse("#0b5394"))
            : new SolidColorBrush(Color.Parse("#E0E0E0"));

        public IBrush EstadoBackground => Estado switch
        {
            "Activa" => new SolidColorBrush(Color.Parse("#D4EDDA")),
            "Inactiva" => new SolidColorBrush(Color.Parse("#F8D7DA")),
            "Borrador" => new SolidColorBrush(Color.Parse("#FFF3CD")),
            _ => new SolidColorBrush(Color.Parse("#E2E3E5"))
        };

        public IBrush EstadoForeground => Estado switch
        {
            "Activa" => new SolidColorBrush(Color.Parse("#155724")),
            "Inactiva" => new SolidColorBrush(Color.Parse("#721C24")),
            "Borrador" => new SolidColorBrush(Color.Parse("#856404")),
            _ => new SolidColorBrush(Color.Parse("#383D41"))
        };
    }

    public class IconoComunicacionItem
    {
        public string NombreArchivo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string RutaAsset { get; set; } = string.Empty;

        public Bitmap? IconoBitmap
        {
            get
            {
                try
                {
                    var uri = new Uri($"avares://Allva.Desktop{RutaAsset}");
                    using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                    return new Bitmap(stream);
                }
                catch { return null; }
            }
        }
    }

    public class ColorComunicacionItem
    {
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ColorFondo { get; set; } = string.Empty;
        public string ColorTexto { get; set; } = string.Empty;
        public string ColorIcono { get; set; } = string.Empty;

        public IBrush FondoBrush => new SolidColorBrush(Color.Parse(ColorFondo));
        public IBrush TextoBrush => new SolidColorBrush(Color.Parse(ColorTexto));
        public IBrush IconoBrush => new SolidColorBrush(Color.Parse(ColorIcono));
    }
}
