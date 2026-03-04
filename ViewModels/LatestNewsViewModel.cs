using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using Npgsql;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el módulo de Últimas Noticias (Dashboard principal)
/// </summary>
public partial class LatestNewsViewModel : BaseViewModel
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    private Timer? _carruselTimer;
    private int _indiceCarruselActual = 0;

    public LatestNewsViewModel()
    {
        Titulo = "Últimas Noticias";
        _ = InicializarAsync();
        IniciarCarruselAutomatico();
    }

    private async Task InicializarAsync()
    {
        // Asegurar que la columna video_data existe antes de hacer queries
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("ALTER TABLE noticias ADD COLUMN IF NOT EXISTS video_data bytea;", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }

        await CargarNoticiasAsync();
    }

    // Noticias para el grid 2x2 (cuadradas)
    [ObservableProperty]
    private ObservableCollection<NoticiaItem> _noticiasCuadradas = new();

    // Noticias para el carrusel vertical
    [ObservableProperty]
    private ObservableCollection<NoticiaItem> _noticiasCarrusel = new();

    // Comunicaciones del sistema
    [ObservableProperty]
    private ObservableCollection<NoticiaItem> _comunicacionesSistema = new();

    // Estado de carga
    [ObservableProperty]
    private bool _cargandoNoticias = true;

    // Noticia actual visible en el carrusel
    [ObservableProperty]
    private NoticiaItem? _noticiaCarruselActual;

    // Video inline rendering (WriteableBitmap approach for Viewbox compatibility)
    private const uint VIDEO_WIDTH = 1280;
    private const uint VIDEO_HEIGHT = 720;
    private const uint VIDEO_PITCH = VIDEO_WIDTH * 4;
    private const int VIDEO_BUFFER_SIZE = (int)(VIDEO_PITCH * VIDEO_HEIGHT);

    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private string? _videoTempPath;
    private WriteableBitmap? _videoFrameA;
    private WriteableBitmap? _videoFrameB;
    private bool _useFrameA = true;
    private IntPtr _videoBuffer = IntPtr.Zero;
    private byte[]? _videoManagedBuffer;
    private readonly object _videoLock = new();
    private DateTime _lastFrameTime = DateTime.MinValue;
    private readonly HashSet<string> _videoTempFiles = new();
    private volatile bool _videoActivo = false; // Flag para indicar que el video está activo
    private IntPtr _videoDummyBuffer = IntPtr.Zero; // Buffer descartable para cuando el video se está deteniendo

    // Dimensiones reales del video (detectadas al parsear)
    private uint _realVideoWidth = VIDEO_WIDTH;
    private uint _realVideoHeight = VIDEO_HEIGHT;
    private uint _realVideoPitch;
    private int _realVideoBufferSize;

    [ObservableProperty]
    private Bitmap? _videoFrame;

    [ObservableProperty]
    private bool _mostrandoVideo;

    partial void OnNoticiaCarruselActualChanged(NoticiaItem? value)
    {
        DetenerVideoInline();
        if (value?.TieneVideo == true)
        {
            _ = IniciarVideoInlineAsync(value);
        }
    }

    // Indica si hay múltiples slides para mostrar controles
    public bool TieneMultiplesSlides => NoticiasCarrusel.Count > 1;

    // Propiedades para verificar si cada noticia existe
    public bool TieneNoticia1 => NoticiasCuadradas.Count > 0;
    public bool TieneNoticia2 => NoticiasCuadradas.Count > 1;
    public bool TieneNoticia3 => NoticiasCuadradas.Count > 2;
    public bool TieneNoticia4 => NoticiasCuadradas.Count > 3;

    [ObservableProperty]
    private ObservableCollection<NoticiaItem> _noticiasDestacadas = new();

    [ObservableProperty]
    private ObservableCollection<NoticiaItem> _noticiasAdicionales = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayNoticiaDestacadaExpandida))]
    private NoticiaItem? _noticiaDestacadaSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayNoticiaAdicionalExpandida))]
    private NoticiaItem? _noticiaAdicionalSeleccionada;

    public bool HayNoticiaDestacadaExpandida => NoticiaDestacadaSeleccionada != null;
    public bool HayNoticiaAdicionalExpandida => NoticiaAdicionalSeleccionada != null;

    public void SeleccionarNoticiaDestacada(NoticiaItem? noticia)
    {
        // Primero, colapsar la noticia anterior si existe
        if (NoticiaDestacadaSeleccionada != null)
        {
            NoticiaDestacadaSeleccionada.EstaExpandida = false;
        }

        // Si es la misma, deseleccionar
        if (NoticiaDestacadaSeleccionada == noticia)
        {
            NoticiaDestacadaSeleccionada = null;
        }
        else
        {
            NoticiaDestacadaSeleccionada = noticia;
            if (noticia != null)
            {
                noticia.EstaExpandida = true;
            }
        }
    }

    public void SeleccionarNoticiaAdicional(NoticiaItem? noticia)
    {
        // Primero, colapsar la noticia anterior si existe
        if (NoticiaAdicionalSeleccionada != null)
        {
            NoticiaAdicionalSeleccionada.EstaExpandida = false;
        }

        // Si es la misma, deseleccionar
        if (NoticiaAdicionalSeleccionada == noticia)
        {
            NoticiaAdicionalSeleccionada = null;
        }
        else
        {
            NoticiaAdicionalSeleccionada = noticia;
            if (noticia != null)
            {
                noticia.EstaExpandida = true;
            }
        }
    }

    private async Task CargarNoticiasAsync()
    {
        CargandoNoticias = true;
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Cargar noticias cuadradas (grid 2x3) - máximo 6
            var cmdCuadradas = new NpgsqlCommand(@"
                SELECT id, titulo, categoria, descripcion, contenido, imagen_url, fecha_publicacion, imagen_data, enlace_url,
                       imagen_crop_x, imagen_crop_y, imagen_crop_width, imagen_crop_height
                FROM noticias
                WHERE estado = 'Activa' AND tipo_noticia = 'cuadrada'
                ORDER BY orden ASC, fecha_publicacion DESC
                LIMIT 4", connection);

            await using var readerCuadradas = await cmdCuadradas.ExecuteReaderAsync();
            NoticiasCuadradas.Clear();

            while (await readerCuadradas.ReadAsync())
            {
                var noticia = CrearNoticiaItem(readerCuadradas);
                noticia.ParentViewModel = this;
                NoticiasCuadradas.Add(noticia);
            }

            await readerCuadradas.CloseAsync();

            // Notificar cambios en las propiedades de existencia de noticias
            OnPropertyChanged(nameof(TieneNoticia1));
            OnPropertyChanged(nameof(TieneNoticia2));
            OnPropertyChanged(nameof(TieneNoticia3));
            OnPropertyChanged(nameof(TieneNoticia4));

            // Cargar noticias del carrusel (incluye flag de video)
            var cmdCarrusel = new NpgsqlCommand(@"
                SELECT id, titulo, categoria, descripcion, contenido, imagen_url, fecha_publicacion, imagen_data, enlace_url,
                       imagen_crop_x, imagen_crop_y, imagen_crop_width, imagen_crop_height, (video_data IS NOT NULL) as tiene_video,
                       video_crop_x, video_crop_y, video_crop_w, video_crop_h
                FROM noticias
                WHERE estado = 'Activa' AND tipo_noticia = 'carrusel'
                ORDER BY orden ASC, fecha_publicacion DESC
                LIMIT 10", connection);

            await using var readerCarrusel = await cmdCarrusel.ExecuteReaderAsync();
            NoticiasCarrusel.Clear();

            while (await readerCarrusel.ReadAsync())
            {
                var noticia = CrearNoticiaItem(readerCarrusel);
                noticia.ParentViewModel = this;
                // Leer flag de video y crop (columnas extra 13-17)
                noticia.TieneVideo = !readerCarrusel.IsDBNull(13) && readerCarrusel.GetBoolean(13);
                noticia.VideoCropX = readerCarrusel.IsDBNull(14) ? 0 : readerCarrusel.GetDouble(14);
                noticia.VideoCropY = readerCarrusel.IsDBNull(15) ? 0 : readerCarrusel.GetDouble(15);
                noticia.VideoCropW = readerCarrusel.IsDBNull(16) ? 0 : readerCarrusel.GetDouble(16);
                noticia.VideoCropH = readerCarrusel.IsDBNull(17) ? 0 : readerCarrusel.GetDouble(17);
                NoticiasCarrusel.Add(noticia);
            }

            await readerCarrusel.CloseAsync();

            // Cargar comunicaciones del sistema (incluye icono y color)
            var cmdComunicaciones = new NpgsqlCommand(@"
                SELECT id, titulo, categoria, descripcion, contenido, imagen_url, fecha_publicacion, imagen_data, enlace_url,
                       imagen_crop_x, imagen_crop_y, imagen_crop_width, imagen_crop_height,
                       icono_comunicacion, color_comunicacion
                FROM noticias
                WHERE estado = 'Activa' AND tipo_noticia = 'comunicacion'
                ORDER BY orden ASC, fecha_publicacion DESC
                LIMIT 5", connection);

            await using var readerComunicaciones = await cmdComunicaciones.ExecuteReaderAsync();
            ComunicacionesSistema.Clear();

            while (await readerComunicaciones.ReadAsync())
            {
                var noticia = CrearNoticiaItem(readerComunicaciones);
                noticia.ParentViewModel = this;
                noticia.IconoComunicacion = readerComunicaciones.IsDBNull(13) ? "ICONOS allva-05.png" : readerComunicaciones.GetString(13);
                noticia.ColorComunicacion = readerComunicaciones.IsDBNull(14) ? "naranja" : readerComunicaciones.GetString(14);
                ComunicacionesSistema.Add(noticia);
            }

            await readerComunicaciones.CloseAsync();

            // Cargar noticias destacadas (máximo 6 para el grid 3x2) - compatibilidad
            var cmdDestacadas = new NpgsqlCommand(@"
                SELECT id, titulo, categoria, descripcion, contenido, imagen_url, fecha_publicacion, imagen_data, enlace_url,
                       imagen_crop_x, imagen_crop_y, imagen_crop_width, imagen_crop_height
                FROM noticias
                WHERE estado = 'Activa' AND es_destacada = true
                ORDER BY orden ASC, fecha_publicacion DESC
                LIMIT 4", connection);

            await using var readerDestacadas = await cmdDestacadas.ExecuteReaderAsync();
            NoticiasDestacadas.Clear();

            while (await readerDestacadas.ReadAsync())
            {
                var noticia = CrearNoticiaItem(readerDestacadas);
                noticia.EsDestacada = true;
                noticia.ParentViewModel = this;
                NoticiasDestacadas.Add(noticia);
            }

            await readerDestacadas.CloseAsync();

            // Cargar noticias adicionales (no destacadas) - compatibilidad
            var cmdAdicionales = new NpgsqlCommand(@"
                SELECT id, titulo, categoria, descripcion, contenido, imagen_url, fecha_publicacion, imagen_data, enlace_url,
                       imagen_crop_x, imagen_crop_y, imagen_crop_width, imagen_crop_height
                FROM noticias
                WHERE estado = 'Activa' AND (es_destacada = false OR es_destacada IS NULL)
                ORDER BY fecha_publicacion DESC
                LIMIT 10", connection);

            await using var readerAdicionales = await cmdAdicionales.ExecuteReaderAsync();
            NoticiasAdicionales.Clear();

            while (await readerAdicionales.ReadAsync())
            {
                var noticia = CrearNoticiaItem(readerAdicionales);
                noticia.EsDestacada = false;
                noticia.ParentViewModel = this;
                NoticiasAdicionales.Add(noticia);
            }

            // Configurar el carrusel inicial
            if (NoticiasCarrusel.Count > 0)
            {
                _indiceCarruselActual = 0;
                ActualizarCarrusel();
            }

            OnPropertyChanged(nameof(TieneMultiplesSlides));
        }
        catch (Exception)
        {
            // En caso de error, mostrar noticias de ejemplo
            CargarNoticiasEjemplo();
        }
        finally
        {
            CargandoNoticias = false;
        }
    }

    private NoticiaItem CrearNoticiaItem(NpgsqlDataReader reader)
    {
        var noticia = new NoticiaItem
        {
            Id = reader.GetInt32(0),
            Titulo = reader.GetString(1),
            Categoria = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Descripcion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Contenido = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            ImagenUrl = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            FechaPublicacion = reader.IsDBNull(6) ? DateTime.Now : reader.GetDateTime(6),
            ImagenData = reader.IsDBNull(7) ? null : (byte[])reader[7],
            EnlaceUrl = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            CropX = reader.IsDBNull(9) ? 0 : reader.GetDouble(9),
            CropY = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
            CropWidth = reader.IsDBNull(11) ? 0 : reader.GetDouble(11),
            CropHeight = reader.IsDBNull(12) ? 0 : reader.GetDouble(12)
        };
        noticia.CargarImagenBitmap();
        return noticia;
    }

    // Métodos para controlar el carrusel automático
    private void IniciarCarruselAutomatico()
    {
        _carruselTimer = new Timer(10000); // 10 segundos
        _carruselTimer.Elapsed += OnCarruselTimerElapsed;
        _carruselTimer.AutoReset = true;
        _carruselTimer.Start();
    }

    private void ReiniciarTimer()
    {
        _carruselTimer?.Stop();
        _carruselTimer?.Start();
    }

    private void OnCarruselTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AvanzarCarrusel();
        });
    }

    [RelayCommand]
    private void AvanzarCarrusel()
    {
        if (NoticiasCarrusel.Count == 0) return;

        _indiceCarruselActual = (_indiceCarruselActual + 1) % NoticiasCarrusel.Count;
        ActualizarCarrusel();
        ReiniciarTimer();
    }

    [RelayCommand]
    private void RetrocederCarrusel()
    {
        if (NoticiasCarrusel.Count == 0) return;

        _indiceCarruselActual--;
        if (_indiceCarruselActual < 0)
            _indiceCarruselActual = NoticiasCarrusel.Count - 1;

        ActualizarCarrusel();
        ReiniciarTimer();
    }

    private void ActualizarCarrusel()
    {
        if (NoticiasCarrusel.Count == 0)
        {
            NoticiaCarruselActual = null;
            return;
        }

        // Actualizar todas las noticias del carrusel para marcar cuál está activa
        for (int i = 0; i < NoticiasCarrusel.Count; i++)
        {
            NoticiasCarrusel[i].EsSlideActivo = (i == _indiceCarruselActual);
        }

        NoticiaCarruselActual = NoticiasCarrusel[_indiceCarruselActual];
        OnPropertyChanged(nameof(TieneMultiplesSlides));
    }

    public void IrASlide(int indice)
    {
        if (indice >= 0 && indice < NoticiasCarrusel.Count)
        {
            _indiceCarruselActual = indice;
            ActualizarCarrusel();
            ReiniciarTimer();
        }
    }

    private void CargarNoticiasEjemplo()
    {
        NoticiasDestacadas = new ObservableCollection<NoticiaItem>
        {
            new NoticiaItem
            {
                Titulo = "Nueva Promoción Disponible",
                Categoria = "Fri Jun 19 2020 | Promociones",
                Descripcion = "Sample small text. Lorem ipsum dolor sit amet.",
                ImagenUrl = string.Empty
            }
        };
    }

    private async Task IniciarVideoInlineAsync(NoticiaItem noticia)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"allva_video_{noticia.Id}.mp4");

            // Usar cache si el archivo temporal ya existe
            if (!File.Exists(tempPath))
            {
                byte[]? videoData = null;
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                var cmd = new NpgsqlCommand("SELECT video_data FROM noticias WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("@id", noticia.Id);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    videoData = (byte[])result;

                if (videoData == null || videoData.Length == 0) return;
                if (NoticiaCarruselActual != noticia) return;

                await File.WriteAllBytesAsync(tempPath, videoData);
            }

            _videoTempFiles.Add(tempPath);
            _videoTempPath = tempPath;

            if (NoticiaCarruselActual != noticia) return;

            // Inicializar LibVLC sin audio (es un carrusel de fondo)
            if (_libVLC == null)
            {
                Core.Initialize();
                _libVLC = new LibVLC("--no-audio");
            }

            // Crear nuevo player (el viejo ya fue limpiado por DetenerVideoInline)
            _mediaPlayer = new MediaPlayer(_libVLC);

            // Parsear media para obtener dimensiones reales del video
            _realVideoWidth = VIDEO_WIDTH;
            _realVideoHeight = VIDEO_HEIGHT;
            using (var mediaParse = new Media(_libVLC, tempPath, FromType.FromPath))
            {
                await mediaParse.Parse(MediaParseOptions.ParseLocal, timeout: 5000);
                foreach (var track in mediaParse.Tracks)
                {
                    if (track.TrackType == TrackType.Video)
                    {
                        _realVideoWidth = track.Data.Video.Width;
                        _realVideoHeight = track.Data.Video.Height;
                        break;
                    }
                }
            }

            // Limitar dimensiones para rendimiento (el carrusel es pequeño, no necesita más de 960px)
            if (_realVideoWidth > 960 || _realVideoHeight > 960)
            {
                double scale = 960.0 / Math.Max(_realVideoWidth, _realVideoHeight);
                _realVideoWidth = (uint)(_realVideoWidth * scale);
                _realVideoHeight = (uint)(_realVideoHeight * scale);
            }
            _realVideoWidth = (_realVideoWidth / 2) * 2;
            _realVideoHeight = (_realVideoHeight / 2) * 2;
            if (_realVideoWidth < 2) _realVideoWidth = 2;
            if (_realVideoHeight < 2) _realVideoHeight = 2;

            _realVideoPitch = _realVideoWidth * 4;
            _realVideoBufferSize = (int)(_realVideoPitch * _realVideoHeight);

            if (NoticiaCarruselActual != noticia) return;

            // Asignar buffer y WriteableBitmaps para renderizado (frame completo, UniformToFill se encarga del ajuste)
            lock (_videoLock)
            {
                if (_videoBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(_videoBuffer);
                _videoBuffer = Marshal.AllocHGlobal(_realVideoBufferSize);
                _videoManagedBuffer = new byte[_realVideoBufferSize];

                // Buffer dummy para cuando se está deteniendo el video
                if (_videoDummyBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(_videoDummyBuffer);
                _videoDummyBuffer = Marshal.AllocHGlobal(_realVideoBufferSize);
            }

            // WriteableBitmaps al tamaño completo del video (UniformToFill recorta automáticamente)
            var size = new Avalonia.PixelSize((int)_realVideoWidth, (int)_realVideoHeight);
            var dpi = new Avalonia.Vector(96, 96);
            _videoFrameA = new WriteableBitmap(size, dpi, Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);
            _videoFrameB = new WriteableBitmap(size, dpi, Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);

            // Configurar formato con dimensiones REALES del video
            _mediaPlayer.SetVideoFormat("RV32", _realVideoWidth, _realVideoHeight, _realVideoPitch);
            _mediaPlayer.SetVideoCallbacks(OnVideoLock, OnVideoUnlock, OnVideoDisplay);

            // Reproducir en loop (por si el video dura menos de 7 segundos)
            _videoActivo = true;
            using var media = new Media(_libVLC, tempPath, FromType.FromPath);
            media.AddOption(":input-repeat=65535");
            _mediaPlayer.Play(media);
            MostrandoVideo = true;
        }
        catch
        {
            MostrandoVideo = false;
        }
    }

    private void DetenerVideoInline()
    {
        // 1. Desactivar el flag PRIMERO para que los callbacks ignoren nuevos frames
        _videoActivo = false;
        MostrandoVideo = false;
        VideoFrame = null;

        // 2. Capturar y desasociar el player actual para evitar race condition
        var player = _mediaPlayer;
        _mediaPlayer = null;

        // 3. Liberar buffers bajo lock (los callbacks ya no los usarán gracias a _videoActivo=false)
        lock (_videoLock)
        {
            if (_videoBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_videoBuffer);
                _videoBuffer = IntPtr.Zero;
            }
            _videoManagedBuffer = null;
        }

        // 4. Detener y disponer el player viejo en background (puede tardar)
        if (player != null)
        {
            Task.Run(() =>
            {
                try { player.Stop(); } catch { }
                System.Threading.Thread.Sleep(50);
                try { player.Dispose(); } catch { }
            });
        }
    }

    // --- LibVLC video callbacks para renderizar a WriteableBitmap ---

    private IntPtr OnVideoLock(IntPtr opaque, IntPtr planes)
    {
        System.Threading.Monitor.Enter(_videoLock);

        // Verificar que el video sigue activo y el buffer es válido
        if (!_videoActivo || _videoBuffer == IntPtr.Zero)
        {
            // Dar a VLC un buffer dummy válido para que no escriba en memoria inválida
            if (_videoDummyBuffer != IntPtr.Zero)
                Marshal.WriteIntPtr(planes, _videoDummyBuffer);
            return IntPtr.Zero;
        }

        Marshal.WriteIntPtr(planes, _videoBuffer);
        return IntPtr.Zero;
    }

    private void OnVideoUnlock(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
        // Solo liberar el lock si lo tenemos (puede no estar tomado si OnVideoLock salió temprano)
        try { System.Threading.Monitor.Exit(_videoLock); } catch (System.Threading.SynchronizationLockException) { }
    }

    private void OnVideoDisplay(IntPtr opaque, IntPtr picture)
    {
        // Si el video ya no está activo, ignorar
        if (!_videoActivo) return;

        // Limitar a ~30fps para no saturar el hilo UI
        var now = DateTime.UtcNow;
        if ((now - _lastFrameTime).TotalMilliseconds < 33) return;
        _lastFrameTime = now;

        Dispatcher.UIThread.Post(() =>
        {
            if (!_videoActivo) return;
            if (!System.Threading.Monitor.TryEnter(_videoLock)) return;
            try
            {
                if (_videoBuffer == IntPtr.Zero || _videoManagedBuffer == null || !_videoActivo) return;

                var target = _useFrameA ? _videoFrameA : _videoFrameB;
                if (target == null) return;

                // Copiar el frame completo del buffer no administrado al managed buffer
                Marshal.Copy(_videoBuffer, _videoManagedBuffer, 0, _realVideoBufferSize);

                // Copiar al WriteableBitmap (UniformToFill en el AXAML ajusta automáticamente al carrusel)
                using var lockedBitmap = target.Lock();
                Marshal.Copy(_videoManagedBuffer, 0, lockedBitmap.Address, _realVideoBufferSize);

                // Alternar entre los dos bitmaps para forzar el cambio de referencia en el binding
                VideoFrame = target;
                _useFrameA = !_useFrameA;
            }
            catch { }
            finally
            {
                System.Threading.Monitor.Exit(_videoLock);
            }
        });
    }

    // Cleanup del timer y video cuando se destruye el ViewModel
    ~LatestNewsViewModel()
    {
        _videoActivo = false;
        _carruselTimer?.Stop();
        _carruselTimer?.Dispose();
        try
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
        }
        catch { }
        _libVLC?.Dispose();

        // Liberar buffers no administrados
        if (_videoBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_videoBuffer);
            _videoBuffer = IntPtr.Zero;
        }
        if (_videoDummyBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_videoDummyBuffer);
            _videoDummyBuffer = IntPtr.Zero;
        }

        // Limpiar archivos temporales de video
        foreach (var tempFile in _videoTempFiles)
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }
}

