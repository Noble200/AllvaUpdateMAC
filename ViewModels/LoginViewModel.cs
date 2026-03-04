using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Services;
using Allva.Desktop.Views;
using Npgsql;
using BCrypt.Net;

namespace Allva.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de inicio de sesion
    /// 
    /// Soporta login de:
    /// - Administradores Allva (codigo = CENTRAL)
    /// - Usuarios Floater (codigo = codigo de uno de sus locales asignados)
    /// - Usuarios Fijos (codigo = codigo de su local asignado)
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        // ============================================
        // LOCALIZACION
        // ============================================

        public LocalizationService Localization => LocalizationService.Instance;

        // ============================================
        // PROPIEDADES OBSERVABLES
        // ============================================

        [ObservableProperty]
        private string _numeroUsuario = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _codigoLocal = string.Empty;

        [ObservableProperty]
        private bool _recordarCredenciales;
        
        [ObservableProperty]
        private bool _recordarSesion;

        [ObservableProperty]
        private bool _mostrarPassword;

        [ObservableProperty]
        private bool _cargando;
        
        public bool IsLoading => Cargando;

        /// <summary>
        /// Muestra el overlay de carga solo si NO hay actualización en progreso
        /// </summary>
        public bool MostrarOverlayCarga => Cargando && !MostrarMensajeActualizacion;

        partial void OnCargandoChanged(bool value)
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(MostrarOverlayCarga));
        }

        [ObservableProperty]
        private string _mensajeError = string.Empty;

        [ObservableProperty]
        private bool _mostrarMensajeError;

        // ============================================
        // PROPIEDADES PARA ACTUALIZACION
        // ============================================

        [ObservableProperty]
        private string _mensajeActualizacion = string.Empty;

        private bool _mostrarMensajeActualizacion;
        public bool MostrarMensajeActualizacion
        {
            get => _mostrarMensajeActualizacion;
            set
            {
                if (SetProperty(ref _mostrarMensajeActualizacion, value))
                {
                    OnPropertyChanged(nameof(MostrarOverlayCarga));
                }
            }
        }

        [ObservableProperty]
        private int _progresoActualizacion;

        public void ActivarMensajeActualizacion(string mensaje, int progreso)
        {
            MensajeActualizacion = mensaje;
            ProgresoActualizacion = progreso;
            MostrarMensajeActualizacion = true;
            Cargando = true;
        }

        public void DesactivarMensajeActualizacion()
        {
            MostrarMensajeActualizacion = false;
            MensajeActualizacion = string.Empty;
            ProgresoActualizacion = 0;
            Cargando = false;
        }

        // ============================================
        // COMANDOS
        // ============================================

        [RelayCommand]
        private async Task IniciarSesion()
        {
            MensajeError = string.Empty;
            MostrarMensajeError = false;

            if (!ValidarCampos())
                return;

            Cargando = true;

            try
            {
                var codigoLocalUpper = CodigoLocal.Trim().ToUpper();
                
                // =============================================
                // CASO 1: ADMINISTRADOR ALLVA (codigo = CENTRAL)
                // =============================================
                if (codigoLocalUpper == "CENTRAL")
                {
                    var resultadoAdmin = await AutenticarAdministradorAllva(NumeroUsuario!, Password!);
                    
                    if (resultadoAdmin.Exitoso)
                    {
                        // Actualizar ultimo_acceso del administrador Allva
                        await ActualizarUltimoAccesoAdmin(resultadoAdmin.IdUsuario);

                        var loginData = new LoginSuccessData
                        {
                            UserName = resultadoAdmin.NombreCompleto,
                            UserNumber = NumeroUsuario,
                            LocalCode = "CENTRAL",
                            LocalName = "Central Allva",
                            IdLocal = 0,
                            IdComercio = 0,
                            IdUsuario = resultadoAdmin.IdUsuario,
                            Token = $"token-{Guid.NewGuid()}",
                            IsSystemAdmin = true,
                            UserType = "ADMIN_ALLVA",
                            RoleName = "Administrador_Allva",
                            EsFloater = false,

                            Permisos = new PermisosAdministrador
                            {
                                AccesoGestionComercios = resultadoAdmin.AccesoGestionComercios,
                                AccesoGestionUsuariosLocales = resultadoAdmin.AccesoGestionUsuariosLocales,
                                AccesoGestionUsuariosAllva = resultadoAdmin.AccesoGestionUsuariosAllva,
                                AccesoAnalytics = resultadoAdmin.AccesoAnalytics,
                                AccesoConfiguracionSistema = resultadoAdmin.AccesoConfiguracionSistema,
                                AccesoFacturacionGlobal = resultadoAdmin.AccesoFacturacionGlobal,
                                AccesoAuditoria = resultadoAdmin.AccesoAuditoria
                            }
                        };

                        var navigationService = new NavigationService();
                        navigationService.NavigateToAdminDashboard(loginData);
                        return;
                    }
                    else
                    {
                        MostrarError(resultadoAdmin.MensajeError);
                        return;
                    }
                }
                
                // =============================================
                // CASO 2 y 3: USUARIO NORMAL O FLOATER
                // =============================================
                var resultadoUsuario = await AutenticarUsuario(NumeroUsuario!, Password!, codigoLocalUpper);
                
                if (resultadoUsuario.Exitoso)
                {
                    var loginData = new LoginSuccessData
                    {
                        UserName = resultadoUsuario.NombreCompleto,
                        UserNumber = NumeroUsuario,
                        LocalCode = codigoLocalUpper,
                        LocalName = resultadoUsuario.NombreLocal,
                        IdLocal = resultadoUsuario.IdLocal,
                        IdComercio = resultadoUsuario.IdComercio,
                        IdUsuario = resultadoUsuario.IdUsuario,
                        Token = $"token-{Guid.NewGuid()}",
                        IsSystemAdmin = false,
                        UserType = resultadoUsuario.EsFloater ? "FLOATER" : "USUARIO_LOCAL",
                        RoleName = resultadoUsuario.EsFloater ? "Usuario_Floater" : "Usuario_Local",
                        EsFloater = resultadoUsuario.EsFloater,
                        ModuloDivisas = resultadoUsuario.ModuloDivisas,
                        ModuloPackAlimentos = resultadoUsuario.ModuloPackAlimentos,
                        ModuloBilletesAvion = resultadoUsuario.ModuloBilletesAvion,
                        ModuloPackViajes = resultadoUsuario.ModuloPackViajes
                    };

                    await RegistrarSesion(resultadoUsuario.IdUsuario, resultadoUsuario.IdLocal, loginData.Token);

                    var navigationService = new NavigationService();
                    navigationService.NavigateTo("MainDashboard", loginData);
                }
                else
                {
                    MostrarError(resultadoUsuario.MensajeError);
                }
            }
            catch (NpgsqlException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error NpgsqlException: {ex.Message}");
                MostrarError("No se pudo conectar con el servidor. Verifica tu conexion a internet e intenta nuevamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Exception: {ex.Message}\n{ex.StackTrace}");
                MostrarError("Ocurrio un error inesperado. Por favor, intenta nuevamente.");
            }
            finally
            {
                Cargando = false;
            }
        }
        
        public IAsyncRelayCommand LoginCommand => IniciarSesionCommand;

        [RelayCommand]
        private void ToggleMostrarPassword()
        {
            MostrarPassword = !MostrarPassword;
        }
        
        [RelayCommand]
        private void CambiarIdioma(string idioma)
        {
            Localization.SetLanguage(idioma);
        }
        
        [RelayCommand]
        private async Task RecuperarPassword()
        {
            try
            {
                Window? mainWindow = null;
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    mainWindow = desktop.MainWindow;
                }

                if (mainWindow == null) return;

                var ventana = new RecuperarContrasenaView();
                await ventana.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en recuperar password: {ex.Message}");
            }
        }

        // ============================================
        // METODOS DE AUTENTICACION
        // ============================================

        private async Task<ResultadoAutenticacion> AutenticarAdministradorAllva(string nombreUsuario, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        id_administrador,
                        nombre_usuario,
                        nombre,
                        apellidos,
                        password_hash,
                        activo,
                        acceso_gestion_comercios,
                        acceso_gestion_usuarios_locales,
                        acceso_gestion_usuarios_allva,
                        acceso_analytics,
                        acceso_configuracion_sistema,
                        acceso_facturacion_global,
                        acceso_auditoria
                    FROM administradores_allva
                    WHERE nombre_usuario = @NombreUsuario";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario.ToLower());

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Usuario de administrador no encontrado. Verifica tu numero de usuario."
                    };
                }

                if (!reader.GetBoolean(5))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Tu cuenta de administrador esta desactivada. Contacta al soporte tecnico."
                    };
                }

                var passwordHash = reader.GetString(4);
                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contrasena incorrecta. Verifica tu contrasena e intenta nuevamente."
                    };
                }

                var nombreCompleto = $"{reader.GetString(2)} {reader.GetString(3)}";

                return new ResultadoAutenticacion
                {
                    Exitoso = true,
                    IdUsuario = reader.GetInt32(0),
                    NombreCompleto = nombreCompleto,
                    EsAdministradorAllva = true,
                    AccesoGestionComercios = reader.GetBoolean(6),
                    AccesoGestionUsuariosLocales = reader.GetBoolean(7),
                    AccesoGestionUsuariosAllva = reader.GetBoolean(8),
                    AccesoAnalytics = reader.GetBoolean(9),
                    AccesoConfiguracionSistema = reader.GetBoolean(10),
                    AccesoFacturacionGlobal = reader.GetBoolean(11),
                    AccesoAuditoria = reader.GetBoolean(12)
                };
            }
            catch (NpgsqlException)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "No se pudo conectar con el servidor. Verifica tu conexion a internet."
                };
            }
            catch (Exception)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "Ocurrio un error al verificar tus credenciales. Intenta nuevamente."
                };
            }
        }

        private async Task<ResultadoAutenticacion> AutenticarUsuario(string numeroUsuario, string password, string codigoLocal)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Obtener datos del usuario
                var queryUsuario = @"
                    SELECT 
                        u.id_usuario,
                        u.numero_usuario,
                        u.nombre,
                        u.apellidos,
                        u.password_hash,
                        u.activo,
                        u.es_flooter,
                        u.id_local,
                        u.id_comercio
                    FROM usuarios u
                    WHERE u.numero_usuario = @NumeroUsuario";

                using var cmdUsuario = new NpgsqlCommand(queryUsuario, connection);
                cmdUsuario.Parameters.AddWithValue("@NumeroUsuario", numeroUsuario);

                int idUsuario;
                string nombre, apellidos, passwordHash;
                bool activo, esFloater;
                int? idLocalUsuario, idComercioUsuario;

                using (var reader = await cmdUsuario.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return new ResultadoAutenticacion
                        {
                            Exitoso = false,
                            MensajeError = "Usuario no encontrado. Verifica que tu numero de usuario sea correcto."
                        };
                    }

                    idUsuario = reader.GetInt32(0);
                    nombre = reader.GetString(2);
                    apellidos = reader.GetString(3);
                    passwordHash = reader.GetString(4);
                    activo = reader.GetBoolean(5);
                    esFloater = reader.IsDBNull(6) ? false : reader.GetBoolean(6);
                    idLocalUsuario = reader.IsDBNull(7) ? null : reader.GetInt32(7);
                    idComercioUsuario = reader.IsDBNull(8) ? null : reader.GetInt32(8);
                }

                if (!activo)
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Tu cuenta esta desactivada. Contacta a tu administrador para mas informacion."
                    };
                }

                if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
                {
                    return new ResultadoAutenticacion
                    {
                        Exitoso = false,
                        MensajeError = "Contrasena incorrecta. Verifica e intenta nuevamente."
                    };
                }

                // Verificar acceso al local segun tipo de usuario
                if (esFloater)
                {
                    return await VerificarAccesoFloater(connection, idUsuario, nombre, apellidos, codigoLocal);
                }
                else
                {
                    return await VerificarAccesoUsuarioNormal(connection, idUsuario, nombre, apellidos, codigoLocal, idLocalUsuario);
                }
            }
            catch (NpgsqlException)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "No se pudo conectar con el servidor. Verifica tu conexion a internet."
                };
            }
            catch (Exception)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "Ocurrio un error al verificar tus credenciales. Intenta nuevamente."
                };
            }
        }

        private async Task<ResultadoAutenticacion> VerificarAccesoFloater(
            NpgsqlConnection connection,
            int idUsuario,
            string nombre,
            string apellidos,
            string codigoLocal)
        {
            // Primero verificar si el codigo de oficina existe en el sistema
            var queryExisteLocal = @"
                SELECT id_local, nombre_local, activo
                FROM locales
                WHERE codigo_local = @CodigoLocal";

            using (var cmdExiste = new NpgsqlCommand(queryExisteLocal, connection))
            {
                cmdExiste.Parameters.AddWithValue("@CodigoLocal", codigoLocal);
                using (var readerExiste = await cmdExiste.ExecuteReaderAsync())
                {
                    if (!await readerExiste.ReadAsync())
                    {
                        return new ResultadoAutenticacion
                        {
                            Exitoso = false,
                            MensajeError = $"El codigo de oficina '{codigoLocal}' no existe. Verifica el codigo e intenta nuevamente."
                        };
                    }
                }
            }

            // Ahora verificar si el usuario tiene acceso a ese local
            int idLocal;
            string nombreLocal;
            bool localActivo;
            int idComercioLocal;
            bool moduloDivisas, moduloAlimentos, moduloBilletes, moduloViajes;

            var queryFloater = @"
                SELECT
                    l.id_local,
                    l.codigo_local,
                    l.nombre_local,
                    l.activo,
                    l.id_comercio,
                    COALESCE(l.modulo_divisas, false),
                    COALESCE(l.modulo_pack_alimentos, false),
                    COALESCE(l.modulo_billetes_avion, false),
                    COALESCE(l.modulo_pack_viajes, false)
                FROM usuario_locales ul
                INNER JOIN locales l ON ul.id_local = l.id_local
                WHERE ul.id_usuario = @IdUsuario
                AND l.codigo_local = @CodigoLocal";

            using (var cmdFloater = new NpgsqlCommand(queryFloater, connection))
            {
                cmdFloater.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmdFloater.Parameters.AddWithValue("@CodigoLocal", codigoLocal);

                using (var reader = await cmdFloater.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return new ResultadoAutenticacion
                        {
                            Exitoso = false,
                            MensajeError = $"No tienes permiso para acceder a la oficina '{codigoLocal}'. Contacta a tu administrador."
                        };
                    }

                    idLocal = reader.GetInt32(0);
                    nombreLocal = reader.GetString(2);
                    localActivo = reader.GetBoolean(3);
                    idComercioLocal = reader.GetInt32(4);
                    moduloDivisas = reader.GetBoolean(5);
                    moduloAlimentos = reader.GetBoolean(6);
                    moduloBilletes = reader.GetBoolean(7);
                    moduloViajes = reader.GetBoolean(8);
                }
            }

            if (!localActivo)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"La oficina '{codigoLocal}' esta temporalmente inactiva. Contacta a tu administrador."
                };
            }

            // Verificar que el comercio este activo
            var queryComercio = @"
                SELECT activo, nombre_comercio
                FROM comercios
                WHERE id_comercio = @IdComercio";

            using (var cmdComercio = new NpgsqlCommand(queryComercio, connection))
            {
                cmdComercio.Parameters.AddWithValue("@IdComercio", idComercioLocal);
                using (var readerComercio = await cmdComercio.ExecuteReaderAsync())
                {
                    if (await readerComercio.ReadAsync())
                    {
                        var comercioActivo = readerComercio.GetBoolean(0);
                        if (!comercioActivo)
                        {
                            return new ResultadoAutenticacion
                            {
                                Exitoso = false,
                                MensajeError = "El comercio asociado a esta oficina esta temporalmente suspendido. Contacta a soporte."
                            };
                        }
                    }
                }
            }

            return new ResultadoAutenticacion
            {
                Exitoso = true,
                IdUsuario = idUsuario,
                NombreCompleto = $"{nombre} {apellidos}",
                EsAdministradorAllva = false,
                EsFloater = true,
                IdLocal = idLocal,
                NombreLocal = nombreLocal,
                CodigoLocal = codigoLocal,
                IdComercio = idComercioLocal,
                ModuloDivisas = moduloDivisas,
                ModuloPackAlimentos = moduloAlimentos,
                ModuloBilletesAvion = moduloBilletes,
                ModuloPackViajes = moduloViajes
            };
        }

        private async Task<ResultadoAutenticacion> VerificarAccesoUsuarioNormal(
            NpgsqlConnection connection,
            int idUsuario,
            string nombre,
            string apellidos,
            string codigoLocal,
            int? idLocalUsuario)
        {
            if (!idLocalUsuario.HasValue)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = "No tienes una oficina asignada. Contacta a tu administrador para que te asigne una."
                };
            }

            // Primero verificar si el codigo de oficina existe en el sistema
            var queryExisteLocal = @"
                SELECT id_local, nombre_local, activo
                FROM locales
                WHERE codigo_local = @CodigoLocal";

            using (var cmdExiste = new NpgsqlCommand(queryExisteLocal, connection))
            {
                cmdExiste.Parameters.AddWithValue("@CodigoLocal", codigoLocal);
                using (var readerExiste = await cmdExiste.ExecuteReaderAsync())
                {
                    if (!await readerExiste.ReadAsync())
                    {
                        return new ResultadoAutenticacion
                        {
                            Exitoso = false,
                            MensajeError = $"El codigo de oficina '{codigoLocal}' no existe. Verifica el codigo e intenta nuevamente."
                        };
                    }
                }
            }

            // Verificar si el usuario tiene acceso a ese local especifico
            int idLocal;
            string nombreLocal;
            bool localActivo;
            int idComercioLocal;
            bool moduloDivisas, moduloAlimentos, moduloBilletes, moduloViajes;

            var queryLocal = @"
                SELECT
                    l.id_local,
                    l.codigo_local,
                    l.nombre_local,
                    l.activo,
                    l.id_comercio,
                    COALESCE(l.modulo_divisas, false),
                    COALESCE(l.modulo_pack_alimentos, false),
                    COALESCE(l.modulo_billetes_avion, false),
                    COALESCE(l.modulo_pack_viajes, false)
                FROM locales l
                WHERE l.id_local = @IdLocal
                AND l.codigo_local = @CodigoLocal";

            using (var cmdLocal = new NpgsqlCommand(queryLocal, connection))
            {
                cmdLocal.Parameters.AddWithValue("@IdLocal", idLocalUsuario.Value);
                cmdLocal.Parameters.AddWithValue("@CodigoLocal", codigoLocal);

                using (var reader = await cmdLocal.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return new ResultadoAutenticacion
                        {
                            Exitoso = false,
                            MensajeError = $"No tienes permiso para acceder a la oficina '{codigoLocal}'. Solo puedes acceder a tu oficina asignada."
                        };
                    }

                    idLocal = reader.GetInt32(0);
                    nombreLocal = reader.GetString(2);
                    localActivo = reader.GetBoolean(3);
                    idComercioLocal = reader.GetInt32(4);
                    moduloDivisas = reader.GetBoolean(5);
                    moduloAlimentos = reader.GetBoolean(6);
                    moduloBilletes = reader.GetBoolean(7);
                    moduloViajes = reader.GetBoolean(8);
                }
            }

            if (!localActivo)
            {
                return new ResultadoAutenticacion
                {
                    Exitoso = false,
                    MensajeError = $"La oficina '{codigoLocal}' esta temporalmente inactiva. Contacta a tu administrador."
                };
            }

            // Verificar que el comercio este activo
            var queryComercio = @"
                SELECT activo, nombre_comercio
                FROM comercios
                WHERE id_comercio = @IdComercio";

            using (var cmdComercio = new NpgsqlCommand(queryComercio, connection))
            {
                cmdComercio.Parameters.AddWithValue("@IdComercio", idComercioLocal);
                using (var readerComercio = await cmdComercio.ExecuteReaderAsync())
                {
                    if (await readerComercio.ReadAsync())
                    {
                        var comercioActivo = readerComercio.GetBoolean(0);
                        if (!comercioActivo)
                        {
                            return new ResultadoAutenticacion
                            {
                                Exitoso = false,
                                MensajeError = "El comercio asociado a esta oficina esta temporalmente suspendido. Contacta a soporte."
                            };
                        }
                    }
                }
            }

            return new ResultadoAutenticacion
            {
                Exitoso = true,
                IdUsuario = idUsuario,
                NombreCompleto = $"{nombre} {apellidos}",
                EsAdministradorAllva = false,
                EsFloater = false,
                IdLocal = idLocal,
                NombreLocal = nombreLocal,
                CodigoLocal = codigoLocal,
                IdComercio = idComercioLocal,
                ModuloDivisas = moduloDivisas,
                ModuloPackAlimentos = moduloAlimentos,
                ModuloBilletesAvion = moduloBilletes,
                ModuloPackViajes = moduloViajes
            };
        }

        private async Task RegistrarSesion(int idUsuario, int idLocal, string token)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Cerrar sesiones anteriores
                var queryCerrar = @"
                    UPDATE sesiones 
                    SET sesion_activa = false, 
                        fecha_cierre = CURRENT_TIMESTAMP,
                        motivo_cierre = 'Nuevo inicio de sesion'
                    WHERE id_usuario = @IdUsuario AND sesion_activa = true";

                using (var cmdCerrar = new NpgsqlCommand(queryCerrar, connection))
                {
                    cmdCerrar.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    await cmdCerrar.ExecuteNonQueryAsync();
                }

                // Crear nueva sesion
                var queryNueva = @"
                    INSERT INTO sesiones (id_usuario, id_local_activo, token_jwt, fecha_expiracion, sesion_activa)
                    VALUES (@IdUsuario, @IdLocal, @Token, @FechaExpiracion, true)";

                using var cmdNueva = new NpgsqlCommand(queryNueva, connection);
                cmdNueva.Parameters.AddWithValue("@IdUsuario", idUsuario);
                cmdNueva.Parameters.AddWithValue("@IdLocal", idLocal);
                cmdNueva.Parameters.AddWithValue("@Token", token);
                cmdNueva.Parameters.AddWithValue("@FechaExpiracion", DateTime.Now.AddHours(8));

                await cmdNueva.ExecuteNonQueryAsync();

                // Actualizar ultimo acceso
                var queryAcceso = @"
                    UPDATE usuarios 
                    SET ultimo_acceso = CURRENT_TIMESTAMP 
                    WHERE id_usuario = @IdUsuario";

                using var cmdAcceso = new NpgsqlCommand(queryAcceso, connection);
                cmdAcceso.Parameters.AddWithValue("@IdUsuario", idUsuario);
                await cmdAcceso.ExecuteNonQueryAsync();
            }
            catch
            {
                // No interrumpir el login si falla el registro de sesion
            }
        }

        private async Task ActualizarUltimoAccesoAdmin(int idAdministrador)
        {
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();

                // Usar zona horaria de España (Europe/Madrid)
                var queryAcceso = @"
                    UPDATE administradores_allva
                    SET ultimo_acceso = (CURRENT_TIMESTAMP AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Madrid')
                    WHERE id_administrador = @IdAdmin";

                using var cmdAcceso = new NpgsqlCommand(queryAcceso, connection);
                cmdAcceso.Parameters.AddWithValue("@IdAdmin", idAdministrador);
                await cmdAcceso.ExecuteNonQueryAsync();
            }
            catch
            {
                // No interrumpir el login si falla la actualizacion
            }
        }

        // ============================================
        // VALIDACIONES
        // ============================================

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NumeroUsuario))
            {
                MostrarError("Ingresa tu numero de usuario para continuar.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MostrarError("Ingresa tu contrasena para continuar.");
                return false;
            }

            if (Password.Length < 6)
            {
                MostrarError("La contrasena debe tener al menos 6 caracteres.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CodigoLocal))
            {
                MostrarError("Ingresa el codigo de tu oficina para continuar.");
                return false;
            }

            return true;
        }

        private void MostrarError(string mensaje)
        {
            MensajeError = mensaje;
            MostrarMensajeError = true;

            Task.Delay(6000).ContinueWith(_ =>
            {
                MostrarMensajeError = false;
            });
        }
    }

    // ============================================
    // CLASES DE SOPORTE
    // ============================================

    public class ResultadoAutenticacion
    {
        public bool Exitoso { get; set; }
        public string MensajeError { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public bool EsAdministradorAllva { get; set; }
        public bool EsFloater { get; set; }

        public int IdUsuario { get; set; }
        public int IdLocal { get; set; }
        public int IdComercio { get; set; }
        public string NombreLocal { get; set; } = string.Empty;
        public string CodigoLocal { get; set; } = string.Empty;

        // Permisos de administrador Allva
        public bool AccesoGestionComercios { get; set; }
        public bool AccesoGestionUsuariosLocales { get; set; }
        public bool AccesoGestionUsuariosAllva { get; set; }
        public bool AccesoAnalytics { get; set; }
        public bool AccesoConfiguracionSistema { get; set; }
        public bool AccesoFacturacionGlobal { get; set; }
        public bool AccesoAuditoria { get; set; }

        // Permisos de modulos del local
        public bool ModuloDivisas { get; set; }
        public bool ModuloPackAlimentos { get; set; }
        public bool ModuloBilletesAvion { get; set; }
        public bool ModuloPackViajes { get; set; }
    }
}