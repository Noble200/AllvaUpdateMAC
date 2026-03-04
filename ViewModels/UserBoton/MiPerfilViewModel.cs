using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Views.UserBoton;

namespace Allva.Desktop.ViewModels.UserBoton;

public partial class MiPerfilViewModel : ObservableObject
{
    [ObservableProperty]
    private string _nombre = "";

    [ObservableProperty]
    private string _segundoNombre = "";

    [ObservableProperty]
    private string _apellidos = "";

    [ObservableProperty]
    private string _segundoApellido = "";

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _telefono = "";

    [ObservableProperty]
    private string _numeroUsuario = "";

    [ObservableProperty]
    private string _codigoLocal = "";

    // Propiedades combinadas para mostrar nombre y apellidos completos
    public string NombreCompletoDisplay => string.IsNullOrWhiteSpace(SegundoNombre)
        ? Nombre
        : $"{Nombre} {SegundoNombre}";

    public string ApellidosCompletoDisplay => string.IsNullOrWhiteSpace(SegundoApellido)
        ? Apellidos
        : $"{Apellidos} {SegundoApellido}";

    [ObservableProperty]
    private int _idUsuario;

    [ObservableProperty]
    private int _idLocal;

    [ObservableProperty]
    private int _idComercio;

    [ObservableProperty]
    private bool _modoEdicion = false;

    [ObservableProperty]
    private string _mensajeEstado = "";

    [ObservableProperty]
    private bool _mensajeVisible = false;

    [ObservableProperty]
    private Avalonia.Media.IBrush _colorMensaje = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#008800"));

    public string BotonEditarTexto => ModoEdicion ? "Guardar" : "Editar";

    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();

    public string Iniciales
    {
        get
        {
            var iniciales = "";
            if (!string.IsNullOrEmpty(Nombre) && Nombre.Length > 0)
                iniciales += Nombre[0];
            if (!string.IsNullOrEmpty(Apellidos) && Apellidos.Length > 0)
                iniciales += Apellidos[0];
            return iniciales.ToUpper();
        }
    }

    private Action? _cerrarAction;
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // Valores originales para cancelar edición
    private string _emailOriginal = "";
    private string _telefonoOriginal = "";

    public MiPerfilViewModel()
    {
        // Constructor por defecto para el designer
        Nombre = "Usuario";
        Apellidos = "Demo";
        Email = "usuario@ejemplo.com";
        Telefono = "+34 600 000 000";
        NumeroUsuario = "USR001";
        CodigoLocal = "LOCAL01";
    }

    public MiPerfilViewModel(int idUsuario, string nombreUsuario, string numeroUsuario,
                              int idLocal, string codigoLocal, int idComercio, Action? cerrarAction = null)
    {
        IdUsuario = idUsuario;
        IdLocal = idLocal;
        IdComercio = idComercio;
        NumeroUsuario = numeroUsuario;
        CodigoLocal = codigoLocal;
        _cerrarAction = cerrarAction;

        // Separar nombre y apellidos del nombreUsuario
        var partes = nombreUsuario.Split(' ', 2);
        Nombre = partes.Length > 0 ? partes[0] : "";
        Apellidos = partes.Length > 1 ? partes[1] : "";

        // Cargar datos del usuario desde la base de datos
        _ = CargarDatosUsuarioAsync();
    }

    private async Task CargarDatosUsuarioAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT nombre, segundo_nombre, apellidos, segundo_apellido, correo, telefono
                FROM usuarios
                WHERE id_usuario = @idUsuario";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@idUsuario", IdUsuario);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                Nombre = reader.IsDBNull(0) ? "" : reader.GetString(0);
                SegundoNombre = reader.IsDBNull(1) ? "" : reader.GetString(1);
                Apellidos = reader.IsDBNull(2) ? "" : reader.GetString(2);
                SegundoApellido = reader.IsDBNull(3) ? "" : reader.GetString(3);
                Email = reader.IsDBNull(4) ? "" : reader.GetString(4);
                Telefono = reader.IsDBNull(5) ? "" : reader.GetString(5);

                // Guardar valores originales para cancelar
                _emailOriginal = Email;
                _telefonoOriginal = Telefono;

                // Notificar cambios en propiedades computadas
                OnPropertyChanged(nameof(NombreCompleto));
                OnPropertyChanged(nameof(NombreCompletoDisplay));
                OnPropertyChanged(nameof(ApellidosCompletoDisplay));
                OnPropertyChanged(nameof(Iniciales));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando datos usuario: {ex.Message}");
            // Mantener datos placeholder en caso de error
            Email = "usuario@allva.com";
            Telefono = "+34 600 000 000";
        }
    }

    public void SetCerrarAction(Action cerrarAction)
    {
        _cerrarAction = cerrarAction;
    }

    [RelayCommand]
    private async Task CambiarContrasenaAsync()
    {
        try
        {
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }

            if (mainWindow == null) return;

            // Abrir ventana de confirmación para enviar email
            var ventana = new CambiarContrasenaView(Email, NombreCompleto);
            await ventana.ShowDialog(mainWindow);

            if (ventana.EmailEnviado && !string.IsNullOrEmpty(ventana.CodigoGenerado))
            {
                // Abrir ventana para ingresar código y nueva contraseña
                var ventanaVerificacion = new VerificarCodigoView(
                    ventana.CodigoGenerado,
                    IdUsuario,
                    NombreCompleto
                );
                await ventanaVerificacion.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en cambiar contrasena: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleEditarAsync()
    {
        if (ModoEdicion)
        {
            // Guardar cambios
            await GuardarCambios();
        }
        else
        {
            // Guardar valores originales antes de editar
            _emailOriginal = Email;
            _telefonoOriginal = Telefono;

            // Entrar en modo edición
            ModoEdicion = true;
            OnPropertyChanged(nameof(BotonEditarTexto));
        }
    }

    [RelayCommand]
    private void CancelarEdicion()
    {
        // Restaurar valores originales
        Email = _emailOriginal;
        Telefono = _telefonoOriginal;

        // Salir del modo edición
        ModoEdicion = false;
        OnPropertyChanged(nameof(BotonEditarTexto));
    }

    [RelayCommand]
    private async Task GuardarCambios()
    {
        try
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
            {
                MostrarMensaje("El email no es valido", true);
                return;
            }

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE usuarios
                SET correo = @correo,
                    telefono = @telefono,
                    fecha_modificacion = NOW()
                WHERE id_usuario = @idUsuario";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@correo", Email);
            command.Parameters.AddWithValue("@telefono", Telefono ?? "");
            command.Parameters.AddWithValue("@idUsuario", IdUsuario);

            var filasAfectadas = await command.ExecuteNonQueryAsync();

            if (filasAfectadas > 0)
            {
                // Actualizar valores originales con los nuevos valores guardados
                _emailOriginal = Email;
                _telefonoOriginal = Telefono ?? "";

                ModoEdicion = false;
                OnPropertyChanged(nameof(BotonEditarTexto));
                MostrarMensaje("Los cambios han sido guardados", false);
            }
            else
            {
                MostrarMensaje("No se pudieron guardar los cambios", true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error guardando datos: {ex.Message}");
            MostrarMensaje("Error al guardar los datos", true);
        }
    }

    private async void MostrarMensaje(string mensaje, bool esError)
    {
        MensajeEstado = mensaje;
        ColorMensaje = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.Parse(esError ? "#CC0000" : "#008800")
        );
        MensajeVisible = true;

        await Task.Delay(3000);
        MensajeVisible = false;
    }

    [RelayCommand]
    private void Cerrar()
    {
        _cerrarAction?.Invoke();
    }
}