/// <summary>
/// Modelo para una noticia individual
/// </summary>
public partial class NoticiaItem : ObservableObject
{
    // Constantes del canvas del admin (para calcular el recorte)
    private const double CANVAS_WIDTH = 600.0;
    private const double CANVAS_HEIGHT = 400.0;

    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string EnlaceUrl { get; set; } = string.Empty;
    public byte[]? ImagenData { get; set; }
    public Bitmap? ImagenBitmap { get; set; }
    public DateTime FechaPublicacion { get; set; }

    // Valores de recorte configurados en el admin
    public double CropX { get; set; }
    public double CropY { get; set; }
    public double CropWidth { get; set; }
    public double CropHeight { get; set; }

    // Referencia al ViewModel padre para notificar cambios de expansión
    public LatestNewsViewModel? ParentViewModel { get; set; }

    // Indica si es una noticia destacada (para saber qué método de selección usar)
    public bool EsDestacada { get; set; }

    public string FechaPublicacionTexto => FechaPublicacion.ToString("dd MMM yyyy");
    public bool TieneImagen => ImagenBitmap != null || !string.IsNullOrEmpty(ImagenUrl);
    public bool TieneEnlace => !string.IsNullOrWhiteSpace(EnlaceUrl);
    public bool TieneVideo { get; set; }

