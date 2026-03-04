using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Npgsql;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para gestionar contraseñas de acceso al FrontOffice por comercio
/// </summary>
public partial class ContrasenaAdminFrontViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    [ObservableProperty]
    private ObservableCollection<ComercioContrasenaModel> _comercios = new();

    [ObservableProperty]
    private ObservableCollection<ComercioContrasenaModel> _comerciosFiltrados = new();

    [ObservableProperty]
    private bool _cargando = false;

    [ObservableProperty]
    private bool _mostrarMensajeExito = false;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private int _totalComercios = 0;

    [ObservableProperty]
    private int _comerciosConContrasena = 0;

    [ObservableProperty]
    private int _comerciosSinContrasena = 0;

    [ObservableProperty]
    private bool _mostrarPanelDetalle = false;

    [ObservableProperty]
    private ComercioContrasenaModel? _comercioSeleccionado;

    [ObservableProperty]
    private string _contrasenaEditando = string.Empty;

    public ContrasenaAdminFrontViewModel()
    {
        _ = CargarComercios();
    }

    partial void OnFiltroBusquedaChanged(string value)
    {
        AplicarFiltros();
    }

    [RelayCommand]
    private async Task CargarComercios()
    {
        Cargando = true;
        try
        {
            Comercios.Clear();

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Obtener todos los comercios activos con su contraseña (si existe)
            var query = @"
                SELECT
                    c.id_comercio,
                    c.nombre_comercio,
                    c.pais,
                    c.mail_contacto,
                    c.activo,
                    caf.contrasena_visible,
                    caf.fecha_creacion,
                    caf.fecha_ultima_modificacion,
                    caf.activo as contrasena_activa
                FROM comercios c
                LEFT JOIN contrasenas_admin_front caf ON c.id_comercio = caf.id_comercio
                WHERE c.activo = true
                ORDER BY c.nombre_comercio ASC";

            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var comercio = new ComercioContrasenaModel
                {
                    IdComercio = reader.GetInt32(0),
                    NombreComercio = reader.GetString(1),
                    Pais = reader.IsDBNull(2) ? "N/A" : reader.GetString(2),
                    MailContacto = reader.IsDBNull(3) ? "N/A" : reader.GetString(3),
                    Activo = reader.GetBoolean(4),
                    Contrasena = reader.IsDBNull(5) ? null : reader.GetString(5),
                    FechaCreacion = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    FechaUltimaModificacion = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    ContrasenaActiva = reader.IsDBNull(8) ? false : reader.GetBoolean(8)
                };

                Comercios.Add(comercio);
            }

            ActualizarEstadisticas();
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar comercios: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private void AplicarFiltros()
    {
        var comerciosFiltrados = Comercios.AsEnumerable();

        // Filtro por búsqueda
        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            var busqueda = FiltroBusqueda.ToLower();
            comerciosFiltrados = comerciosFiltrados.Where(c =>
                c.NombreComercio.ToLower().Contains(busqueda) ||
                c.Pais.ToLower().Contains(busqueda) ||
                c.MailContacto.ToLower().Contains(busqueda));
        }

        ComerciosFiltrados.Clear();
        foreach (var comercio in comerciosFiltrados)
        {
            ComerciosFiltrados.Add(comercio);
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FiltroBusqueda = string.Empty;
    }

    [RelayCommand]
    private void SeleccionarComercio(ComercioContrasenaModel? comercio)
    {
        if (comercio == null) return;
        ComercioSeleccionado = comercio;
        ContrasenaEditando = comercio.Contrasena ?? string.Empty;
        MostrarPanelDetalle = true;
    }

    [RelayCommand]
    private void CerrarPanelDetalle()
    {
        MostrarPanelDetalle = false;
        ComercioSeleccionado = null;
        ContrasenaEditando = string.Empty;
    }

    [RelayCommand]
    private void GenerarContrasenaAleatoria()
    {
        ContrasenaEditando = GenerarNuevaContrasena();
    }

    [RelayCommand]
    private async Task GuardarContrasena()
    {
        if (ComercioSeleccionado == null) return;
        if (string.IsNullOrWhiteSpace(ContrasenaEditando))
        {
            MostrarMensaje("La contraseña no puede estar vacía");
            return;
        }

        Cargando = true;
        try
        {
            var contrasena = ContrasenaEditando.Trim();
            var contrasenaHash = HashPassword(contrasena);

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Verificar si ya existe una contraseña para este comercio
            var queryCheck = "SELECT COUNT(*) FROM contrasenas_admin_front WHERE id_comercio = @idComercio";
            await using var commandCheck = new NpgsqlCommand(queryCheck, connection);
            commandCheck.Parameters.AddWithValue("@idComercio", ComercioSeleccionado.IdComercio);
            var existe = Convert.ToInt32(await commandCheck.ExecuteScalarAsync()) > 0;

            if (existe)
            {
                // Actualizar contraseña existente
                var queryUpdate = @"
                    UPDATE contrasenas_admin_front
                    SET contrasena_hash = @contrasenaHash,
                        contrasena_visible = @contrasenaVisible,
                        fecha_ultima_modificacion = CURRENT_TIMESTAMP,
                        activo = true
                    WHERE id_comercio = @idComercio";

                await using var commandUpdate = new NpgsqlCommand(queryUpdate, connection);
                commandUpdate.Parameters.AddWithValue("@contrasenaHash", contrasenaHash);
                commandUpdate.Parameters.AddWithValue("@contrasenaVisible", contrasena);
                commandUpdate.Parameters.AddWithValue("@idComercio", ComercioSeleccionado.IdComercio);
                await commandUpdate.ExecuteNonQueryAsync();
            }
            else
            {
                // Insertar nueva contraseña
                var queryInsert = @"
                    INSERT INTO contrasenas_admin_front
                    (id_comercio, contrasena_hash, contrasena_visible, activo)
                    VALUES (@idComercio, @contrasenaHash, @contrasenaVisible, true)";

                await using var commandInsert = new NpgsqlCommand(queryInsert, connection);
                commandInsert.Parameters.AddWithValue("@idComercio", ComercioSeleccionado.IdComercio);
                commandInsert.Parameters.AddWithValue("@contrasenaHash", contrasenaHash);
                commandInsert.Parameters.AddWithValue("@contrasenaVisible", contrasena);
                await commandInsert.ExecuteNonQueryAsync();
            }

            // Actualizar el modelo del comercio seleccionado
            ComercioSeleccionado.Contrasena = contrasena;
            ComercioSeleccionado.FechaCreacion = existe ? ComercioSeleccionado.FechaCreacion : DateTime.Now;
            ComercioSeleccionado.FechaUltimaModificacion = DateTime.Now;
            ComercioSeleccionado.ContrasenaActiva = true;

            // Actualizar también en la lista
            var comercioEnLista = Comercios.FirstOrDefault(c => c.IdComercio == ComercioSeleccionado.IdComercio);
            if (comercioEnLista != null)
            {
                comercioEnLista.Contrasena = contrasena;
                comercioEnLista.FechaCreacion = ComercioSeleccionado.FechaCreacion;
                comercioEnLista.FechaUltimaModificacion = ComercioSeleccionado.FechaUltimaModificacion;
                comercioEnLista.ContrasenaActiva = true;
            }

            OnPropertyChanged(nameof(ComercioSeleccionado));
            MostrarMensaje($"Contraseña guardada para {ComercioSeleccionado.NombreComercio}");
            ActualizarEstadisticas();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar contraseña: {ex.Message}");
            MostrarMensaje($"Error: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private void CopiarContrasena(ComercioContrasenaModel? comercio)
    {
        if (comercio == null || string.IsNullOrEmpty(comercio.Contrasena)) return;

        // Por ahora, solo mostramos un mensaje informativo
        // TODO: Implementar copia al portapapeles con TextCopy o Avalonia Clipboard
        MostrarMensaje($"Contraseña: {comercio.Contrasena}");
    }

    private string GenerarNuevaContrasena()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private void ActualizarEstadisticas()
    {
        TotalComercios = Comercios.Count;
        ComerciosConContrasena = Comercios.Count(c => !string.IsNullOrEmpty(c.Contrasena));
        ComerciosSinContrasena = TotalComercios - ComerciosConContrasena;
    }

    private async void MostrarMensaje(string mensaje)
    {
        MensajeExito = mensaje;
        MostrarMensajeExito = true;
        await Task.Delay(3000);
        MostrarMensajeExito = false;
    }
}

/// <summary>
/// Modelo para representar un comercio con su contraseña de FrontOffice
/// </summary>
public partial class ComercioContrasenaModel : ObservableObject
{
    [ObservableProperty]
    private int _idComercio;

    [ObservableProperty]
    private string _nombreComercio = string.Empty;

    [ObservableProperty]
    private string _pais = string.Empty;

    [ObservableProperty]
    private string _mailContacto = string.Empty;

    [ObservableProperty]
    private bool _activo;

    [ObservableProperty]
    private string? _contrasena;

    [ObservableProperty]
    private DateTime? _fechaCreacion;

    [ObservableProperty]
    private DateTime? _fechaUltimaModificacion;

    [ObservableProperty]
    private bool _contrasenaActiva;

    public bool TieneContrasena => !string.IsNullOrEmpty(Contrasena);

    public string EstadoContrasena => TieneContrasena ? "Configurada" : "Sin configurar";

    public string ColorEstado => TieneContrasena ? "#28a745" : "#dc3545";

    public string FechaCreacionTexto => FechaCreacion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";

    public string FechaModificacionTexto => FechaUltimaModificacion?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";

    public string TextoBoton => TieneContrasena ? "REGENERAR" : "GENERAR";

    public string ContrasenaOculta => TieneContrasena ? "••••••••" : "Sin contraseña";
}
