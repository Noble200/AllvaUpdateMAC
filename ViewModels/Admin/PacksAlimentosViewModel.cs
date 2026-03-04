using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Models;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class PacksAlimentosViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        // ============================================
        // PROPIEDADES OBSERVABLES
        // ============================================

        [ObservableProperty]
        private ObservableCollection<PackAlimento> _packs = new();

        [ObservableProperty]
        private PackAlimento? _packSeleccionado;

        [ObservableProperty]
        private bool _estaCargando;

        [ObservableProperty]
        private string _mensajeEstado = string.Empty;

        [ObservableProperty]
        private bool _hayMensaje;

        [ObservableProperty]
        private bool _mensajeEsError;

        // Panel de creacion/edicion
        [ObservableProperty]
        private bool _mostrarPanelEdicion;

        [ObservableProperty]
        private bool _modoEdicion;

        [ObservableProperty]
        private string _tituloPanel = "Nuevo Pack de Alimentos";

        // Datos del formulario
        [ObservableProperty]
        private string _nombrePack = string.Empty;

        [ObservableProperty]
        private string _descripcionPack = string.Empty;

        [ObservableProperty]
        private byte[]? _imagenPoster;

        [ObservableProperty]
        private string? _nombreImagenPoster;

        // Imagen original sin recortar (para poder reencuadrar)
        private byte[]? _imagenPosterOriginal;

        // Propiedades para el panel de encuadre circular
        [ObservableProperty]
        private bool _mostrarPanelEncuadrePoster;

        [ObservableProperty]
        private double _cropPosterX = 0;

        [ObservableProperty]
        private double _cropPosterY = 0;

        [ObservableProperty]
        private double _cropPosterSize = 300;

        [ObservableProperty]
        private double _zoomPosterLevel = 1.0;

        [ObservableProperty]
        private Bitmap? _imagenPosterParaEncuadre;

        // Constantes del canvas de encuadre poster
        private const double POSTER_CANVAS_WIDTH = 500;
        private const double POSTER_CANVAS_HEIGHT = 500;
        private const double MIN_CROP_POSTER_SIZE = 50;

        // Propiedades calculadas para el crop circular
        public Avalonia.Thickness CropPosterMargin => new Avalonia.Thickness(CropPosterX, CropPosterY, 0, 0);
        public double CropPosterImageOffsetXConZoom => (POSTER_CANVAS_WIDTH / 2) * (1 - ZoomPosterLevel) - CropPosterX;
        public double CropPosterImageOffsetYConZoom => (POSTER_CANVAS_HEIGHT / 2) * (1 - ZoomPosterLevel) - CropPosterY;
        public double ImagenPosterBackgroundLeftConZoom => (POSTER_CANVAS_WIDTH / 2) * (1 - ZoomPosterLevel);
        public double ImagenPosterBackgroundTopConZoom => (POSTER_CANVAS_HEIGHT / 2) * (1 - ZoomPosterLevel);

        public bool TieneImagenPosterParaEncuadre => ImagenPoster != null && ImagenPoster.Length > 0;

        [ObservableProperty]
        private ObservableCollection<PackAlimentoProducto> _productosActuales = new();

        [ObservableProperty]
        private ObservableCollection<PackAlimentoImagen> _imagenesActuales = new();

        // Formulario de nuevo producto
        [ObservableProperty]
        private string _nuevoProductoNombre = string.Empty;

        [ObservableProperty]
        private string _nuevoProductoDescripcion = string.Empty;

        [ObservableProperty]
        private string _nuevoProductoDetalles = string.Empty;

        [ObservableProperty]
        private string _nuevoProductoCantidadTexto = "1";

        [ObservableProperty]
        private string _nuevoProductoUnidad = "unidad";

        [ObservableProperty]
        private byte[]? _nuevoProductoImagen;

        [ObservableProperty]
        private string? _nuevoProductoImagenNombre;

        // Edicion de producto existente
        [ObservableProperty]
        private PackAlimentoProducto? _productoEnEdicion;

        [ObservableProperty]
        private bool _esModoEdicionProducto = false;

        public string TextoBotonProducto => EsModoEdicionProducto ? "Actualizar Producto" : "Agregar Producto";
        public string TituloFormularioProducto => EsModoEdicionProducto ? "Editar Producto" : "Agregar Producto";

        // ASIGNACION A COMERCIOS (integrada en creacion)
        [ObservableProperty]
        private bool _asignarATodosLosComercios = true;

        [ObservableProperty]
        private ObservableCollection<ComercioConPrecio> _comerciosParaAsignar = new();

        [ObservableProperty]
        private string _precioGeneralTexto = "0.00";

        [ObservableProperty]
        private string _divisaGeneral = "EUR";

        // Modal de detalles del producto
        [ObservableProperty]
        private bool _mostrarModalDetalles;

        [ObservableProperty]
        private PackAlimentoProducto? _productoParaVerDetalles;

        // Modal de imagen ampliada
        [ObservableProperty]
        private bool _mostrarModalImagen;

        [ObservableProperty]
        private byte[]? _imagenAmpliada;

        [ObservableProperty]
        private string? _imagenAmpliadaNombre;

        // Modal de previsualizacion del poster
        [ObservableProperty]
        private bool _mostrarPrevisualizacionPoster;

        // ============================================
        // PROPIEDADES PARA NAVEGACION POR PAISES
        // ============================================

        [ObservableProperty]
        private bool _mostrarVistaPaises = true;

        [ObservableProperty]
        private bool _mostrarVistaPacksPais;

        [ObservableProperty]
        private PaisDesignado? _paisActual;

        [ObservableProperty]
        private ObservableCollection<PackAlimento> _packsPaisActual = new();

        // ============================================
        // PROPIEDADES PARA PAISES DESIGNADOS
        // ============================================

        [ObservableProperty]
        private ObservableCollection<PaisDesignado> _paisesDisponibles = new();

        [ObservableProperty]
        private PaisDesignado? _paisSeleccionado;

        // Modal agregar/editar pais
        [ObservableProperty]
        private bool _mostrarModalPais;

        [ObservableProperty]
        private bool _modoEdicionPais;

        [ObservableProperty]
        private int _paisIdEnEdicion;

        [ObservableProperty]
        private string _tituloModalPais = "Nuevo Pais";

        [ObservableProperty]
        private string _nuevoPaisNombre = string.Empty;

        [ObservableProperty]
        private string _nuevoPaisCodigoIso = string.Empty;

        [ObservableProperty]
        private byte[]? _nuevoPaisBandera;

        [ObservableProperty]
        private string? _nuevoPaisBanderaNombre;

        // ============================================
        // PROPIEDADES PARA CONFIRMACION DE ELIMINACION
        // ============================================

        [ObservableProperty]
        private bool _mostrarModalConfirmacion;

        [ObservableProperty]
        private PackAlimento? _packParaEliminar;

        [ObservableProperty]
        private string _mensajeConfirmacion = string.Empty;

        // ============================================
        // PROPIEDADES PARA CATEGORIAS
        // ============================================

        [ObservableProperty]
        private ObservableCollection<CategoriaPackAlimento> _categoriasPaisActual = new();

        [ObservableProperty]
        private bool _mostrarModalCategoria;

        [ObservableProperty]
        private string _nuevaCategoriaNombre = string.Empty;

        [ObservableProperty]
        private byte[]? _nuevaCategoriaImagen;

        [ObservableProperty]
        private string? _nuevaCategoriaImagenNombre;

        [ObservableProperty]
        private bool _mostrarModalConfirmacionCategoria;

        [ObservableProperty]
        private CategoriaPackAlimento? _categoriaParaEliminar;

        [ObservableProperty]
        private string _mensajeConfirmacionCategoria = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CategoriaSeleccionable> _categoriasParaAsignar = new();

        public bool TieneNuevaCategoriaImagen => NuevaCategoriaImagen != null && NuevaCategoriaImagen.Length > 0;
        public bool HayCategorias => CategoriasPaisActual.Count > 0;

        partial void OnNuevaCategoriaImagenChanged(byte[]? value)
        {
            OnPropertyChanged(nameof(TieneNuevaCategoriaImagen));
        }

        // Unidades de medida disponibles
        public ObservableCollection<string> UnidadesMedida { get; } = new()
        {
            "unidad", "paquete", "kg", "g", "l", "ml", "caja", "docena"
        };

        // Divisas disponibles
        public ObservableCollection<string> DivisasDisponibles { get; } = new() { "EUR", "USD" };

        private int _packIdEnEdicion;

        // ============================================
        // PROPIEDADES CALCULADAS PARA UI
        // ============================================

        public IBrush MensajeBackground => MensajeEsError 
            ? new SolidColorBrush(Color.Parse("#dc3545")) 
            : new SolidColorBrush(Color.Parse("#28a745"));

        public string TextoBotonGuardar => ModoEdicion ? "Actualizar Pack" : "Crear Pack";

        public bool TieneImagenProductoNuevo => NuevoProductoImagen != null && NuevoProductoImagen.Length > 0;

        public bool MostrarSeleccionComercios => !AsignarATodosLosComercios;

        public bool TieneNuevoPaisBandera => NuevoPaisBandera != null && NuevoPaisBandera.Length > 0;

        public bool HayPaisesDisponibles => PaisesDisponibles.Count > 0;

        public string TextoBotonGuardarPais => ModoEdicionPais ? "Actualizar Pais" : "Guardar Pais";

        public string SubtituloModalPais => ModoEdicionPais
            ? "Modifica los datos del pais"
            : "Agrega un pais para organizar tus packs";

        // ============================================
        // CONSTRUCTOR
        // ============================================

        public PacksAlimentosViewModel()
        {
            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            await AsegurarTablaPaisesAsync();
            await AsegurarTablaCategoriasAsync();
            await AsegurarColumnasImagenPosterAsync();
            await AsegurarColumnaCantidadDecimalAsync();
            await CargarPaisesAsync();
            await CargarPacksAsync();
        }

        private async Task AsegurarColumnasImagenPosterAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Verificar y agregar columnas de posicionamiento de imagen
                var columnas = new[] { "imagen_poster_offset_x", "imagen_poster_offset_y", "imagen_poster_zoom" };
                var tipos = new[] { "DOUBLE PRECISION DEFAULT 0", "DOUBLE PRECISION DEFAULT 0", "DOUBLE PRECISION DEFAULT 1.0" };

                for (int i = 0; i < columnas.Length; i++)
                {
                    var checkColumn = $@"
                        SELECT EXISTS (
                            SELECT FROM information_schema.columns
                            WHERE table_name = 'packs_alimentos' AND column_name = '{columnas[i]}'
                        )";

                    await using var checkCmd = new NpgsqlCommand(checkColumn, conn);
                    var exists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);

                    if (!exists)
                    {
                        var addColumn = $"ALTER TABLE packs_alimentos ADD COLUMN {columnas[i]} {tipos[i]}";
                        await using var addCmd = new NpgsqlCommand(addColumn, conn);
                        await addCmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al asegurar columnas de imagen: {ex.Message}");
            }
        }

        private async Task AsegurarColumnaCantidadDecimalAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Verificar si la columna cantidad es INTEGER y cambiarla a NUMERIC
                var checkType = @"
                    SELECT data_type
                    FROM information_schema.columns
                    WHERE table_name = 'pack_alimentos_productos' AND column_name = 'cantidad'";

                await using var checkCmd = new NpgsqlCommand(checkType, conn);
                var dataType = await checkCmd.ExecuteScalarAsync() as string;

                if (dataType != null && dataType.ToLower() == "integer")
                {
                    var alterColumn = "ALTER TABLE pack_alimentos_productos ALTER COLUMN cantidad TYPE NUMERIC(10,2) USING cantidad::NUMERIC(10,2)";
                    await using var alterCmd = new NpgsqlCommand(alterColumn, conn);
                    await alterCmd.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("Columna cantidad actualizada a NUMERIC(10,2)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al asegurar columna cantidad decimal: {ex.Message}");
            }
        }

        // Notificar cambios
        partial void OnModoEdicionChanged(bool value)
        {
            OnPropertyChanged(nameof(TextoBotonGuardar));
        }

        partial void OnMensajeEsErrorChanged(bool value)
        {
            OnPropertyChanged(nameof(MensajeBackground));
        }

        partial void OnNuevoProductoImagenChanged(byte[]? value)
        {
            OnPropertyChanged(nameof(TieneImagenProductoNuevo));
        }

        partial void OnAsignarATodosLosComerciosChanged(bool value)
        {
            OnPropertyChanged(nameof(MostrarSeleccionComercios));
        }

        partial void OnNuevoPaisBanderaChanged(byte[]? value)
        {
            OnPropertyChanged(nameof(TieneNuevoPaisBandera));
        }

        partial void OnModoEdicionPaisChanged(bool value)
        {
            OnPropertyChanged(nameof(TextoBotonGuardarPais));
            OnPropertyChanged(nameof(SubtituloModalPais));
        }

        // ============================================
        // COMANDOS PARA NAVEGACION POR PAISES
        // ============================================

        [RelayCommand]
        private async Task EntrarEnPaisAsync(PaisDesignado? pais)
        {
            if (pais == null) return;

            PaisActual = pais;
            await CargarPacksPorPaisAsync(pais.IdPais);
            await CargarCategoriasPorPaisAsync(pais.IdPais);
            MostrarVistaPaises = false;
            MostrarVistaPacksPais = true;
        }

        [RelayCommand]
        private void VolverAPaises()
        {
            PaisActual = null;
            PacksPaisActual.Clear();
            CategoriasPaisActual.Clear();
            MostrarVistaPacksPais = false;
            MostrarVistaPaises = true;
        }

        private async Task CargarPacksPorPaisAsync(int idPais)
        {
            EstaCargando = true;
            PacksPaisActual.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT pa.id_pack, pa.nombre_pack, pa.descripcion, pa.imagen_poster,
                           pa.imagen_poster_nombre, pa.activo, pa.fecha_creacion, pa.id_pais,
                           pd.nombre_pais, pd.bandera_imagen,
                           COALESCE(pap.precio, pap_global.precio, 0) as precio,
                           COALESCE(pap.divisa, pap_global.divisa, 'EUR') as divisa
                    FROM packs_alimentos pa
                    LEFT JOIN paises_designados pd ON pa.id_pais = pd.id_pais
                    LEFT JOIN pack_alimentos_asignacion_comercios paac
                        ON pa.id_pack = paac.id_pack AND paac.activo = true
                    LEFT JOIN pack_alimentos_precios pap
                        ON paac.id_precio = pap.id_precio
                    LEFT JOIN pack_alimentos_asignacion_global paag
                        ON pa.id_pack = paag.id_pack AND paag.activo = true
                    LEFT JOIN pack_alimentos_precios pap_global
                        ON paag.id_precio = pap_global.id_precio
                    WHERE pa.id_pais = @idPais
                    ORDER BY pa.fecha_creacion DESC";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPais", idPais);
                await using var reader = await cmd.ExecuteReaderAsync();

                var packsIds = new System.Collections.Generic.HashSet<int>();

                while (await reader.ReadAsync())
                {
                    var idPack = reader.GetInt32(0);
                    if (packsIds.Contains(idPack)) continue;
                    packsIds.Add(idPack);

                    var pack = new PackAlimento
                    {
                        IdPack = idPack,
                        NombrePack = reader.GetString(1),
                        Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ImagenPoster = reader.IsDBNull(3) ? null : (byte[])reader["imagen_poster"],
                        ImagenPosterNombre = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Activo = reader.GetBoolean(5),
                        FechaCreacion = reader.GetDateTime(6),
                        IdPais = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                        NombrePais = reader.IsDBNull(8) ? null : reader.GetString(8),
                        BanderaPais = reader.IsDBNull(9) ? null : (byte[])reader["bandera_imagen"],
                        PrecioPack = reader.IsDBNull(10) ? 0 : reader.GetDecimal(10),
                        DivisaPack = reader.IsDBNull(11) ? "EUR" : reader.GetString(11)
                    };

                    PacksPaisActual.Add(pack);
                }

                await reader.CloseAsync();

                // Cargar productos e imagenes de cada pack
                foreach (var pack in PacksPaisActual)
                {
                    await CargarDetallesPackAsync(conn, pack);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar packs: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task CargarDetallesPackAsync(NpgsqlConnection conn, PackAlimento pack)
        {
            var prodQuery = @"
                SELECT id_producto, nombre_producto, descripcion, detalles, cantidad, unidad_medida, orden, imagen, imagen_nombre, imagen_tipo
                FROM pack_alimentos_productos
                WHERE id_pack = @idPack
                ORDER BY orden";

            await using var prodCmd = new NpgsqlCommand(prodQuery, conn);
            prodCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
            await using var prodReader = await prodCmd.ExecuteReaderAsync();

            while (await prodReader.ReadAsync())
            {
                pack.Productos.Add(new PackAlimentoProducto
                {
                    IdProducto = prodReader.GetInt32(0),
                    IdPack = pack.IdPack,
                    NombreProducto = prodReader.GetString(1),
                    Descripcion = prodReader.IsDBNull(2) ? null : prodReader.GetString(2),
                    Detalles = prodReader.IsDBNull(3) ? null : prodReader.GetString(3),
                    Cantidad = prodReader.GetDecimal(4),
                    UnidadMedida = prodReader.GetString(5),
                    Orden = prodReader.GetInt32(6),
                    Imagen = prodReader.IsDBNull(7) ? null : (byte[])prodReader["imagen"],
                    ImagenNombre = prodReader.IsDBNull(8) ? null : prodReader.GetString(8),
                    ImagenTipo = prodReader.IsDBNull(9) ? null : prodReader.GetString(9)
                });
            }

            await prodReader.CloseAsync();

            var imgQuery = @"
                SELECT id_imagen, imagen_contenido, imagen_nombre, imagen_tipo, descripcion, orden
                FROM pack_alimentos_imagenes
                WHERE id_pack = @idPack
                ORDER BY orden";

            await using var imgCmd = new NpgsqlCommand(imgQuery, conn);
            imgCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
            await using var imgReader = await imgCmd.ExecuteReaderAsync();

            while (await imgReader.ReadAsync())
            {
                pack.Imagenes.Add(new PackAlimentoImagen
                {
                    IdImagen = imgReader.GetInt32(0),
                    IdPack = pack.IdPack,
                    ImagenContenido = (byte[])imgReader["imagen_contenido"],
                    ImagenNombre = imgReader.IsDBNull(2) ? null : imgReader.GetString(2),
                    ImagenTipo = imgReader.IsDBNull(3) ? null : imgReader.GetString(3),
                    Descripcion = imgReader.IsDBNull(4) ? null : imgReader.GetString(4),
                    Orden = imgReader.GetInt32(5)
                });
            }
        }

        // ============================================
        // COMANDOS PARA PAISES DESIGNADOS
        // ============================================

        [RelayCommand]
        private void MostrarFormularioPais()
        {
            ModoEdicionPais = false;
            PaisIdEnEdicion = 0;
            TituloModalPais = "Nuevo Pais";
            NuevoPaisNombre = string.Empty;
            NuevoPaisCodigoIso = string.Empty;
            NuevoPaisBandera = null;
            NuevoPaisBanderaNombre = null;
            MostrarModalPais = true;
        }

        [RelayCommand]
        private void CerrarModalPais()
        {
            MostrarModalPais = false;
            ModoEdicionPais = false;
            PaisIdEnEdicion = 0;
            TituloModalPais = "Nuevo Pais";
            NuevoPaisNombre = string.Empty;
            NuevoPaisCodigoIso = string.Empty;
            NuevoPaisBandera = null;
            NuevoPaisBanderaNombre = null;
        }

        [RelayCommand]
        private async Task SeleccionarBanderaPaisAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen de bandera",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                NuevoPaisBandera = ms.ToArray();
                NuevoPaisBanderaNombre = file.Name;
            }
        }

        [RelayCommand]
        private void QuitarBanderaPais()
        {
            NuevoPaisBandera = null;
            NuevoPaisBanderaNombre = null;
        }

        [RelayCommand]
        private void EditarPais(PaisDesignado? pais)
        {
            if (pais == null) return;

            ModoEdicionPais = true;
            PaisIdEnEdicion = pais.IdPais;
            TituloModalPais = "Editar Pais";
            NuevoPaisNombre = pais.NombrePais;
            NuevoPaisCodigoIso = pais.CodigoIso ?? string.Empty;
            NuevoPaisBandera = pais.BanderaImagen;
            NuevoPaisBanderaNombre = pais.BanderaNombre;
            MostrarModalPais = true;
        }

        [RelayCommand]
        private async Task GuardarPaisAsync()
        {
            if (string.IsNullOrWhiteSpace(NuevoPaisNombre))
            {
                MostrarMensaje("El nombre del pais es requerido", true);
                return;
            }

            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                if (ModoEdicionPais)
                {
                    // Actualizar pais existente
                    var updateQuery = @"
                        UPDATE paises_designados
                        SET nombre_pais = @nombre,
                            codigo_iso = @codigo,
                            bandera_imagen = @bandera,
                            bandera_nombre = @banderaNombre,
                            fecha_modificacion = CURRENT_TIMESTAMP
                        WHERE id_pais = @idPais";

                    await using var cmd = new NpgsqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@nombre", NuevoPaisNombre.Trim());
                    cmd.Parameters.AddWithValue("@codigo", string.IsNullOrWhiteSpace(NuevoPaisCodigoIso) ? DBNull.Value : NuevoPaisCodigoIso.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@bandera", (object?)NuevoPaisBandera ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@banderaNombre", (object?)NuevoPaisBanderaNombre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@idPais", PaisIdEnEdicion);

                    await cmd.ExecuteNonQueryAsync();

                    MostrarMensaje($"Pais '{NuevoPaisNombre}' actualizado correctamente", false);
                }
                else
                {
                    // Insertar nuevo pais
                    var insertQuery = @"
                        INSERT INTO paises_designados (nombre_pais, codigo_iso, bandera_imagen, bandera_nombre)
                        VALUES (@nombre, @codigo, @bandera, @banderaNombre)
                        RETURNING id_pais";

                    await using var cmd = new NpgsqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@nombre", NuevoPaisNombre.Trim());
                    cmd.Parameters.AddWithValue("@codigo", string.IsNullOrWhiteSpace(NuevoPaisCodigoIso) ? DBNull.Value : NuevoPaisCodigoIso.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@bandera", (object?)NuevoPaisBandera ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@banderaNombre", (object?)NuevoPaisBanderaNombre ?? DBNull.Value);

                    await cmd.ExecuteScalarAsync();

                    MostrarMensaje($"Pais '{NuevoPaisNombre}' agregado correctamente", false);
                }

                CerrarModalPais();
                await CargarPaisesAsync();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al guardar pais: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task CargarPaisesAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                PaisesDisponibles.Clear();

                var query = @"
                    SELECT pd.id_pais, pd.nombre_pais, pd.codigo_iso, pd.bandera_imagen, pd.bandera_nombre, pd.activo, pd.fecha_creacion,
                           (SELECT COUNT(*) FROM packs_alimentos pa WHERE pa.id_pais = pd.id_pais) as cantidad_packs
                    FROM paises_designados pd
                    WHERE pd.activo = true
                    ORDER BY pd.nombre_pais";

                await using var cmd = new NpgsqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    PaisesDisponibles.Add(new PaisDesignado
                    {
                        IdPais = reader.GetInt32(0),
                        NombrePais = reader.GetString(1),
                        CodigoIso = reader.IsDBNull(2) ? null : reader.GetString(2),
                        BanderaImagen = reader.IsDBNull(3) ? null : (byte[])reader["bandera_imagen"],
                        BanderaNombre = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Activo = reader.GetBoolean(5),
                        FechaCreacion = reader.GetDateTime(6),
                        CantidadPacks = reader.GetInt32(7)
                    });
                }

                OnPropertyChanged(nameof(HayPaisesDisponibles));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar paises: {ex.Message}");
            }
        }

        private async Task AsegurarTablaPaisesAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Verificar si existe la tabla paises_designados
                var checkTable = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_name = 'paises_designados'
                    )";

                await using var checkCmd = new NpgsqlCommand(checkTable, conn);
                var exists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);

                if (!exists)
                {
                    var createTable = @"
                        CREATE TABLE paises_designados (
                            id_pais SERIAL PRIMARY KEY,
                            nombre_pais VARCHAR(100) NOT NULL,
                            codigo_iso VARCHAR(3),
                            bandera_imagen BYTEA,
                            bandera_nombre VARCHAR(255),
                            activo BOOLEAN DEFAULT true,
                            fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            fecha_modificacion TIMESTAMP
                        )";

                    await using var createCmd = new NpgsqlCommand(createTable, conn);
                    await createCmd.ExecuteNonQueryAsync();
                }

                // Verificar si packs_alimentos tiene la columna id_pais
                var checkColumn = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'packs_alimentos' AND column_name = 'id_pais'
                    )";

                await using var checkColCmd = new NpgsqlCommand(checkColumn, conn);
                var columnExists = (bool)(await checkColCmd.ExecuteScalarAsync() ?? false);

                if (!columnExists)
                {
                    var addColumn = "ALTER TABLE packs_alimentos ADD COLUMN id_pais INTEGER REFERENCES paises_designados(id_pais)";
                    await using var addColCmd = new NpgsqlCommand(addColumn, conn);
                    await addColCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al asegurar tabla paises: {ex.Message}");
            }
        }

        private async Task AsegurarTablaCategoriasAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var checkTable = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_name = 'categorias_pack_alimentos'
                    )";

                await using var checkCmd = new NpgsqlCommand(checkTable, conn);
                var exists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);

                if (!exists)
                {
                    var createTable = @"
                        CREATE TABLE categorias_pack_alimentos (
                            id_categoria SERIAL PRIMARY KEY,
                            id_pais INTEGER NOT NULL REFERENCES paises_designados(id_pais) ON DELETE CASCADE,
                            nombre_categoria VARCHAR(150) NOT NULL,
                            imagen_categoria BYTEA,
                            imagen_categoria_nombre VARCHAR(255),
                            fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";

                    await using var createCmd = new NpgsqlCommand(createTable, conn);
                    await createCmd.ExecuteNonQueryAsync();
                }

                var checkJunction = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_name = 'pack_categoria_asignacion'
                    )";

                await using var checkJCmd = new NpgsqlCommand(checkJunction, conn);
                var junctionExists = (bool)(await checkJCmd.ExecuteScalarAsync() ?? false);

                if (!junctionExists)
                {
                    var createJunction = @"
                        CREATE TABLE pack_categoria_asignacion (
                            id_pack INTEGER NOT NULL REFERENCES packs_alimentos(id_pack) ON DELETE CASCADE,
                            id_categoria INTEGER NOT NULL REFERENCES categorias_pack_alimentos(id_categoria) ON DELETE CASCADE,
                            fecha_asignacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            PRIMARY KEY (id_pack, id_categoria)
                        )";

                    await using var createJCmd = new NpgsqlCommand(createJunction, conn);
                    await createJCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al asegurar tablas categorias: {ex.Message}");
            }
        }

        // ============================================
        // COMANDOS CATEGORIAS
        // ============================================

        private async Task CargarCategoriasPorPaisAsync(int idPais)
        {
            CategoriasPaisActual.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT c.id_categoria, c.nombre_categoria, c.imagen_categoria,
                           c.imagen_categoria_nombre, c.fecha_creacion,
                           (SELECT COUNT(*) FROM pack_categoria_asignacion pca
                            WHERE pca.id_categoria = c.id_categoria) as cantidad_packs
                    FROM categorias_pack_alimentos c
                    WHERE c.id_pais = @idPais
                    ORDER BY c.nombre_categoria";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPais", idPais);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    CategoriasPaisActual.Add(new CategoriaPackAlimento
                    {
                        IdCategoria = reader.GetInt32(0),
                        NombreCategoria = reader.GetString(1),
                        ImagenCategoria = reader.IsDBNull(2) ? null : (byte[])reader["imagen_categoria"],
                        ImagenCategoriaNombre = reader.IsDBNull(3) ? null : reader.GetString(3),
                        FechaCreacion = reader.GetDateTime(4),
                        CantidadPacks = reader.GetInt32(5),
                        IdPais = idPais
                    });
                }

                OnPropertyChanged(nameof(HayCategorias));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar categorias: {ex.Message}");
            }
        }

        [RelayCommand]
        private void MostrarFormularioCategoria()
        {
            NuevaCategoriaNombre = string.Empty;
            NuevaCategoriaImagen = null;
            NuevaCategoriaImagenNombre = null;
            MostrarModalCategoria = true;
        }

        [RelayCommand]
        private void CerrarModalCategoria()
        {
            MostrarModalCategoria = false;
            NuevaCategoriaNombre = string.Empty;
            NuevaCategoriaImagen = null;
            NuevaCategoriaImagenNombre = null;
        }

        [RelayCommand]
        private async Task SeleccionarImagenCategoriaAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen de categoría",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.webp" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                NuevaCategoriaImagen = ms.ToArray();
                NuevaCategoriaImagenNombre = file.Name;
            }
        }

        [RelayCommand]
        private void QuitarImagenCategoria()
        {
            NuevaCategoriaImagen = null;
            NuevaCategoriaImagenNombre = null;
        }

        [RelayCommand]
        private async Task GuardarCategoriaAsync()
        {
            if (PaisActual == null)
            {
                MostrarMensaje("Error: no hay país seleccionado", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(NuevaCategoriaNombre))
            {
                MostrarMensaje("El nombre de la categoría es requerido", true);
                return;
            }

            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var insertQuery = @"
                    INSERT INTO categorias_pack_alimentos (id_pais, nombre_categoria, imagen_categoria, imagen_categoria_nombre)
                    VALUES (@idPais, @nombre, @imagen, @imagenNombre)
                    RETURNING id_categoria";

                await using var cmd = new NpgsqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@idPais", PaisActual.IdPais);
                cmd.Parameters.AddWithValue("@nombre", NuevaCategoriaNombre.Trim());
                cmd.Parameters.AddWithValue("@imagen", (object?)NuevaCategoriaImagen ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@imagenNombre", (object?)NuevaCategoriaImagenNombre ?? DBNull.Value);

                await cmd.ExecuteScalarAsync();

                MostrarMensaje($"Categoría '{NuevaCategoriaNombre}' creada correctamente", false);
                CerrarModalCategoria();
                await CargarCategoriasPorPaisAsync(PaisActual.IdPais);
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al crear categoría: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        [RelayCommand]
        private void MostrarConfirmacionEliminarCategoria(CategoriaPackAlimento? categoria)
        {
            if (categoria == null) return;

            CategoriaParaEliminar = categoria;
            MensajeConfirmacionCategoria = $"¿Está seguro que desea eliminar la categoría \"{categoria.NombreCategoria}\"?\n\nLos packs NO serán eliminados, solo se quitará la categoría.\nEsta acción no se puede deshacer.";
            MostrarModalConfirmacionCategoria = true;
        }

        [RelayCommand]
        private void CancelarEliminacionCategoria()
        {
            MostrarModalConfirmacionCategoria = false;
            CategoriaParaEliminar = null;
            MensajeConfirmacionCategoria = string.Empty;
        }

        [RelayCommand]
        private async Task ConfirmarEliminacionCategoriaAsync()
        {
            if (CategoriaParaEliminar == null) return;

            MostrarModalConfirmacionCategoria = false;
            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    "DELETE FROM categorias_pack_alimentos WHERE id_categoria = @idCategoria", conn);
                cmd.Parameters.AddWithValue("@idCategoria", CategoriaParaEliminar.IdCategoria);
                await cmd.ExecuteNonQueryAsync();

                MostrarMensaje("Categoría eliminada correctamente", false);

                if (PaisActual != null)
                {
                    await CargarCategoriasPorPaisAsync(PaisActual.IdPais);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al eliminar categoría: {ex.Message}", true);
            }
            finally
            {
                CategoriaParaEliminar = null;
                MensajeConfirmacionCategoria = string.Empty;
                EstaCargando = false;
            }
        }

        private async Task CargarCategoriasParaAsignarAsync(int idPais, int? idPackExistente = null)
        {
            CategoriasParaAsignar.Clear();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT c.id_categoria, c.nombre_categoria, c.imagen_categoria
                    FROM categorias_pack_alimentos c
                    WHERE c.id_pais = @idPais
                    ORDER BY c.nombre_categoria";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPais", idPais);
                await using var reader = await cmd.ExecuteReaderAsync();

                var categorias = new System.Collections.Generic.List<CategoriaSeleccionable>();
                while (await reader.ReadAsync())
                {
                    categorias.Add(new CategoriaSeleccionable
                    {
                        IdCategoria = reader.GetInt32(0),
                        NombreCategoria = reader.GetString(1),
                        ImagenCategoria = reader.IsDBNull(2) ? null : (byte[])reader["imagen_categoria"],
                        Seleccionada = false
                    });
                }
                await reader.CloseAsync();

                if (idPackExistente.HasValue)
                {
                    var asignQuery = "SELECT id_categoria FROM pack_categoria_asignacion WHERE id_pack = @idPack";
                    await using var asignCmd = new NpgsqlCommand(asignQuery, conn);
                    asignCmd.Parameters.AddWithValue("@idPack", idPackExistente.Value);
                    await using var asignReader = await asignCmd.ExecuteReaderAsync();

                    var asignadas = new System.Collections.Generic.HashSet<int>();
                    while (await asignReader.ReadAsync())
                    {
                        asignadas.Add(asignReader.GetInt32(0));
                    }

                    foreach (var cat in categorias)
                    {
                        cat.Seleccionada = asignadas.Contains(cat.IdCategoria);
                    }
                }

                foreach (var cat in categorias)
                {
                    CategoriasParaAsignar.Add(cat);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar categorias para asignar: {ex.Message}");
            }
        }

        // ============================================
        // COMANDOS DE CONFIRMACION DE ELIMINACION
        // ============================================

        [RelayCommand]
        private void MostrarConfirmacionEliminar(PackAlimento? pack)
        {
            if (pack == null) return;

            PackParaEliminar = pack;
            MensajeConfirmacion = $"Esta seguro que desea eliminar el pack \"{pack.NombrePack}\"?\n\nEsta accion no se puede deshacer.";
            MostrarModalConfirmacion = true;
        }

        [RelayCommand]
        private void CancelarEliminacion()
        {
            MostrarModalConfirmacion = false;
            PackParaEliminar = null;
            MensajeConfirmacion = string.Empty;
        }

        [RelayCommand]
        private async Task ConfirmarEliminacionAsync()
        {
            if (PackParaEliminar == null) return;

            MostrarModalConfirmacion = false;
            await EliminarPackInternoAsync(PackParaEliminar);
            PackParaEliminar = null;
            MensajeConfirmacion = string.Empty;
        }

        // ============================================
        // COMANDOS PRINCIPALES
        // ============================================

        [RelayCommand]
        private async Task NuevoPackAsync()
        {
            LimpiarFormulario();
            ModoEdicion = false;
            TituloPanel = PaisActual != null
                ? $"Nuevo Pack para {PaisActual.NombrePais}"
                : "Nuevo Pack de Alimentos";
            await CargarComerciosAsync();
            await CargarPaisesAsync();

            // Pre-seleccionar el pais actual si estamos dentro de uno
            if (PaisActual != null)
            {
                PaisSeleccionado = PaisesDisponibles.FirstOrDefault(p => p.IdPais == PaisActual.IdPais);
                await CargarCategoriasParaAsignarAsync(PaisActual.IdPais);
            }

            MostrarVistaPacksPais = false;
            MostrarVistaPaises = false;
            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private async Task EditarPackAsync(PackAlimento? pack)
        {
            if (pack == null) return;

            _packIdEnEdicion = pack.IdPack;
            NombrePack = pack.NombrePack;
            DescripcionPack = pack.Descripcion ?? string.Empty;
            ImagenPoster = pack.ImagenPoster;
            NombreImagenPoster = pack.ImagenPosterNombre;
            _imagenPosterOriginal = null; // Se cargara desde la BD si existe

            ProductosActuales.Clear();
            foreach (var p in pack.Productos)
            {
                ProductosActuales.Add(p);
            }

            ImagenesActuales.Clear();
            foreach (var img in pack.Imagenes)
            {
                ImagenesActuales.Add(img);
            }

            await CargarComerciosAsync();
            await CargarPaisesAsync();
            await CargarAsignacionesExistentesAsync(pack.IdPack);

            // Seleccionar el pais del pack
            if (pack.IdPais.HasValue)
            {
                PaisSeleccionado = PaisesDisponibles.FirstOrDefault(p => p.IdPais == pack.IdPais.Value);
                await CargarCategoriasParaAsignarAsync(pack.IdPais.Value, pack.IdPack);
            }
            else
            {
                PaisSeleccionado = null;
            }

            ModoEdicion = true;
            TituloPanel = $"Editar: {pack.NombrePack}";
            MostrarVistaPacksPais = false;
            MostrarVistaPaises = false;
            MostrarPanelEdicion = true;
        }

        [RelayCommand]
        private async Task CancelarEdicion()
        {
            LimpiarFormulario();
            MostrarPanelEdicion = false;

            // Volver a la vista correcta
            if (PaisActual != null)
            {
                await CargarPacksPorPaisAsync(PaisActual.IdPais);
                MostrarVistaPacksPais = true;
            }
            else
            {
                await CargarPaisesAsync();
                MostrarVistaPaises = true;
            }
        }

        [RelayCommand]
        private async Task GuardarPackAsync()
        {
            if (string.IsNullOrWhiteSpace(NombrePack))
            {
                MostrarMensaje("El nombre del pack es requerido", true);
                return;
            }

            if (ProductosActuales.Count == 0)
            {
                MostrarMensaje("Debe agregar al menos un producto al pack", true);
                return;
            }

            // Validar precio
            if (!decimal.TryParse(PrecioGeneralTexto.Replace(",", "."), System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out decimal precioGeneral) || precioGeneral <= 0)
            {
                MostrarMensaje("El precio debe ser mayor a 0", true);
                return;
            }

            // Validar comercios seleccionados
            if (!AsignarATodosLosComercios)
            {
                var comerciosSeleccionados = ComerciosParaAsignar.Where(c => c.Seleccionado).ToList();
                if (comerciosSeleccionados.Count == 0)
                {
                    MostrarMensaje("Debe seleccionar al menos un comercio", true);
                    return;
                }
            }

            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                int idPack;

                if (ModoEdicion)
                {
                    var updateQuery = @"
                        UPDATE packs_alimentos
                        SET nombre_pack = @nombre,
                            descripcion = @descripcion,
                            imagen_poster = @imagen,
                            imagen_poster_nombre = @imagenNombre,
                            id_pais = @idPais,
                            fecha_modificacion = CURRENT_TIMESTAMP
                        WHERE id_pack = @idPack";

                    await using var cmd = new NpgsqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@nombre", NombrePack);
                    cmd.Parameters.AddWithValue("@descripcion", (object?)DescripcionPack ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagen", (object?)ImagenPoster ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagenNombre", (object?)NombreImagenPoster ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@idPais", PaisSeleccionado?.IdPais ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@idPack", _packIdEnEdicion);
                    await cmd.ExecuteNonQueryAsync();

                    idPack = _packIdEnEdicion;

                    // Limpiar datos anteriores
                    await using var deleteCmd = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_productos WHERE id_pack = @idPack", conn);
                    deleteCmd.Parameters.AddWithValue("@idPack", idPack);
                    await deleteCmd.ExecuteNonQueryAsync();

                    await using var deleteImgCmd = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_imagenes WHERE id_pack = @idPack", conn);
                    deleteImgCmd.Parameters.AddWithValue("@idPack", idPack);
                    await deleteImgCmd.ExecuteNonQueryAsync();

                    // Limpiar asignaciones anteriores
                    await using var deleteAsigGlobal = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_asignacion_global WHERE id_pack = @idPack", conn);
                    deleteAsigGlobal.Parameters.AddWithValue("@idPack", idPack);
                    await deleteAsigGlobal.ExecuteNonQueryAsync();

                    await using var deleteAsigCom = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_asignacion_comercios WHERE id_pack = @idPack", conn);
                    deleteAsigCom.Parameters.AddWithValue("@idPack", idPack);
                    await deleteAsigCom.ExecuteNonQueryAsync();

                    await using var deletePrecios = new NpgsqlCommand(
                        "DELETE FROM pack_alimentos_precios WHERE id_pack = @idPack", conn);
                    deletePrecios.Parameters.AddWithValue("@idPack", idPack);
                    await deletePrecios.ExecuteNonQueryAsync();
                }
                else
                {
                    var insertQuery = @"
                        INSERT INTO packs_alimentos (nombre_pack, descripcion, imagen_poster, imagen_poster_nombre, id_pais)
                        VALUES (@nombre, @descripcion, @imagen, @imagenNombre, @idPais)
                        RETURNING id_pack";

                    await using var cmd = new NpgsqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@nombre", NombrePack);
                    cmd.Parameters.AddWithValue("@descripcion", (object?)DescripcionPack ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagen", (object?)ImagenPoster ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@imagenNombre", (object?)NombreImagenPoster ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@idPais", PaisSeleccionado?.IdPais ?? (object)DBNull.Value);

                    var result = await cmd.ExecuteScalarAsync();
                    idPack = Convert.ToInt32(result);
                }

                // Insertar productos
                int orden = 0;
                foreach (var producto in ProductosActuales)
                {
                    var insertProd = @"
                        INSERT INTO pack_alimentos_productos 
                        (id_pack, nombre_producto, descripcion, detalles, cantidad, unidad_medida, orden, imagen, imagen_nombre, imagen_tipo)
                        VALUES (@idPack, @nombre, @descripcion, @detalles, @cantidad, @unidad, @orden, @imagen, @imagenNombre, @imagenTipo)";

                    await using var prodCmd = new NpgsqlCommand(insertProd, conn);
                    prodCmd.Parameters.AddWithValue("@idPack", idPack);
                    prodCmd.Parameters.AddWithValue("@nombre", producto.NombreProducto);
                    prodCmd.Parameters.AddWithValue("@descripcion", (object?)producto.Descripcion ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@detalles", (object?)producto.Detalles ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                    prodCmd.Parameters.AddWithValue("@unidad", producto.UnidadMedida);
                    prodCmd.Parameters.AddWithValue("@orden", orden++);
                    prodCmd.Parameters.AddWithValue("@imagen", (object?)producto.Imagen ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@imagenNombre", (object?)producto.ImagenNombre ?? DBNull.Value);
                    prodCmd.Parameters.AddWithValue("@imagenTipo", (object?)producto.ImagenTipo ?? DBNull.Value);
                    await prodCmd.ExecuteNonQueryAsync();
                }

                // Insertar imagenes adicionales
                orden = 0;
                foreach (var imagen in ImagenesActuales)
                {
                    var insertImg = @"
                        INSERT INTO pack_alimentos_imagenes 
                        (id_pack, imagen_contenido, imagen_nombre, imagen_tipo, descripcion, orden)
                        VALUES (@idPack, @contenido, @nombre, @tipo, @descripcion, @orden)";

                    await using var imgCmd = new NpgsqlCommand(insertImg, conn);
                    imgCmd.Parameters.AddWithValue("@idPack", idPack);
                    imgCmd.Parameters.AddWithValue("@contenido", imagen.ImagenContenido);
                    imgCmd.Parameters.AddWithValue("@nombre", (object?)imagen.ImagenNombre ?? DBNull.Value);
                    imgCmd.Parameters.AddWithValue("@tipo", (object?)imagen.ImagenTipo ?? DBNull.Value);
                    imgCmd.Parameters.AddWithValue("@descripcion", (object?)imagen.Descripcion ?? DBNull.Value);
                    imgCmd.Parameters.AddWithValue("@orden", orden++);
                    await imgCmd.ExecuteNonQueryAsync();
                }

                // Crear precio
                var insertPrecio = @"
                    INSERT INTO pack_alimentos_precios (id_pack, divisa, precio)
                    VALUES (@idPack, @divisa, @precio)
                    RETURNING id_precio";

                await using var precioCmd = new NpgsqlCommand(insertPrecio, conn);
                precioCmd.Parameters.AddWithValue("@idPack", idPack);
                precioCmd.Parameters.AddWithValue("@divisa", DivisaGeneral);
                precioCmd.Parameters.AddWithValue("@precio", precioGeneral);
                var idPrecio = Convert.ToInt32(await precioCmd.ExecuteScalarAsync());

                // Crear asignaciones
                if (AsignarATodosLosComercios)
                {
                    var insertGlobal = @"
                        INSERT INTO pack_alimentos_asignacion_global (id_pack, id_precio)
                        VALUES (@idPack, @idPrecio)";

                    await using var globalCmd = new NpgsqlCommand(insertGlobal, conn);
                    globalCmd.Parameters.AddWithValue("@idPack", idPack);
                    globalCmd.Parameters.AddWithValue("@idPrecio", idPrecio);
                    await globalCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    foreach (var comercio in ComerciosParaAsignar.Where(c => c.Seleccionado))
                    {
                        var insertCom = @"
                            INSERT INTO pack_alimentos_asignacion_comercios (id_pack, id_comercio, id_precio)
                            VALUES (@idPack, @idComercio, @idPrecio)";

                        await using var comCmd = new NpgsqlCommand(insertCom, conn);
                        comCmd.Parameters.AddWithValue("@idPack", idPack);
                        comCmd.Parameters.AddWithValue("@idComercio", comercio.IdComercio);
                        comCmd.Parameters.AddWithValue("@idPrecio", idPrecio);
                        await comCmd.ExecuteNonQueryAsync();
                    }
                }

                // Guardar asignaciones de categorias
                var deleteCats = new NpgsqlCommand(
                    "DELETE FROM pack_categoria_asignacion WHERE id_pack = @idPack", conn);
                deleteCats.Parameters.AddWithValue("@idPack", idPack);
                await deleteCats.ExecuteNonQueryAsync();

                foreach (var cat in CategoriasParaAsignar.Where(c => c.Seleccionada))
                {
                    var insertCat = @"
                        INSERT INTO pack_categoria_asignacion (id_pack, id_categoria)
                        VALUES (@idPack, @idCategoria)";
                    await using var catCmd = new NpgsqlCommand(insertCat, conn);
                    catCmd.Parameters.AddWithValue("@idPack", idPack);
                    catCmd.Parameters.AddWithValue("@idCategoria", cat.IdCategoria);
                    await catCmd.ExecuteNonQueryAsync();
                }

                MostrarMensaje(ModoEdicion ? "Pack actualizado correctamente" : "Pack creado correctamente", false);
                MostrarPanelEdicion = false;
                LimpiarFormulario();

                // Volver a la vista correcta
                if (PaisActual != null)
                {
                    await CargarPacksPorPaisAsync(PaisActual.IdPais);
                    await CargarCategoriasPorPaisAsync(PaisActual.IdPais);
                    MostrarVistaPacksPais = true;
                }
                else
                {
                    await CargarPaisesAsync();
                    MostrarVistaPaises = true;
                }
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

        private async Task EliminarPackInternoAsync(PackAlimento pack)
        {
            EstaCargando = true;

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    "DELETE FROM packs_alimentos WHERE id_pack = @idPack", conn);
                cmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                await cmd.ExecuteNonQueryAsync();

                MostrarMensaje("Pack eliminado correctamente", false);

                // Recargar la vista actual
                if (PaisActual != null)
                {
                    await CargarPacksPorPaisAsync(PaisActual.IdPais);
                }
                else
                {
                    await CargarPaisesAsync();
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al eliminar: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        // ============================================
        // COMANDOS DE PRODUCTOS
        // ============================================

        [RelayCommand]
        private void AgregarProducto()
        {
            if (string.IsNullOrWhiteSpace(NuevoProductoNombre))
            {
                MostrarMensaje("El nombre del producto es requerido", true);
                return;
            }

            if (!decimal.TryParse(NuevoProductoCantidadTexto.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal cantidad) || cantidad <= 0)
            {
                MostrarMensaje("La cantidad debe ser un numero valido mayor a 0", true);
                return;
            }

            if (EsModoEdicionProducto && ProductoEnEdicion != null)
            {
                // Modo edicion: actualizar producto existente
                ProductoEnEdicion.NombreProducto = NuevoProductoNombre;
                ProductoEnEdicion.Descripcion = NuevoProductoDescripcion;
                ProductoEnEdicion.Detalles = NuevoProductoDetalles;
                ProductoEnEdicion.Cantidad = cantidad;
                ProductoEnEdicion.UnidadMedida = NuevoProductoUnidad;
                ProductoEnEdicion.Imagen = NuevoProductoImagen;
                ProductoEnEdicion.ImagenNombre = NuevoProductoImagenNombre;
                ProductoEnEdicion.ImagenTipo = NuevoProductoImagenNombre != null ? Path.GetExtension(NuevoProductoImagenNombre) : null;

                // Forzar actualizacion de la lista
                var index = ProductosActuales.IndexOf(ProductoEnEdicion);
                if (index >= 0)
                {
                    ProductosActuales.RemoveAt(index);
                    ProductosActuales.Insert(index, ProductoEnEdicion);
                }

                LimpiarFormularioProducto();
                MostrarMensaje("Producto actualizado", false);
            }
            else
            {
                // Modo creacion: agregar nuevo producto
                var producto = new PackAlimentoProducto
                {
                    NombreProducto = NuevoProductoNombre,
                    Descripcion = NuevoProductoDescripcion,
                    Detalles = NuevoProductoDetalles,
                    Cantidad = cantidad,
                    UnidadMedida = NuevoProductoUnidad,
                    Orden = ProductosActuales.Count,
                    Imagen = NuevoProductoImagen,
                    ImagenNombre = NuevoProductoImagenNombre,
                    ImagenTipo = NuevoProductoImagenNombre != null ? Path.GetExtension(NuevoProductoImagenNombre) : null
                };

                ProductosActuales.Add(producto);
                LimpiarFormularioProducto();
                MostrarMensaje("Producto agregado al pack", false);
            }
        }

        [RelayCommand]
        private void EditarProducto(PackAlimentoProducto? producto)
        {
            if (producto == null) return;

            // Cargar datos del producto en el formulario
            ProductoEnEdicion = producto;
            EsModoEdicionProducto = true;
            NuevoProductoNombre = producto.NombreProducto;
            NuevoProductoDescripcion = producto.Descripcion ?? string.Empty;
            NuevoProductoDetalles = producto.Detalles ?? string.Empty;
            NuevoProductoCantidadTexto = producto.Cantidad.ToString();
            NuevoProductoUnidad = producto.UnidadMedida;
            NuevoProductoImagen = producto.Imagen;
            NuevoProductoImagenNombre = producto.ImagenNombre;

            OnPropertyChanged(nameof(TextoBotonProducto));
            OnPropertyChanged(nameof(TituloFormularioProducto));
        }

        [RelayCommand]
        private void CancelarEdicionProducto()
        {
            LimpiarFormularioProducto();
        }

        [RelayCommand]
        private void EliminarProducto(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            ProductosActuales.Remove(producto);
        }

        [RelayCommand]
        private void MoverProductoArriba(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            var index = ProductosActuales.IndexOf(producto);
            if (index > 0)
            {
                ProductosActuales.Move(index, index - 1);
            }
        }

        [RelayCommand]
        private void MoverProductoAbajo(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            var index = ProductosActuales.IndexOf(producto);
            if (index < ProductosActuales.Count - 1)
            {
                ProductosActuales.Move(index, index + 1);
            }
        }

        [RelayCommand]
        private async Task SeleccionarImagenProductoAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen del producto",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                NuevoProductoImagen = ms.ToArray();
                NuevoProductoImagenNombre = file.Name;
            }
        }

        [RelayCommand]
        private void QuitarImagenProducto()
        {
            NuevoProductoImagen = null;
            NuevoProductoImagenNombre = null;
        }

        // ============================================
        // COMANDOS DE MODALES
        // ============================================

        [RelayCommand]
        private void VerDetallesProducto(PackAlimentoProducto? producto)
        {
            if (producto == null) return;
            ProductoParaVerDetalles = producto;
            MostrarModalDetalles = true;
        }

        [RelayCommand]
        private void CerrarModalDetalles()
        {
            MostrarModalDetalles = false;
            ProductoParaVerDetalles = null;
        }

        [RelayCommand]
        private void VerImagenAmpliada(PackAlimentoProducto? producto)
        {
            if (producto?.Imagen == null) return;
            ImagenAmpliada = producto.Imagen;
            ImagenAmpliadaNombre = producto.ImagenNombre ?? producto.NombreProducto;
            MostrarModalImagen = true;
        }

        [RelayCommand]
        private void VerImagenAmpliadaGeneral(byte[]? imagen)
        {
            if (imagen == null || imagen.Length == 0) return;
            ImagenAmpliada = imagen;
            ImagenAmpliadaNombre = "Imagen";
            MostrarModalImagen = true;
        }

        [RelayCommand]
        private void CerrarModalImagen()
        {
            MostrarModalImagen = false;
            ImagenAmpliada = null;
            ImagenAmpliadaNombre = null;
        }

        [RelayCommand]
        private void AbrirPrevisualizacionPoster()
        {
            MostrarPrevisualizacionPoster = true;
        }

        [RelayCommand]
        private void CerrarPrevisualizacionPoster()
        {
            MostrarPrevisualizacionPoster = false;
        }

        // ============================================
        // COMANDOS DE IMAGENES DEL PACK
        // ============================================

        [RelayCommand]
        private async Task SeleccionarImagenPosterAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagen poster",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var bytes = ms.ToArray();
                _imagenPosterOriginal = bytes;
                ImagenPoster = bytes;
                NombreImagenPoster = file.Name;
                OnPropertyChanged(nameof(TieneImagenPosterParaEncuadre));
            }
        }

        [RelayCommand]
        private async Task AgregarImagenAdicionalAsync(Window? window)
        {
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar imagenes",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Imagenes") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif" } }
                }
            });

            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                var imagen = new PackAlimentoImagen
                {
                    ImagenContenido = ms.ToArray(),
                    ImagenNombre = file.Name,
                    ImagenTipo = Path.GetExtension(file.Name),
                    Orden = ImagenesActuales.Count
                };

                ImagenesActuales.Add(imagen);
            }
        }

        [RelayCommand]
        private void EliminarImagen(PackAlimentoImagen? imagen)
        {
            if (imagen == null) return;
            ImagenesActuales.Remove(imagen);
        }

        [RelayCommand]
        private void EliminarImagenPoster()
        {
            ImagenPoster = null;
            NombreImagenPoster = null;
            _imagenPosterOriginal = null;
        }

        // ============================================
        // COMANDOS DE ENCUADRE CIRCULAR DEL POSTER
        // ============================================

        [RelayCommand]
        private void AbrirEncuadrePoster()
        {
            if (!TieneImagenPosterParaEncuadre) return;

            var imagenParaCargar = _imagenPosterOriginal ?? ImagenPoster;
            if (imagenParaCargar == null) return;

            try
            {
                using var ms = new MemoryStream(imagenParaCargar);
                ImagenPosterParaEncuadre = new Bitmap(ms);
            }
            catch
            {
                return;
            }

            // Inicializar crop centrado como circulo
            var size = Math.Min(POSTER_CANVAS_WIDTH, POSTER_CANVAS_HEIGHT) * 0.6;
            CropPosterSize = size;
            CropPosterX = (POSTER_CANVAS_WIDTH - size) / 2;
            CropPosterY = (POSTER_CANVAS_HEIGHT - size) / 2;
            ZoomPosterLevel = 1.0;

            MostrarPanelEncuadrePoster = true;
        }

        [RelayCommand]
        private void CerrarEncuadrePoster()
        {
            MostrarPanelEncuadrePoster = false;
        }

        [RelayCommand]
        private void ConfirmarEncuadrePoster()
        {
            if (ImagenPosterParaEncuadre != null && (_imagenPosterOriginal != null || ImagenPoster != null))
            {
                var resultado = ProcesarCropCircular();
                if (resultado != null)
                {
                    // Guardar la original antes del primer recorte
                    if (_imagenPosterOriginal == null)
                    {
                        _imagenPosterOriginal = ImagenPoster;
                    }
                    ImagenPoster = resultado;
                    MostrarMensaje("Imagen recortada correctamente", false);
                }
            }
            MostrarPanelEncuadrePoster = false;
        }

        [RelayCommand]
        private void EncuadrarPosterAutomatico()
        {
            // Centrar el crop en el canvas
            CropPosterX = (POSTER_CANVAS_WIDTH - CropPosterSize) / 2;
            CropPosterY = (POSTER_CANVAS_HEIGHT - CropPosterSize) / 2;
            ZoomPosterLevel = 1.0;
            NotificarCambioCropPoster();
            NotificarCambioZoomPoster();
        }

        [RelayCommand]
        private void PosterPosicionArriba()
        {
            CropPosterX = (POSTER_CANVAS_WIDTH - CropPosterSize) / 2;
            CropPosterY = 0;
            NotificarCambioCropPoster();
        }

        [RelayCommand]
        private void PosterPosicionCentro()
        {
            CropPosterX = (POSTER_CANVAS_WIDTH - CropPosterSize) / 2;
            CropPosterY = (POSTER_CANVAS_HEIGHT - CropPosterSize) / 2;
            NotificarCambioCropPoster();
        }

        [RelayCommand]
        private void PosterPosicionAbajo()
        {
            CropPosterX = (POSTER_CANVAS_WIDTH - CropPosterSize) / 2;
            CropPosterY = POSTER_CANVAS_HEIGHT - CropPosterSize;
            NotificarCambioCropPoster();
        }

        [RelayCommand]
        private void PosterPosicionIzquierda()
        {
            CropPosterX = 0;
            NotificarCambioCropPoster();
        }

        [RelayCommand]
        private void PosterPosicionCentroHorizontal()
        {
            CropPosterX = (POSTER_CANVAS_WIDTH - CropPosterSize) / 2;
            NotificarCambioCropPoster();
        }

        [RelayCommand]
        private void PosterPosicionDerecha()
        {
            CropPosterX = POSTER_CANVAS_WIDTH - CropPosterSize;
            NotificarCambioCropPoster();
        }

        public void MoverCropPoster(double deltaX, double deltaY)
        {
            var newX = CropPosterX + deltaX;
            var newY = CropPosterY + deltaY;

            newX = Math.Max(-CropPosterSize + 50, Math.Min(newX, POSTER_CANVAS_WIDTH - 50));
            newY = Math.Max(-CropPosterSize + 50, Math.Min(newY, POSTER_CANVAS_HEIGHT - 50));

            CropPosterX = newX;
            CropPosterY = newY;
            NotificarCambioCropPoster();
        }

        public void RedimensionarCropPoster(string esquina, double deltaX, double deltaY)
        {
            double delta = (Math.Abs(deltaX) + Math.Abs(deltaY)) / 2;

            double signoX = esquina.Contains("Right") || esquina == "SE" || esquina == "NE" ? 1 : -1;
            double signoY = esquina.Contains("Bottom") || esquina == "SE" || esquina == "SW" ? 1 : -1;

            bool agrandar = (deltaX * signoX > 0) || (deltaY * signoY > 0);
            delta = agrandar ? delta : -delta;

            // El crop circular es siempre cuadrado (size x size)
            double nuevoSize = CropPosterSize + delta * 2;
            nuevoSize = Math.Max(MIN_CROP_POSTER_SIZE, Math.Min(nuevoSize, Math.Min(POSTER_CANVAS_WIDTH, POSTER_CANVAS_HEIGHT)));

            double deltaSize = nuevoSize - CropPosterSize;

            // Ajustar posicion para mantener centrado el redimensionamiento
            if (esquina == "TopLeft" || esquina == "NW")
            {
                CropPosterX -= deltaSize;
                CropPosterY -= deltaSize;
            }
            else if (esquina == "TopRight" || esquina == "NE")
            {
                CropPosterY -= deltaSize;
            }
            else if (esquina == "BottomLeft" || esquina == "SW")
            {
                CropPosterX -= deltaSize;
            }

            CropPosterSize = nuevoSize;
            NotificarCambioCropPoster();
            OnPropertyChanged(nameof(CropPosterSize));
        }

        public void AjustarZoomPoster(double delta)
        {
            var newZoom = ZoomPosterLevel + delta;
            newZoom = Math.Max(0.5, Math.Min(3.0, newZoom));
            if (Math.Abs(newZoom - ZoomPosterLevel) > 0.001)
            {
                ZoomPosterLevel = newZoom;
                NotificarCambioZoomPoster();
            }
        }

        private void NotificarCambioZoomPoster()
        {
            OnPropertyChanged(nameof(ZoomPosterLevel));
            OnPropertyChanged(nameof(ImagenPosterBackgroundLeftConZoom));
            OnPropertyChanged(nameof(ImagenPosterBackgroundTopConZoom));
            OnPropertyChanged(nameof(CropPosterImageOffsetXConZoom));
            OnPropertyChanged(nameof(CropPosterImageOffsetYConZoom));
        }

        private void NotificarCambioCropPoster()
        {
            OnPropertyChanged(nameof(CropPosterMargin));
            OnPropertyChanged(nameof(CropPosterImageOffsetXConZoom));
            OnPropertyChanged(nameof(CropPosterImageOffsetYConZoom));
        }

        private byte[]? ProcesarCropCircular()
        {
            try
            {
                var imagenParaProcesar = _imagenPosterOriginal ?? ImagenPoster;
                if (imagenParaProcesar == null) return null;

                using var ms = new MemoryStream(imagenParaProcesar);
                var imagenOriginal = new Bitmap(ms);

                double imgWidth = imagenOriginal.PixelSize.Width;
                double imgHeight = imagenOriginal.PixelSize.Height;

                // La imagen se escala con Stretch=Uniform para caber en el canvas
                double baseScaleX = POSTER_CANVAS_WIDTH / imgWidth;
                double baseScaleY = POSTER_CANVAS_HEIGHT / imgHeight;
                double baseScale = Math.Min(baseScaleX, baseScaleY);

                double scaledWidthBase = imgWidth * baseScale;
                double scaledHeightBase = imgHeight * baseScale;

                double offsetInContainerX = (POSTER_CANVAS_WIDTH - scaledWidthBase) / 2;
                double offsetInContainerY = (POSTER_CANVAS_HEIGHT - scaledHeightBase) / 2;

                double totalScale = baseScale * ZoomPosterLevel;

                double imgLeft = POSTER_CANVAS_WIDTH / 2 + (offsetInContainerX - POSTER_CANVAS_WIDTH / 2) * ZoomPosterLevel;
                double imgTop = POSTER_CANVAS_HEIGHT / 2 + (offsetInContainerY - POSTER_CANVAS_HEIGHT / 2) * ZoomPosterLevel;
                double scaledWidth = scaledWidthBase * ZoomPosterLevel;
                double scaledHeight = scaledHeightBase * ZoomPosterLevel;

                // Interseccion del crop (cuadrado) con la imagen
                double intersectLeft = Math.Max(CropPosterX, imgLeft);
                double intersectTop = Math.Max(CropPosterY, imgTop);
                double intersectRight = Math.Min(CropPosterX + CropPosterSize, imgLeft + scaledWidth);
                double intersectBottom = Math.Min(CropPosterY + CropPosterSize, imgTop + scaledHeight);

                // Exportar como cuadrado de 400x400
                int tamanoFinal = 400;

                using var renderTarget = new Avalonia.Media.Imaging.RenderTargetBitmap(
                    new Avalonia.PixelSize(tamanoFinal, tamanoFinal));

                using (var ctx = renderTarget.CreateDrawingContext())
                {
                    ctx.FillRectangle(Avalonia.Media.Brushes.White, new Avalonia.Rect(0, 0, tamanoFinal, tamanoFinal));

                    if (intersectRight > intersectLeft && intersectBottom > intersectTop)
                    {
                        double relX = intersectLeft - imgLeft;
                        double relY = intersectTop - imgTop;
                        double intersectWidth = intersectRight - intersectLeft;
                        double intersectHeight = intersectBottom - intersectTop;

                        int sourceX = (int)Math.Round(relX / totalScale);
                        int sourceY = (int)Math.Round(relY / totalScale);
                        int sourceWidth = (int)Math.Round(intersectWidth / totalScale);
                        int sourceHeight = (int)Math.Round(intersectHeight / totalScale);

                        sourceX = Math.Max(0, Math.Min(sourceX, (int)imgWidth - 1));
                        sourceY = Math.Max(0, Math.Min(sourceY, (int)imgHeight - 1));
                        sourceWidth = Math.Max(1, Math.Min(sourceWidth, (int)imgWidth - sourceX));
                        sourceHeight = Math.Max(1, Math.Min(sourceHeight, (int)imgHeight - sourceY));

                        double destOffsetX = (intersectLeft - CropPosterX) / CropPosterSize * tamanoFinal;
                        double destOffsetY = (intersectTop - CropPosterY) / CropPosterSize * tamanoFinal;
                        double destWidth = intersectWidth / CropPosterSize * tamanoFinal;
                        double destHeight = intersectHeight / CropPosterSize * tamanoFinal;

                        var sourceRect = new Avalonia.Rect(sourceX, sourceY, sourceWidth, sourceHeight);
                        var destRect = new Avalonia.Rect(destOffsetX, destOffsetY, destWidth, destHeight);
                        ctx.DrawImage(imagenOriginal, sourceRect, destRect);
                    }
                }

                using var outputStream = new MemoryStream();
                renderTarget.Save(outputStream);
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al procesar imagen: {ex.Message}", true);
                return null;
            }
        }

        // ============================================
        // METODOS PRIVADOS
        // ============================================

        private async Task CargarPacksAsync()
        {
            EstaCargando = true;

            try
            {
                Packs.Clear();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT pa.id_pack, pa.nombre_pack, pa.descripcion, pa.imagen_poster,
                           pa.imagen_poster_nombre, pa.activo, pa.fecha_creacion, pa.id_pais,
                           pd.nombre_pais, pd.bandera_imagen,
                           COALESCE(pap.precio, pap_global.precio, 0) as precio,
                           COALESCE(pap.divisa, pap_global.divisa, 'EUR') as divisa
                    FROM packs_alimentos pa
                    LEFT JOIN paises_designados pd ON pa.id_pais = pd.id_pais
                    LEFT JOIN pack_alimentos_asignacion_comercios paac
                        ON pa.id_pack = paac.id_pack AND paac.activo = true
                    LEFT JOIN pack_alimentos_precios pap
                        ON paac.id_precio = pap.id_precio
                    LEFT JOIN pack_alimentos_asignacion_global paag
                        ON pa.id_pack = paag.id_pack AND paag.activo = true
                    LEFT JOIN pack_alimentos_precios pap_global
                        ON paag.id_precio = pap_global.id_precio
                    ORDER BY pa.fecha_creacion DESC";

                await using var cmd = new NpgsqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var packs = new System.Collections.Generic.List<PackAlimento>();
                var packsIds = new System.Collections.Generic.HashSet<int>();

                while (await reader.ReadAsync())
                {
                    var idPack = reader.GetInt32(0);

                    // Evitar duplicados por JOINs multiples
                    if (packsIds.Contains(idPack)) continue;
                    packsIds.Add(idPack);

                    packs.Add(new PackAlimento
                    {
                        IdPack = idPack,
                        NombrePack = reader.GetString(1),
                        Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ImagenPoster = reader.IsDBNull(3) ? null : (byte[])reader["imagen_poster"],
                        ImagenPosterNombre = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Activo = reader.GetBoolean(5),
                        FechaCreacion = reader.GetDateTime(6),
                        IdPais = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                        NombrePais = reader.IsDBNull(8) ? null : reader.GetString(8),
                        BanderaPais = reader.IsDBNull(9) ? null : (byte[])reader["bandera_imagen"],
                        PrecioPack = reader.IsDBNull(10) ? 0 : reader.GetDecimal(10),
                        DivisaPack = reader.IsDBNull(11) ? "EUR" : reader.GetString(11)
                    });
                }

                await reader.CloseAsync();

                foreach (var pack in packs)
                {
                    var prodQuery = @"
                        SELECT id_producto, nombre_producto, descripcion, detalles, cantidad, unidad_medida, orden, imagen, imagen_nombre, imagen_tipo
                        FROM pack_alimentos_productos
                        WHERE id_pack = @idPack
                        ORDER BY orden";

                    await using var prodCmd = new NpgsqlCommand(prodQuery, conn);
                    prodCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                    await using var prodReader = await prodCmd.ExecuteReaderAsync();

                    while (await prodReader.ReadAsync())
                    {
                        pack.Productos.Add(new PackAlimentoProducto
                        {
                            IdProducto = prodReader.GetInt32(0),
                            IdPack = pack.IdPack,
                            NombreProducto = prodReader.GetString(1),
                            Descripcion = prodReader.IsDBNull(2) ? null : prodReader.GetString(2),
                            Detalles = prodReader.IsDBNull(3) ? null : prodReader.GetString(3),
                            Cantidad = prodReader.GetDecimal(4),
                            UnidadMedida = prodReader.GetString(5),
                            Orden = prodReader.GetInt32(6),
                            Imagen = prodReader.IsDBNull(7) ? null : (byte[])prodReader["imagen"],
                            ImagenNombre = prodReader.IsDBNull(8) ? null : prodReader.GetString(8),
                            ImagenTipo = prodReader.IsDBNull(9) ? null : prodReader.GetString(9)
                        });
                    }

                    await prodReader.CloseAsync();

                    var imgQuery = @"
                        SELECT id_imagen, imagen_contenido, imagen_nombre, imagen_tipo, descripcion, orden
                        FROM pack_alimentos_imagenes
                        WHERE id_pack = @idPack
                        ORDER BY orden";

                    await using var imgCmd = new NpgsqlCommand(imgQuery, conn);
                    imgCmd.Parameters.AddWithValue("@idPack", pack.IdPack);
                    await using var imgReader = await imgCmd.ExecuteReaderAsync();

                    while (await imgReader.ReadAsync())
                    {
                        pack.Imagenes.Add(new PackAlimentoImagen
                        {
                            IdImagen = imgReader.GetInt32(0),
                            IdPack = pack.IdPack,
                            ImagenContenido = (byte[])imgReader["imagen_contenido"],
                            ImagenNombre = imgReader.IsDBNull(2) ? null : imgReader.GetString(2),
                            ImagenTipo = imgReader.IsDBNull(3) ? null : imgReader.GetString(3),
                            Descripcion = imgReader.IsDBNull(4) ? null : imgReader.GetString(4),
                            Orden = imgReader.GetInt32(5)
                        });
                    }

                    Packs.Add(pack);
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar packs: {ex.Message}", true);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task CargarComerciosAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                ComerciosParaAsignar.Clear();

                var query = "SELECT id_comercio, nombre_comercio FROM comercios WHERE activo = true ORDER BY nombre_comercio";
                await using var cmd = new NpgsqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    ComerciosParaAsignar.Add(new ComercioConPrecio
                    {
                        IdComercio = reader.GetInt32(0),
                        NombreComercio = reader.GetString(1),
                        Seleccionado = false
                    });
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar comercios: {ex.Message}", true);
            }
        }

        private async Task CargarAsignacionesExistentesAsync(int idPack)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var globalQuery = @"
                    SELECT ag.id_asignacion, p.divisa, p.precio
                    FROM pack_alimentos_asignacion_global ag
                    INNER JOIN pack_alimentos_precios p ON ag.id_precio = p.id_precio
                    WHERE ag.id_pack = @idPack AND ag.activo = true
                    LIMIT 1";

                await using var globalCmd = new NpgsqlCommand(globalQuery, conn);
                globalCmd.Parameters.AddWithValue("@idPack", idPack);
                await using var globalReader = await globalCmd.ExecuteReaderAsync();

                if (await globalReader.ReadAsync())
                {
                    AsignarATodosLosComercios = true;
                    DivisaGeneral = globalReader.GetString(1);
                    PrecioGeneralTexto = globalReader.GetDecimal(2).ToString("F2");
                    await globalReader.CloseAsync();
                    return;
                }
                await globalReader.CloseAsync();

                AsignarATodosLosComercios = false;

                var comQuery = @"
                    SELECT ac.id_comercio, p.divisa, p.precio
                    FROM pack_alimentos_asignacion_comercios ac
                    INNER JOIN pack_alimentos_precios p ON ac.id_precio = p.id_precio
                    WHERE ac.id_pack = @idPack AND ac.activo = true";

                await using var comCmd = new NpgsqlCommand(comQuery, conn);
                comCmd.Parameters.AddWithValue("@idPack", idPack);
                await using var comReader = await comCmd.ExecuteReaderAsync();

                bool first = true;
                while (await comReader.ReadAsync())
                {
                    var idComercio = comReader.GetInt32(0);
                    var comercio = ComerciosParaAsignar.FirstOrDefault(c => c.IdComercio == idComercio);
                    if (comercio != null)
                    {
                        comercio.Seleccionado = true;
                    }

                    if (first)
                    {
                        DivisaGeneral = comReader.GetString(1);
                        PrecioGeneralTexto = comReader.GetDecimal(2).ToString("F2");
                        first = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje($"Error al cargar asignaciones: {ex.Message}", true);
            }
        }

        private void LimpiarFormulario()
        {
            _packIdEnEdicion = 0;
            NombrePack = string.Empty;
            DescripcionPack = string.Empty;
            ImagenPoster = null;
            NombreImagenPoster = null;
            _imagenPosterOriginal = null;
            ProductosActuales.Clear();
            ImagenesActuales.Clear();
            LimpiarFormularioProducto();
            PaisSeleccionado = null;

            CategoriasParaAsignar.Clear();
            AsignarATodosLosComercios = true;
            PrecioGeneralTexto = "0.00";
            DivisaGeneral = "EUR";
            foreach (var c in ComerciosParaAsignar)
            {
                c.Seleccionado = false;
            }
        }

        private void LimpiarFormularioProducto()
        {
            NuevoProductoNombre = string.Empty;
            NuevoProductoDescripcion = string.Empty;
            NuevoProductoDetalles = string.Empty;
            NuevoProductoCantidadTexto = "1";
            NuevoProductoUnidad = "unidad";
            NuevoProductoImagen = null;
            NuevoProductoImagenNombre = null;
            ProductoEnEdicion = null;
            EsModoEdicionProducto = false;
            OnPropertyChanged(nameof(TextoBotonProducto));
            OnPropertyChanged(nameof(TituloFormularioProducto));
        }

        private void MostrarMensaje(string mensaje, bool esError)
        {
            MensajeEstado = mensaje;
            MensajeEsError = esError;
            HayMensaje = true;

            Task.Delay(4000).ContinueWith(_ =>
            {
                HayMensaje = false;
            });
        }
    }

    public class ComercioConPrecio : ObservableObject
    {
        public int IdComercio { get; set; }
        public string NombreComercio { get; set; } = string.Empty;

        private bool _seleccionado;
        public bool Seleccionado
        {
            get => _seleccionado;
            set => SetProperty(ref _seleccionado, value);
        }
    }

    public class CategoriaSeleccionable : ObservableObject
    {
        public int IdCategoria { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public byte[]? ImagenCategoria { get; set; }
        public bool TieneImagen => ImagenCategoria != null && ImagenCategoria.Length > 0;

        private bool _seleccionada;
        public bool Seleccionada
        {
            get => _seleccionada;
            set => SetProperty(ref _seleccionada, value);
        }
    }
}