    // Icono y Color para comunicaciones
    public string IconoComunicacion { get; set; } = "ICONOS allva-05.png";
    public string ColorComunicacion { get; set; } = "naranja";

    // Ruta del asset del icono
    public string IconoComunicacionRuta => $"/Assets/Noticias/{IconoComunicacion}";

    public Bitmap? IconoComunicacionBitmap
    {
        get
        {
            try
            {
                var uri = new Uri($"avares://Allva.Desktop/Assets/Noticias/{IconoComunicacion}");
                using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                return new Bitmap(stream);
            }
            catch { return null; }
        }
    }

    // Colores derivados de la clave de color
    public string ColorComunicacionFondoHex => ColorComunicacion switch
    {
        "rojo" => "#FFCDD2",
        "naranja" => "#FFE0B2",
        "amarillo" => "#FFF9C4",
        "azul" => "#BBDEFB",
        "verde" => "#C8E6C9",
        "morado" => "#E1BEE7",
        _ => "#FFE0B2"
    };

    public string ColorComunicacionTextoHex => ColorComunicacion switch
    {
        "rojo" => "#C62828",
        "naranja" => "#E65100",
        "amarillo" => "#F57F17",
        "azul" => "#0D47A1",
        "verde" => "#1B5E20",
        "morado" => "#4A148C",
        _ => "#E65100"
    };

