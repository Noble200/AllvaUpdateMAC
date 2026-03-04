using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Npgsql;
using BCrypt.Net;
using ClosedXML.Excel;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para la gestión de usuarios normales y floaters
/// VERSIÓN COMPLETA Y CORREGIDA
/// </summary>
public partial class ManageUsersViewModel : ObservableObject
{
    // ============================================
    // CONFIGURACIÓN DE BASE DE DATOS
    // ============================================
    
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // ============================================
    // PROPIEDADES OBSERVABLES - DATOS PRINCIPALES
    // ============================================

    [ObservableProperty]
    private ObservableCollection<UserModel> _usuarios = new();

    [ObservableProperty]
    private ObservableCollection<UserModel> _usuariosFiltrados = new();

    [ObservableProperty]
    private UserModel? _usuarioSeleccionado;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private bool _mostrarMensajeExito;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    [ObservableProperty]
    private string _mensajeExitoColor = "#28a745";

    // Propiedades para ventana de confirmación de eliminación
    [ObservableProperty]
    private bool _mostrarDialogoConfirmacion = false;

    // ============================================
    // PROPIEDADES PARA PANEL DERECHO
    // ============================================

    [ObservableProperty]
    private bool _mostrarPanelDerecho = false;

    [ObservableProperty]
    private string _tituloPanelDerecho = "Detalles del Usuario";

    [ObservableProperty]
    private bool _mostrarFormulario;

    [ObservableProperty]
    private bool _modoEdicion;

    [ObservableProperty]
    private string _botonGuardarTexto = "CREAR USUARIO";

    // ============================================
    // CAMPOS DEL FORMULARIO
    // ============================================

    [ObservableProperty]
    private string _formNombre = string.Empty;

    [ObservableProperty]
    private string _formSegundoNombre = string.Empty;

    [ObservableProperty]
    private string _formApellidos = string.Empty;

    [ObservableProperty]
    private string _formSegundoApellido = string.Empty;

    [ObservableProperty]
    private string _formNumeroUsuario = string.Empty;

    [ObservableProperty]
    private string _formCorreo = string.Empty;

    [ObservableProperty]
    private string _formTelefono = string.Empty;

    [ObservableProperty]
    private string _formPassword = string.Empty;

    [ObservableProperty]
    private bool _formActivo = true;

    [ObservableProperty]
    private string _tipoEmpleadoTexto = "REGULAR (1 local asignado)";

    // ============================================
    // PROPIEDADES PARA FILTROS
    // ============================================

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private string _filtroLocal = string.Empty;

    [ObservableProperty]
    private string _filtroComercio = string.Empty;

    [ObservableProperty]
    private string _filtroEstado = "Todos";

    [ObservableProperty]
    private string _filtroTipoUsuario = "Todos";

    // Filtros de fecha (entrada manual con formato dd/MM/yyyy)
    [ObservableProperty]
    private string _filtroUltimaOperacionDesde = string.Empty;

    [ObservableProperty]
    private string _filtroUltimaConexionDesde = string.Empty;

    // Autocompletado de comercios
    [ObservableProperty]
    private ObservableCollection<string> _sugerenciasComercio = new();

    [ObservableProperty]
    private bool _mostrarSugerenciasComercio;

    // ============================================
    // BÚSQUEDA Y ASIGNACIÓN DE LOCALES
    // ============================================

    [ObservableProperty]
    private string _busquedaComercio = string.Empty;

    [ObservableProperty]
    private bool _mostrarResultadosBusqueda;

    [ObservableProperty]
    private ObservableCollection<LocalFormModel> _resultadosBusquedaLocales = new();

    [ObservableProperty]
    private ObservableCollection<LocalFormModel> _localesAsignados = new();

    // Contador de locales seleccionados en la búsqueda
    public string ContadorSeleccionados =>
        $"{ResultadosBusquedaLocales.Count(l => l.EstaSeleccionado)} de {ResultadosBusquedaLocales.Count} seleccionados";

    // ============================================
    // ESTADÍSTICAS
    // ============================================

    public int TotalUsuarios => Usuarios.Count;
    public int UsuariosActivos => Usuarios.Count(u => u.Activo);
    public int UsuariosInactivos => Usuarios.Count(u => !u.Activo);

    // Usuario en edición
    private UserModel? _usuarioEnEdicion;
    
    // Diccionario para mapear usuarios con sus locales (para filtros)
    private Dictionary<int, List<string>> _usuariosConLocales = new();
    
    // Contraseña guardada del usuario (para comparar si cambió)
    private string _passwordGuardada = string.Empty;

    // Lista de comercios para autocompletado
    private List<string> _todosLosComercios = new();

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public ManageUsersViewModel()
    {
        _ = CargarDatosDesdeBaseDatos();
    }

    // ============================================
    // OBSERVADORES DE CAMBIOS - AUTO-GENERACIÓN
    // ============================================

    partial void OnFormNombreChanged(string value)
    {
        ActualizarNumeroUsuarioAutomaticamente();
    }

    partial void OnFormApellidosChanged(string value)
    {
        ActualizarNumeroUsuarioAutomaticamente();
    }

    partial void OnLocalesAsignadosChanged(ObservableCollection<LocalFormModel> value)
    {
        ActualizarTipoEmpleado();
    }

    private void ActualizarNumeroUsuarioAutomaticamente()
    {
        if (ModoEdicion)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(FormNombre) || string.IsNullOrWhiteSpace(FormApellidos))
        {
            FormNumeroUsuario = string.Empty;
            return;
        }