    public string ColorComunicacionIconoHex => ColorComunicacion switch
    {
        "rojo" => "#EF5350",
        "naranja" => "#FF7043",
        "amarillo" => "#FFD54F",
        "azul" => "#42A5F5",
        "verde" => "#66BB6A",
        "morado" => "#AB47BC",
        _ => "#FF7043"
    };

    public IBrush ColorComunicacionFondoBrush => new SolidColorBrush(Color.Parse(ColorComunicacionFondoHex));
    public IBrush ColorComunicacionTextoBrush => new SolidColorBrush(Color.Parse(ColorComunicacionTextoHex));
    public IBrush ColorComunicacionIconoBrush => new SolidColorBrush(Color.Parse(ColorComunicacionIconoHex));

    // Video crop normalizado (0-1)
    public double VideoCropX { get; set; }
    public double VideoCropY { get; set; }
    public double VideoCropW { get; set; }
    public double VideoCropH { get; set; }

    // Propiedad para saber si esta noticia está expandida
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxHeightPanel))]
    [NotifyPropertyChangedFor(nameof(OpacidadPanel))]
    private bool _estaExpandida = false;

    // Propiedad para saber si esta noticia es el slide activo del carrusel
    [ObservableProperty]
    private bool _esSlideActivo = false;

    public string TextoBotonExpandir => EstaExpandida ? "Leer menos" : "Leer mas";
    public double MaxHeightPanel => EstaExpandida ? 500 : 0;
    public double OpacidadPanel => EstaExpandida ? 1.0 : 0.0;

    partial void OnEstaExpandidaChanged(bool value)
    {
        OnPropertyChanged(nameof(TextoBotonExpandir));
    }

    [RelayCommand]
    private void ToggleExpandir()
    {
        if (EsDestacada)
        {
            ParentViewModel?.SeleccionarNoticiaDestacada(this);
        }
        else
        {
            ParentViewModel?.SeleccionarNoticiaAdicional(this);
        }
    }

    [RelayCommand]
    private void AbrirEnlace()
    {
        if (string.IsNullOrWhiteSpace(EnlaceUrl)) return;

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = EnlaceUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            // Ignorar errores al abrir el enlace
        }
    }

    public void CargarImagenBitmap()
    {
        if (ImagenData != null && ImagenData.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(ImagenData);
                var imagenOriginal = new Bitmap(ms);

                // Si hay valores de crop válidos, aplicar el recorte
                if (CropWidth > 0 && CropHeight > 0)
                {
                    ImagenBitmap = AplicarRecorte(imagenOriginal);
                }
                else
                {
                    ImagenBitmap = imagenOriginal;
                }
            }
            catch
            {
                ImagenBitmap = null;
            }
        }
    }

    private Bitmap? AplicarRecorte(Bitmap imagenOriginal)
    {
        try
        {
            // Calcular cómo se escala la imagen al canvas 600x400 con Stretch=Uniform
            double imgWidth = imagenOriginal.PixelSize.Width;
            double imgHeight = imagenOriginal.PixelSize.Height;

            // Factor de escala para que la imagen quepa en el canvas manteniendo proporción
            double scaleX = CANVAS_WIDTH / imgWidth;
            double scaleY = CANVAS_HEIGHT / imgHeight;
            double scale = Math.Min(scaleX, scaleY);

            // Tamaño de la imagen escalada en el canvas
            double scaledWidth = imgWidth * scale;
            double scaledHeight = imgHeight * scale;

            // Offset de la imagen en el canvas (centrada)
            double offsetX = (CANVAS_WIDTH - scaledWidth) / 2;
            double offsetY = (CANVAS_HEIGHT - scaledHeight) / 2;

            // Calcular la intersección del área de crop con el área de la imagen en el canvas
            double cropLeft = CropX;
            double cropTop = CropY;
            double cropRight = CropX + CropWidth;
            double cropBottom = CropY + CropHeight;

            // Límites de la imagen en el canvas
            double imgLeft = offsetX;
            double imgTop = offsetY;
            double imgRight = offsetX + scaledWidth;
            double imgBottom = offsetY + scaledHeight;

            // Calcular intersección (clamp del crop al área de la imagen)
            double intersectLeft = Math.Max(cropLeft, imgLeft);
            double intersectTop = Math.Max(cropTop, imgTop);
            double intersectRight = Math.Min(cropRight, imgRight);
            double intersectBottom = Math.Min(cropBottom, imgBottom);

            // Verificar que hay intersección válida
            if (intersectRight <= intersectLeft || intersectBottom <= intersectTop)
            {
                return imagenOriginal;
            }

            // Convertir coordenadas de intersección a coordenadas relativas a la imagen escalada
            double relX = intersectLeft - offsetX;
            double relY = intersectTop - offsetY;
            double relWidth = intersectRight - intersectLeft;
            double relHeight = intersectBottom - intersectTop;

            // Convertir a pixeles reales de la imagen original
            int realX = (int)(relX / scale);
            int realY = (int)(relY / scale);
            int realWidth = (int)(relWidth / scale);
            int realHeight = (int)(relHeight / scale);

            // Asegurar que los valores están dentro de los límites
            realX = Math.Max(0, Math.Min(realX, (int)imgWidth - 1));
            realY = Math.Max(0, Math.Min(realY, (int)imgHeight - 1));
            realWidth = Math.Max(1, Math.Min(realWidth, (int)imgWidth - realX));
            realHeight = Math.Max(1, Math.Min(realHeight, (int)imgHeight - realY));

            // Crear un nuevo bitmap con solo el área recortada
            using var renderTarget = new Avalonia.Media.Imaging.RenderTargetBitmap(
                new Avalonia.PixelSize(realWidth, realHeight));

            using (var ctx = renderTarget.CreateDrawingContext())
            {
                var sourceRect = new Avalonia.Rect(realX, realY, realWidth, realHeight);
                var destRect = new Avalonia.Rect(0, 0, realWidth, realHeight);
                ctx.DrawImage(imagenOriginal, sourceRect, destRect);
            }

            // Convertir a Bitmap normal
            using var stream = new MemoryStream();
            renderTarget.Save(stream);
            stream.Position = 0;

            return new Bitmap(stream);
        }
        catch
        {
            return imagenOriginal;
        }
    }
}