        var nombre = FormNombre.Trim().ToUpper().Replace(" ", "");
        var apellido = FormApellidos.Trim().ToUpper().Replace(" ", "");
        var baseNumero = $"{nombre}_{apellido}";

        var numero = baseNumero;
        var contador = 2;

        while (Usuarios.Any(u => u.NumeroUsuario.Equals(numero, StringComparison.OrdinalIgnoreCase)))
        {
            numero = $"{baseNumero}_{contador:D2}";
            contador++;
        }

        FormNumeroUsuario = numero;
    }

    private void ActualizarTipoEmpleado()
    {
        var cantidadLocales = LocalesAsignados.Count;
        
        if (cantidadLocales == 0)
        {
            TipoEmpleadoTexto = "Sin locales asignados";
        }
        else if (cantidadLocales == 1)
        {
            TipoEmpleadoTexto = "REGULAR (1 local asignado)";
        }
        else
        {
            TipoEmpleadoTexto = $"FLOATER ({cantidadLocales} locales asignados)";
        }
    }

    // ============================================
    // MÉTODOS DE BASE DE DATOS - CARGAR
    // ============================================

    private async Task CargarDatosDesdeBaseDatos()
    {
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var usuarios = await CargarUsuarios(connection);

            Usuarios.Clear();
            foreach (var usuario in usuarios)
            {
                Usuarios.Add(usuario);
            }
            
            await CargarMapeoUsuariosLocales(connection);
            await CargarDatosFloaters(connection);
            await CargarListaComercios(connection);

            OnPropertyChanged(nameof(TotalUsuarios));
            OnPropertyChanged(nameof(UsuariosActivos));
            OnPropertyChanged(nameof(UsuariosInactivos));

            await InicializarFiltros();
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task<List<UserModel>> CargarUsuarios(NpgsqlConnection connection)
    {
        var usuarios = new List<UserModel>();

        var query = @"SELECT u.id_usuario, u.numero_usuario, u.nombre,
                             COALESCE(u.segundo_nombre, '') as segundo_nombre,
                             u.apellidos, COALESCE(u.segundo_apellido, '') as segundo_apellido,
                             u.correo, COALESCE(u.telefono, '') as telefono,
                             COALESCE(u.es_flooter, false) as es_flotante,
                             u.activo, u.ultimo_acceso,
                             COALESCE(l.id_local, 0) as id_local,
                             COALESCE(l.nombre_local, 'Sin asignar') as nombre_local,
                             COALESCE(l.codigo_local, 'N/A') as codigo_local,
                             COALESCE(c.id_comercio, 0) as id_comercio,
                             COALESCE(c.nombre_comercio, 'Sin asignar') as nombre_comercio,
                             (SELECT MAX(op.fecha_operacion + op.hora_operacion) FROM operaciones op WHERE op.id_usuario = u.id_usuario) as ultima_operacion
                      FROM usuarios u
                      LEFT JOIN locales l ON u.id_local = l.id_local
                      LEFT JOIN comercios c ON l.id_comercio = c.id_comercio
                      WHERE u.id_rol = 2
                      ORDER BY u.nombre, u.apellidos";

        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            usuarios.Add(new UserModel
            {
                IdUsuario = reader.GetInt32(0),
                NumeroUsuario = reader.GetString(1),
                Nombre = reader.GetString(2),
                SegundoNombre = reader.GetString(3),
                Apellidos = reader.GetString(4),
                SegundoApellido = reader.GetString(5),
                Correo = reader.GetString(6),
                Telefono = reader.GetString(7),
                EsFlotante = reader.GetBoolean(8),
                Activo = reader.GetBoolean(9),
                UltimoAcceso = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                IdLocal = reader.GetInt32(11),
                NombreLocal = reader.GetString(12),
                CodigoLocal = reader.GetString(13),
                IdComercio = reader.GetInt32(14),
                NombreComercio = reader.GetString(15),
                UltimaOperacion = reader.IsDBNull(16) ? null : reader.GetDateTime(16)
            });
        }

        return usuarios;
    }
    
    /// <summary>
    /// Carga todos los locales donde trabaja cada usuario (incluyendo floaters)
    /// </summary>
    private async Task CargarMapeoUsuariosLocales(NpgsqlConnection connection)
    {
        _usuariosConLocales.Clear();
        
        var query = @"SELECT ul.id_usuario, l.codigo_local
                      FROM usuario_locales ul
                      INNER JOIN locales l ON ul.id_local = l.id_local
                      ORDER BY ul.id_usuario";
        
        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var idUsuario = reader.GetInt32(0);
            var codigoLocal = reader.GetString(1);
            
            if (!_usuariosConLocales.ContainsKey(idUsuario))
            {
                _usuariosConLocales[idUsuario] = new List<string>();
            }
            
            _usuariosConLocales[idUsuario].Add(codigoLocal);
        }
    }

    /// <summary>
    /// Carga los códigos de locales y nombre del comercio para usuarios floaters
    /// </summary>
    private async Task CargarDatosFloaters(NpgsqlConnection connection)
    {
        foreach (var usuario in Usuarios.Where(u => u.EsFlotante))
        {
            if (_usuariosConLocales.ContainsKey(usuario.IdUsuario))
            {
                var codigosLocales = _usuariosConLocales[usuario.IdUsuario];
                
                // Formatear códigos de locales (máximo 3 visibles)
                if (codigosLocales.Count <= 3)
                {
                    usuario.CodigosLocalesFlooter = string.Join(", ", codigosLocales);
                }
                else
                {
                    usuario.CodigosLocalesFlooter = string.Join(", ", codigosLocales.Take(3)) + $" +{codigosLocales.Count - 3} más";
                }
                
                // Obtener el nombre del comercio
                var query = @"SELECT DISTINCT c.nombre_comercio
                              FROM usuario_locales ul
                              INNER JOIN locales l ON ul.id_local = l.id_local
                              INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                              WHERE ul.id_usuario = @IdUsuario
                              LIMIT 1";
                
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    usuario.NombreComercioFlooter = result.ToString() ?? "Sin comercio";
                }
                else
                {
                    usuario.NombreComercioFlooter = "Sin comercio";
                }
            }
            else
            {
                usuario.CodigosLocalesFlooter = "N/A";
                usuario.NombreComercioFlooter = "Sin comercio";
            }
        }
    }

    /// <summary>
    /// Carga la lista de comercios para autocompletado
    /// </summary>
    private async Task CargarListaComercios(NpgsqlConnection connection)
    {
        _todosLosComercios.Clear();

        var query = "SELECT DISTINCT nombre_comercio FROM comercios ORDER BY nombre_comercio";

        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            _todosLosComercios.Add(reader.GetString(0));
        }
    }

    // Variables para evitar recursión en formateo de fechas
    private bool _formateandoFechaOperacion = false;
    private bool _formateandoFechaConexion = false;

    /// <summary>
    /// Formatea automáticamente la fecha de última operación (dd/MM/yyyy)
    /// </summary>
    partial void OnFiltroUltimaOperacionDesdeChanged(string value)
    {
        if (_formateandoFechaOperacion) return;

        var formateado = FormatearFechaAutomaticamente(value);
        if (formateado != value)
        {
            _formateandoFechaOperacion = true;
            FiltroUltimaOperacionDesde = formateado;
            _formateandoFechaOperacion = false;
        }
    }

    /// <summary>
    /// Formatea automáticamente la fecha de última conexión (dd/MM/yyyy)
    /// </summary>
    partial void OnFiltroUltimaConexionDesdeChanged(string value)
    {
        if (_formateandoFechaConexion) return;

        var formateado = FormatearFechaAutomaticamente(value);
        if (formateado != value)
        {
            _formateandoFechaConexion = true;
            FiltroUltimaConexionDesde = formateado;
            _formateandoFechaConexion = false;
        }
    }

    /// <summary>
    /// Formatea una cadena de fecha agregando / automáticamente (dd/MM/yyyy)
    /// </summary>
    private string FormatearFechaAutomaticamente(string valor)
    {
        if (string.IsNullOrEmpty(valor))
            return valor;

        // Solo permitir dígitos y /
        var soloDigitos = new string(valor.Where(c => char.IsDigit(c)).ToArray());

        // Limitar a 8 dígitos máximo (ddMMyyyy)
        if (soloDigitos.Length > 8)
            soloDigitos = soloDigitos.Substring(0, 8);

        // Construir la fecha con /
        var resultado = string.Empty;
        for (int i = 0; i < soloDigitos.Length; i++)
        {
            resultado += soloDigitos[i];
            // Agregar / después de la posición 2 (día) y 4 (mes)
            if ((i == 1 || i == 3) && i < soloDigitos.Length - 1)
            {
                resultado += "/";
            }
        }

        return resultado;
    }

    /// <summary>
    /// Intenta parsear una fecha en formato dd/MM/yyyy
    /// </summary>
    private bool TryParseFecha(string valor, out DateTime fecha)
    {
        fecha = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        return DateTime.TryParseExact(valor, "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out fecha);
    }

    /// <summary>
    /// Actualiza las sugerencias de comercio según el texto ingresado
    /// </summary>
    partial void OnFiltroComercioChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            SugerenciasComercio.Clear();
            MostrarSugerenciasComercio = false;
            return;
        }

        var sugerencias = _todosLosComercios
            .Where(c => c.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        SugerenciasComercio.Clear();
        foreach (var s in sugerencias)
        {
            SugerenciasComercio.Add(s);
        }

        MostrarSugerenciasComercio = SugerenciasComercio.Count > 0;
    }

    [RelayCommand]
    private void SeleccionarComercioSugerido(string comercio)
    {
        FiltroComercio = comercio;
        MostrarSugerenciasComercio = false;
    }

    // ============================================
    // COMANDOS - PANEL DERECHO
    // ============================================

    [RelayCommand]
    private void MostrarFormularioCrear()
    {
        LimpiarFormulario();
        ModoEdicion = false;
        TituloPanelDerecho = "Crear Nuevo Usuario";
        BotonGuardarTexto = "CREAR USUARIO";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    [RelayCommand]
    private async Task EditarUsuario(UserModel usuario)
    {
        _usuarioEnEdicion = usuario;
        UsuarioSeleccionado = usuario;

        FormNombre = usuario.Nombre;
        FormSegundoNombre = usuario.SegundoNombre ?? string.Empty;
        FormApellidos = usuario.Apellidos;
        FormSegundoApellido = usuario.SegundoApellido ?? string.Empty;
        FormNumeroUsuario = usuario.NumeroUsuario;
        FormCorreo = usuario.Correo;
        FormTelefono = usuario.Telefono == "Sin teléfono" ? string.Empty : usuario.Telefono ?? string.Empty;
        FormActivo = usuario.Activo;
        
        await CargarPasswordGuardada(usuario.IdUsuario);
        FormPassword = string.Empty;

        await CargarLocalesAsignadosUsuario(usuario.IdUsuario);

        ModoEdicion = true;
        TituloPanelDerecho = $"Editar: {usuario.NombreCompleto}";
        BotonGuardarTexto = "ACTUALIZAR USUARIO";
        MostrarFormulario = true;
        MostrarPanelDerecho = true;
    }

    /// <summary>
    /// Carga la contraseña guardada del usuario para mostrarla al editar
    /// </summary>
    private async Task CargarPasswordGuardada(int idUsuario)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = "SELECT password_hash FROM usuarios WHERE id_usuario = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idUsuario);

            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                _passwordGuardada = result.ToString() ?? string.Empty;
            }
            else
            {
                _passwordGuardada = string.Empty;
            }
        }
        catch
        {
            _passwordGuardada = string.Empty;
        }
    }

    [RelayCommand]
    private async Task VerDetallesUsuario(UserModel usuario)
    {
        UsuarioSeleccionado = usuario;
        TituloPanelDerecho = $"Detalles: {usuario.NombreCompleto}";
        MostrarFormulario = false;
        MostrarPanelDerecho = true;

        await CargarLocalesAsignadosUsuario(usuario.IdUsuario);
    }

    [RelayCommand]
    private void CerrarPanelDerecho()
    {
        MostrarPanelDerecho = false;
        MostrarFormulario = false;
        UsuarioSeleccionado = null;
        LimpiarFormulario();
    }

    // ============================================
    // COMANDOS - GENERAR CONTRASEÑA
    // ============================================

    [RelayCommand]
    private void GenerarPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        FormPassword = password;
        MostrarMensajeExitoNotificacion("✓ Contraseña generada");
    }

    // ============================================
    // COMANDOS - ACCIONES CRUD
    // ============================================

    [RelayCommand]
    private async Task GuardarUsuario()
    {
        if (!ValidarFormulario(out string mensajeError))
        {
            MostrarMensajeError(mensajeError);
            return;
        }

        Cargando = true;

        try
        {
            if (ModoEdicion && _usuarioEnEdicion != null)
            {
                await ActualizarUsuario();
                MostrarMensajeExitoNotificacion("✓ Usuario actualizado correctamente");
            }
            else
            {
                await CrearNuevoUsuario();
                MostrarMensajeExitoNotificacion("✓ Usuario creado correctamente");
            }

            await CargarDatosDesdeBaseDatos();
            CerrarPanelDerecho();
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al guardar: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task CrearNuevoUsuario()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);

            bool esFlotante = LocalesAsignados.Count > 1;

            int? idLocalPrincipal = esFlotante ? null : LocalesAsignados.FirstOrDefault()?.IdLocal;
            int? idComercio = null;

            var primerLocal = LocalesAsignados.FirstOrDefault();
            if (primerLocal != null)
            {
                var queryComercio = "SELECT id_comercio FROM locales WHERE id_local = @IdLocal";
                using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
                cmdComercio.Parameters.AddWithValue("@IdLocal", primerLocal.IdLocal);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    idComercio = Convert.ToInt32(result);
                }
            }

            var queryUsuario = @"
                INSERT INTO usuarios (
                    id_comercio, id_local, id_rol, nombre, segundo_nombre, apellidos, segundo_apellido,
                    correo, telefono, numero_usuario, password_hash, es_flooter, idioma, activo, primer_login,
                    fecha_creacion, fecha_modificacion
                )
                VALUES (
                    @IdComercio, @IdLocal, 2, @Nombre, @SegundoNombre, @Apellidos, @SegundoApellido,
                    @Correo, @Telefono, @NumeroUsuario, @PasswordHash, @EsFlotante, 'es', @Activo, true,
                    @FechaCreacion, @FechaModificacion
                )
                RETURNING id_usuario";

            using var cmdUsuario = new NpgsqlCommand(queryUsuario, connection, transaction);
            cmdUsuario.Parameters.AddWithValue("@IdComercio", idComercio.HasValue ? idComercio.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@IdLocal", idLocalPrincipal.HasValue ? idLocalPrincipal.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@Nombre", FormNombre.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@SegundoNombre",
                string.IsNullOrWhiteSpace(FormSegundoNombre) ? DBNull.Value : FormSegundoNombre.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Apellidos", FormApellidos.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@SegundoApellido",
                string.IsNullOrWhiteSpace(FormSegundoApellido) ? DBNull.Value : FormSegundoApellido.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Correo", FormCorreo.Trim());
            cmdUsuario.Parameters.AddWithValue("@Telefono",
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono.Trim());
            cmdUsuario.Parameters.AddWithValue("@NumeroUsuario", FormNumeroUsuario);
            cmdUsuario.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmdUsuario.Parameters.AddWithValue("@EsFlotante", esFlotante);
            cmdUsuario.Parameters.AddWithValue("@Activo", FormActivo);
            cmdUsuario.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
            cmdUsuario.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);

            var idUsuario = Convert.ToInt32(await cmdUsuario.ExecuteScalarAsync());

            foreach (var local in LocalesAsignados)
            {
                var queryAsignacion = @"
                    INSERT INTO usuario_locales (id_usuario, id_local, es_principal)
                    VALUES (@IdUsuario, @IdLocal, @EsPrincipal)
                    ON CONFLICT (id_usuario, id_local) DO NOTHING";

                using var cmdAsignacion = new NpgsqlCommand(queryAsignacion, connection, transaction);
                cmdAsignacion.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmdAsignacion.Parameters.AddWithValue("@IdLocal", local.IdLocal);
                cmdAsignacion.Parameters.AddWithValue("@EsPrincipal", local == LocalesAsignados.First());

                await cmdAsignacion.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ActualizarUsuario()
    {
        if (_usuarioEnEdicion == null) return;

        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            bool esFlotante = LocalesAsignados.Count > 1;
            
            int? idLocalPrincipal = esFlotante ? null : LocalesAsignados.FirstOrDefault()?.IdLocal;
            int? idComercio = null;

            var primerLocal = LocalesAsignados.FirstOrDefault();
            if (primerLocal != null)
            {
                var queryComercio = "SELECT id_comercio FROM locales WHERE id_local = @IdLocal";
                using var cmdComercio = new NpgsqlCommand(queryComercio, connection, transaction);
                cmdComercio.Parameters.AddWithValue("@IdLocal", primerLocal.IdLocal);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    idComercio = Convert.ToInt32(result);
                }
            }

            var queryUsuario = @"
                UPDATE usuarios SET
                    id_comercio = @IdComercio,
                    id_local = @IdLocal,
                    numero_usuario = @NumeroUsuario,
                    nombre = @Nombre,
                    segundo_nombre = @SegundoNombre,
                    apellidos = @Apellidos,
                    segundo_apellido = @SegundoApellido,
                    correo = @Correo,
                    telefono = @Telefono,
                    es_flooter = @EsFlotante,
                    activo = @Activo,
                    fecha_modificacion = @FechaModificacion" +
                    (string.IsNullOrWhiteSpace(FormPassword) || FormPassword == _passwordGuardada ? "" : ", password_hash = @PasswordHash") + @"
                WHERE id_usuario = @IdUsuario";

            using var cmdUsuario = new NpgsqlCommand(queryUsuario, connection, transaction);
            cmdUsuario.Parameters.AddWithValue("@IdComercio", idComercio.HasValue ? idComercio.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@IdLocal", idLocalPrincipal.HasValue ? idLocalPrincipal.Value : DBNull.Value);
            cmdUsuario.Parameters.AddWithValue("@NumeroUsuario", FormNumeroUsuario);
            cmdUsuario.Parameters.AddWithValue("@Nombre", FormNombre.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@SegundoNombre",
                string.IsNullOrWhiteSpace(FormSegundoNombre) ? DBNull.Value : FormSegundoNombre.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Apellidos", FormApellidos.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@SegundoApellido",
                string.IsNullOrWhiteSpace(FormSegundoApellido) ? DBNull.Value : FormSegundoApellido.Trim().ToUpper());
            cmdUsuario.Parameters.AddWithValue("@Correo", FormCorreo.Trim());
            cmdUsuario.Parameters.AddWithValue("@Telefono",
                string.IsNullOrWhiteSpace(FormTelefono) ? DBNull.Value : FormTelefono.Trim());
            cmdUsuario.Parameters.AddWithValue("@EsFlotante", esFlotante);
            cmdUsuario.Parameters.AddWithValue("@Activo", FormActivo);
            cmdUsuario.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
            cmdUsuario.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);

            if (!string.IsNullOrWhiteSpace(FormPassword) && FormPassword != _passwordGuardada)
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(FormPassword);
                cmdUsuario.Parameters.AddWithValue("@PasswordHash", passwordHash);
            }

            await cmdUsuario.ExecuteNonQueryAsync();

            var queryDeleteAsignaciones = "DELETE FROM usuario_locales WHERE id_usuario = @IdUsuario";
            using var cmdDelete = new NpgsqlCommand(queryDeleteAsignaciones, connection, transaction);
            cmdDelete.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);
            await cmdDelete.ExecuteNonQueryAsync();

            foreach (var local in LocalesAsignados)
            {
                var queryAsignacion = @"
                    INSERT INTO usuario_locales (id_usuario, id_local, es_principal)
                    VALUES (@IdUsuario, @IdLocal, @EsPrincipal)
                    ON CONFLICT (id_usuario, id_local) DO NOTHING";

                using var cmdAsignacion = new NpgsqlCommand(queryAsignacion, connection, transaction);
                cmdAsignacion.Parameters.AddWithValue("@IdUsuario", _usuarioEnEdicion.IdUsuario);
                cmdAsignacion.Parameters.AddWithValue("@IdLocal", local.IdLocal);
                cmdAsignacion.Parameters.AddWithValue("@EsPrincipal", local == LocalesAsignados.First());

                await cmdAsignacion.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [RelayCommand]
    private void EliminarUsuario(UserModel usuario)
    {
        UsuarioSeleccionado = usuario;
        MostrarDialogoConfirmacion = true;
    }

    [RelayCommand]
    private void CancelarEliminarUsuario()
    {
        MostrarDialogoConfirmacion = false;
    }

    [RelayCommand]
    private async Task ConfirmarEliminarUsuario()
    {
        if (UsuarioSeleccionado == null) return;

        MostrarDialogoConfirmacion = false;
        Cargando = true;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var queryDeleteAsignaciones = "DELETE FROM usuario_locales WHERE id_usuario = @Id";
                using var cmdAsignaciones = new NpgsqlCommand(queryDeleteAsignaciones, connection, transaction);
                cmdAsignaciones.Parameters.AddWithValue("@Id", UsuarioSeleccionado.IdUsuario);
                await cmdAsignaciones.ExecuteNonQueryAsync();
            }
            catch
            {
            }

            var query = "DELETE FROM usuarios WHERE id_usuario = @Id";
            using var cmd = new NpgsqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@Id", UsuarioSeleccionado.IdUsuario);
            await cmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            await CargarDatosDesdeBaseDatos();

            MostrarMensajeExitoNotificacion($"✓ Usuario {UsuarioSeleccionado.NombreCompleto} eliminado correctamente");
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al eliminar: {ex.Message}");
        }
        finally
        {
            UsuarioSeleccionado = null;
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task CambiarEstadoUsuario(UserModel usuario)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var nuevoEstado = !usuario.Activo;
            var query = @"UPDATE usuarios 
                         SET activo = @Activo, fecha_modificacion = @Fecha
                         WHERE id_usuario = @Id";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Activo", nuevoEstado);
            cmd.Parameters.AddWithValue("@Fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("@Id", usuario.IdUsuario);

            await cmd.ExecuteNonQueryAsync();

            usuario.Activo = nuevoEstado;

            OnPropertyChanged(nameof(UsuariosActivos));
            OnPropertyChanged(nameof(UsuariosInactivos));

            MostrarMensajeExitoNotificacion($"✓ Estado: {(nuevoEstado ? "Activo" : "Inactivo")}");
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al cambiar estado: {ex.Message}");
        }
    }

    // ============================================
    // COMANDOS - FILTROS
    // ============================================

    [RelayCommand]
    private void AplicarFiltros()
    {
        var filtrados = Usuarios.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            filtrados = filtrados.Where(u =>
                u.NombreCompleto.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                u.NumeroUsuario.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase) ||
                u.Correo.Contains(FiltroBusqueda, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(FiltroLocal))
        {
            filtrados = filtrados.Where(u =>
            {
                if (u.CodigoLocal.Contains(FiltroLocal, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                if (u.EsFlotante && _usuariosConLocales.ContainsKey(u.IdUsuario))
                {
                    return _usuariosConLocales[u.IdUsuario].Any(codigo => codigo.Contains(FiltroLocal, StringComparison.OrdinalIgnoreCase));
                }
                
                return false;
            });
        }

        if (!string.IsNullOrEmpty(FiltroComercio))
        {
            filtrados = filtrados.Where(u =>
            {
                // Para usuarios normales, buscar en NombreComercio
                if (!string.IsNullOrEmpty(u.NombreComercio) && 
                    u.NombreComercio.Contains(FiltroComercio, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // Para floaters, buscar en NombreComercioFlooter
                if (u.EsFlotante && !string.IsNullOrEmpty(u.NombreComercioFlooter))
                {
                    return u.NombreComercioFlooter.Contains(FiltroComercio, StringComparison.OrdinalIgnoreCase);
                }
                
                return false;
            });
        }

        if (!string.IsNullOrEmpty(FiltroEstado) && FiltroEstado != "Todos")
        {
            var activo = FiltroEstado == "Activo";
            filtrados = filtrados.Where(u => u.Activo == activo);
        }

        if (!string.IsNullOrEmpty(FiltroTipoUsuario) && FiltroTipoUsuario != "Todos")
        {
            var esFlotante = FiltroTipoUsuario == "Floater";
            filtrados = filtrados.Where(u => u.EsFlotante == esFlotante);
        }

        // Filtro por última operación desde
        if (!string.IsNullOrWhiteSpace(FiltroUltimaOperacionDesde) && TryParseFecha(FiltroUltimaOperacionDesde, out var fechaOperacion))
        {
            filtrados = filtrados.Where(u =>
                u.UltimaOperacion.HasValue &&
                u.UltimaOperacion.Value.Date >= fechaOperacion.Date);
        }

        // Filtro por última conexión desde
        if (!string.IsNullOrWhiteSpace(FiltroUltimaConexionDesde) && TryParseFecha(FiltroUltimaConexionDesde, out var fechaConexion))
        {
            filtrados = filtrados.Where(u =>
                u.UltimoAcceso.HasValue &&
                u.UltimoAcceso.Value.Date >= fechaConexion.Date);
        }

        UsuariosFiltrados.Clear();
        foreach (var usuario in filtrados.OrderBy(u => u.NombreCompleto))
        {
            UsuariosFiltrados.Add(usuario);
        }
    }
    
    [RelayCommand]
    private void LimpiarFiltros()
    {
        FiltroBusqueda = string.Empty;
        FiltroLocal = string.Empty;
        FiltroComercio = string.Empty;
        FiltroEstado = "Todos";
        FiltroTipoUsuario = "Todos";
        FiltroUltimaOperacionDesde = string.Empty;
        FiltroUltimaConexionDesde = string.Empty;
        MostrarSugerenciasComercio = false;
        
        UsuariosFiltrados.Clear();
        foreach (var usuario in Usuarios.OrderBy(u => u.NombreCompleto))
        {
            UsuariosFiltrados.Add(usuario);
        }
        
        MostrarMensajeExitoNotificacion("✓ Filtros limpiados");
    }

    // ============================================
    // EXPORTAR A EXCEL
    // ============================================

    [RelayCommand]
    private async Task ExportarUsuariosExcel()
    {
        try
        {
            // Usar usuarios filtrados si hay filtros aplicados, sino todos
            var usuariosAExportar = UsuariosFiltrados.Count > 0 && UsuariosFiltrados.Count != Usuarios.Count
                ? UsuariosFiltrados.ToList()
                : Usuarios.ToList();

            if (!usuariosAExportar.Any())
            {
                MostrarMensajeError("No hay usuarios para exportar");
                return;
            }

            Cargando = true;

            // Crear directorio de exportaciones si no existe
            var exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Allva", "Exportaciones");
            Directory.CreateDirectory(exportPath);

            var fileName = $"Usuarios_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var filePath = Path.Combine(exportPath, fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Usuarios");

            // Encabezados
            var headers = new[] {
                "Nº Usuario", "Nombre", "Segundo Nombre", "Apellidos", "Segundo Apellido",
                "Correo", "Móvil", "Comercio", "Código Local", "Tipo",
                "Estado", "Última Operación", "Última Conexión"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0b5394");
                worksheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Datos
            int row = 2;
            foreach (var usuario in usuariosAExportar)
            {
                worksheet.Cell(row, 1).Value = usuario.NumeroUsuario;
                worksheet.Cell(row, 2).Value = usuario.Nombre;
                worksheet.Cell(row, 3).Value = usuario.SegundoNombre ?? "";
                worksheet.Cell(row, 4).Value = usuario.Apellidos;
                worksheet.Cell(row, 5).Value = usuario.SegundoApellido ?? "";
                worksheet.Cell(row, 6).Value = usuario.Correo;
                worksheet.Cell(row, 7).Value = usuario.Telefono ?? "";
                worksheet.Cell(row, 8).Value = usuario.NombreComercioDisplay;
                worksheet.Cell(row, 9).Value = usuario.CodigoLocalDisplay;
                worksheet.Cell(row, 10).Value = usuario.TipoUsuarioDisplay;
                worksheet.Cell(row, 11).Value = usuario.EstadoTexto;
                worksheet.Cell(row, 12).Value = usuario.UltimaOperacionTexto;
                worksheet.Cell(row, 13).Value = usuario.UltimoAccesoTexto;

                // Colorear estado
                if (usuario.Activo)
                {
                    worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.FromHtml("#28a745");
                }
                else
                {
                    worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.FromHtml("#dc3545");
                }

                row++;
            }

            // Ajustar ancho de columnas
            worksheet.Columns().AdjustToContents();

            // Añadir filtros automáticos
            worksheet.RangeUsed()?.SetAutoFilter();

            await Task.Run(() => workbook.SaveAs(filePath));

            // Abrir el archivo
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);

            MostrarMensajeExitoNotificacion($"✓ Exportados {usuariosAExportar.Count} usuarios a Excel");
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al exportar: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    // ============================================
    // BÚSQUEDA Y ASIGNACIÓN DE LOCALES
    // ============================================

    [RelayCommand]
    private async Task BuscarLocalesPorComercio()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BusquedaComercio))
            {
                MostrarResultadosBusqueda = false;
                return;
            }

            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"SELECT l.id_local, 
                                l.codigo_local, 
                                COALESCE(l.nombre_local, '') as nombre_local, 
                                COALESCE(l.tipo_via, '') as tipo_via, 
                                COALESCE(l.direccion, '') as direccion, 
                                COALESCE(l.local_numero, '') as local_numero, 
                                COALESCE(l.escalera, '') as escalera, 
                                COALESCE(l.piso, '') as piso,
                                COALESCE(l.codigo_postal, '') as codigo_postal, 
                                COALESCE(l.pais, '') as pais, 
                                COALESCE(l.telefono, '') as telefono, 
                                COALESCE(l.email, '') as email,
                                c.id_comercio, 
                                c.nombre_comercio
                        FROM locales l
                        INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                        WHERE LOWER(c.nombre_comercio) LIKE LOWER(@Busqueda)
                        ORDER BY l.codigo_local";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Busqueda", $"%{BusquedaComercio}%");

            ResultadosBusquedaLocales.Clear();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ResultadosBusquedaLocales.Add(new LocalFormModel
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    NombreLocal = reader.GetString(2),
                    TipoVia = reader.GetString(3),
                    Direccion = reader.GetString(4),
                    LocalNumero = reader.GetString(5),
                    Escalera = string.IsNullOrEmpty(reader.GetString(6)) ? null : reader.GetString(6),
                    Piso = string.IsNullOrEmpty(reader.GetString(7)) ? null : reader.GetString(7),
                    CodigoPostal = reader.GetString(8),
                    Pais = reader.GetString(9),
                    Telefono = string.IsNullOrEmpty(reader.GetString(10)) ? null : reader.GetString(10),
                    Email = string.IsNullOrEmpty(reader.GetString(11)) ? null : reader.GetString(11),
                    IdComercio = reader.GetInt32(12)
                });
            }

            MostrarResultadosBusqueda = ResultadosBusquedaLocales.Count > 0;
            OnPropertyChanged(nameof(ContadorSeleccionados));

            if (ResultadosBusquedaLocales.Count == 0)
            {
                MostrarMensajeError("No se encontraron locales para ese comercio");
            }
        }
        catch (Exception ex)
        {
            MostrarMensajeError($"Error al buscar locales: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SeleccionarLocal(LocalFormModel local)
    {
        // Toggle de selección
        local.EstaSeleccionado = !local.EstaSeleccionado;
        OnPropertyChanged(nameof(ContadorSeleccionados));
    }

    [RelayCommand]
    private void AgregarLocalesSeleccionados()
    {
        var seleccionados = ResultadosBusquedaLocales.Where(l => l.EstaSeleccionado).ToList();

        foreach (var local in seleccionados)
        {
            if (!LocalesAsignados.Any(l => l.IdLocal == local.IdLocal))
            {
                // Crear una nueva instancia para evitar referencias compartidas
                var nuevoLocal = new LocalFormModel
                {
                    IdLocal = local.IdLocal,
                    IdComercio = local.IdComercio,
                    CodigoLocal = local.CodigoLocal,
                    NombreLocal = local.NombreLocal,
                    TipoVia = local.TipoVia,
                    Direccion = local.Direccion,
                    LocalNumero = local.LocalNumero,
                    Escalera = local.Escalera,
                    Piso = local.Piso,
                    CodigoPostal = local.CodigoPostal,
                    Ciudad = local.Ciudad,
                    Pais = local.Pais
                };
                LocalesAsignados.Add(nuevoLocal);
            }
        }

        ActualizarTipoEmpleado();
        CerrarBusquedaLocales();

        if (seleccionados.Count > 0)
        {
            MostrarMensajeExitoNotificacion($"✓ {seleccionados.Count} local(es) agregado(s)");
        }
    }

    [RelayCommand]
    private void CerrarBusquedaLocales()
    {
        BusquedaComercio = string.Empty;
        MostrarResultadosBusqueda = false;
        ResultadosBusquedaLocales.Clear();
    }

    [RelayCommand]
    private void QuitarLocalAsignado(LocalFormModel local)
    {
        LocalesAsignados.Remove(local);
        ActualizarTipoEmpleado();
    }

    // ============================================
    // MÉTODOS AUXILIARES
    // ============================================

    private void LimpiarFormulario()
    {
        FormNombre = string.Empty;
        FormSegundoNombre = string.Empty;
        FormApellidos = string.Empty;
        FormSegundoApellido = string.Empty;
        FormNumeroUsuario = string.Empty;
        FormCorreo = string.Empty;
        FormTelefono = string.Empty;
        FormPassword = string.Empty;
        FormActivo = true;

        LocalesAsignados.Clear();
        ResultadosBusquedaLocales.Clear();
        BusquedaComercio = string.Empty;
        MostrarResultadosBusqueda = false;
        TipoEmpleadoTexto = "REGULAR (1 local asignado)";

        _usuarioEnEdicion = null;
        _passwordGuardada = string.Empty;
    }

    private bool ValidarFormulario(out string mensajeError)
    {
        mensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(FormNombre))
        {
            mensajeError = "El nombre es obligatorio";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormApellidos))
        {
            mensajeError = "Los apellidos son obligatorios";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FormCorreo))
        {
            mensajeError = "El correo electrónico es obligatorio";
            return false;
        }

        if (!FormCorreo.Contains("@") || !FormCorreo.Contains("."))
        {
            mensajeError = "El formato del correo no es válido";
            return false;
        }

        if (!ModoEdicion && string.IsNullOrWhiteSpace(FormPassword))
        {
            mensajeError = "La contraseña es obligatoria para nuevos usuarios";
            return false;
        }

        if (!ModoEdicion && FormPassword.Length < 6)
        {
            mensajeError = "La contraseña debe tener al menos 6 caracteres";
            return false;
        }

        if (!LocalesAsignados.Any())
        {
            mensajeError = "⚠️ DEBE asignar al menos un local al usuario.\n\n" +
                          "Usa el buscador de 'Asignación de Locales' para buscar por nombre de comercio " +
                          "y selecciona el local donde trabajará este usuario.";
            return false;
        }

        if (LocalesAsignados.Any(l => l.IdLocal <= 0))
        {
            mensajeError = "Error: Hay locales asignados sin ID válido. Por favor, elimínalos y asígnalos nuevamente.";
            return false;
        }

        return true;
    }

    private async Task CargarLocalesAsignadosUsuario(int idUsuario)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"SELECT l.id_local, 
                                l.codigo_local, 
                                COALESCE(l.nombre_local, '') as nombre_local,
                                COALESCE(l.tipo_via, '') as tipo_via, 
                                COALESCE(l.direccion, '') as direccion, 
                                COALESCE(l.local_numero, '') as local_numero, 
                                COALESCE(l.escalera, '') as escalera, 
                                COALESCE(l.piso, '') as piso,
                                COALESCE(l.codigo_postal, '') as codigo_postal, 
                                COALESCE(l.pais, '') as pais, 
                                COALESCE(l.telefono, '') as telefono, 
                                COALESCE(l.email, '') as email,
                                c.id_comercio
                        FROM locales l
                        INNER JOIN usuario_locales ul ON l.id_local = ul.id_local
                        INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                        WHERE ul.id_usuario = @IdUsuario
                        ORDER BY ul.es_principal DESC, l.nombre_local";

            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

            LocalesAsignados.Clear();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                LocalesAsignados.Add(new LocalFormModel
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    NombreLocal = reader.GetString(2),
                    TipoVia = reader.GetString(3),
                    Direccion = reader.GetString(4),
                    LocalNumero = reader.GetString(5),
                    Escalera = string.IsNullOrEmpty(reader.GetString(6)) ? null : reader.GetString(6),
                    Piso = string.IsNullOrEmpty(reader.GetString(7)) ? null : reader.GetString(7),
                    CodigoPostal = reader.GetString(8),
                    Pais = reader.GetString(9),
                    Telefono = string.IsNullOrEmpty(reader.GetString(10)) ? null : reader.GetString(10),
                    Email = string.IsNullOrEmpty(reader.GetString(11)) ? null : reader.GetString(11),
                    IdComercio = reader.GetInt32(12)
                });
            }

            ActualizarTipoEmpleado();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar locales asignados: {ex.Message}");
        }
    }

    private async Task InicializarFiltros()
    {
        await Task.Delay(100);

        UsuariosFiltrados.Clear();
        foreach (var usuario in Usuarios.OrderBy(u => u.NombreCompleto))
        {
            UsuariosFiltrados.Add(usuario);
        }
    }

    private async void MostrarMensajeExitoNotificacion(string mensaje)
    {
        MensajeExitoColor = "#28a745";
        MensajeExito = mensaje;
        MostrarMensajeExito = true;
        await Task.Delay(3000);
        MostrarMensajeExito = false;
    }

    private async void MostrarMensajeError(string mensaje)
    {
        MensajeExitoColor = "#dc3545";
        MensajeExito = $"❌ {mensaje}";
        MostrarMensajeExito = true;
        await Task.Delay(5000);
        MostrarMensajeExito = false;
    }
}