using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Npgsql;
using Allva.Desktop.Models;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el módulo de Billetes de Avión (Wizard de 5 pasos)
/// </summary>
public partial class FlightTicketsViewModel : BaseViewModel
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    public FlightTicketsViewModel()
    {
        Titulo = "Billetes de Avión";
        // Cargar datos de ejemplo inmediatamente para que el buscador funcione al instante
        CargarAeropuertosEjemplo();
        CargandoDatos = false;
        // Luego intentar cargar desde BD en segundo plano
        _ = IntentarCargarDesdeBDAsync();
    }

    // ============================================================
    // PROPIEDADES DE VISTA (wizard steps)
    // ============================================================

    [ObservableProperty]
    private bool _vistaBusqueda = true;

    [ObservableProperty]
    private bool _vistaCalendario = false;

    [ObservableProperty]
    private bool _vistaResultados = false;

    [ObservableProperty]
    private bool _vistaDetalle = false;

    [ObservableProperty]
    private bool _vistaExtras = false;

    [ObservableProperty]
    private bool _vistaDatos = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsPaso1Activo))]
    [NotifyPropertyChangedFor(nameof(EsPaso2Activo))]
    [NotifyPropertyChangedFor(nameof(EsPaso3Activo))]
    [NotifyPropertyChangedFor(nameof(EsPaso4Activo))]
    [NotifyPropertyChangedFor(nameof(EsPaso1Completado))]
    [NotifyPropertyChangedFor(nameof(EsPaso2Completado))]
    [NotifyPropertyChangedFor(nameof(EsPaso3Completado))]
    private int _pasoActual = 1;

    // Stepper
    public bool EsPaso1Activo => PasoActual == 1;
    public bool EsPaso2Activo => PasoActual == 2;
    public bool EsPaso3Activo => PasoActual == 3;
    public bool EsPaso4Activo => PasoActual == 4;
    public bool EsPaso1Completado => PasoActual > 1;
    public bool EsPaso2Completado => PasoActual > 2;
    public bool EsPaso3Completado => PasoActual > 3;

    // ============================================================
    // PROPIEDADES DE DATOS
    // ============================================================

    [ObservableProperty]
    private bool _cargandoDatos = true;

    [ObservableProperty]
    private bool _cargandoVuelos = false;

    [ObservableProperty]
    private ObservableCollection<AeropuertoItem> _aeropuertos = new();

    [ObservableProperty]
    private ObservableCollection<AeropuertoItem> _aeropuertosFiltradosOrigen = new();

    [ObservableProperty]
    private ObservableCollection<AeropuertoItem> _aeropuertosFiltradosDestino = new();

    [ObservableProperty]
    private ObservableCollection<VueloItem> _vuelosDisponibles = new();

    [ObservableProperty]
    private ObservableCollection<PasajeroItem> _pasajeros = new();

    [ObservableProperty]
    private ObservableCollection<PrecioCalendarioItem> _preciosCalendarioMes1 = new();

    [ObservableProperty]
    private ObservableCollection<PrecioCalendarioItem> _preciosCalendarioMes2 = new();

    // Búsqueda
    [ObservableProperty]
    private AeropuertoItem? _origenSeleccionado;

    [ObservableProperty]
    private AeropuertoItem? _destinoSeleccionado;

    [ObservableProperty]
    private string _busquedaOrigen = string.Empty;

    [ObservableProperty]
    private string _busquedaDestino = string.Empty;

    [ObservableProperty]
    private bool _mostrarDropdownOrigen = false;

    [ObservableProperty]
    private bool _mostrarDropdownDestino = false;

    [ObservableProperty]
    private int _adultos = 1;

    [ObservableProperty]
    private int _ninos = 0;

    [ObservableProperty]
    private int _bebes = 0;

    [ObservableProperty]
    private bool _mostrarSelectorPasajeros = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FechaIdaTexto))]
    private DateTimeOffset _fechaIda = new DateTimeOffset(DateTime.Today.AddDays(15));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FechaVueltaTexto))]
    private DateTimeOffset _fechaVuelta = new DateTimeOffset(DateTime.Today.AddDays(30));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoCheckSoloIda))]
    private bool _soloIda = false;

    // Meses del calendario
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloMes1))]
    [NotifyPropertyChangedFor(nameof(TituloMes2))]
    private DateTime _mesActualCalendario = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    // Selección vuelos
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneVueloIdaSeleccionado))]
    [NotifyPropertyChangedFor(nameof(ResumenVueloIda))]
    [NotifyPropertyChangedFor(nameof(PrecioTotal))]
    [NotifyPropertyChangedFor(nameof(PrecioTotalTexto))]
    [NotifyPropertyChangedFor(nameof(PrecioBilletesTexto))]
    private VueloItem? _vueloIdaSeleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneVueloVueltaSeleccionado))]
    [NotifyPropertyChangedFor(nameof(ResumenVueloVuelta))]
    [NotifyPropertyChangedFor(nameof(PrecioTotal))]
    [NotifyPropertyChangedFor(nameof(PrecioTotalTexto))]
    [NotifyPropertyChangedFor(nameof(PrecioBilletesTexto))]
    private VueloItem? _vueloVueltaSeleccionado;

    [ObservableProperty]
    private bool _seleccionandoVuelta = false;

    // Extras / Equipaje
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrecioEquipaje))]
    [NotifyPropertyChangedFor(nameof(PrecioEquipajeTexto))]
    [NotifyPropertyChangedFor(nameof(PrecioTotal))]
    [NotifyPropertyChangedFor(nameof(PrecioTotalTexto))]
    [NotifyPropertyChangedFor(nameof(EquipajeSeleccionadoTexto))]
    private int _maletasFacturadas = 0; // 0, 1 o 2

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrecioExtras))]
    [NotifyPropertyChangedFor(nameof(PrecioExtrasTexto))]
    [NotifyPropertyChangedFor(nameof(PrecioSentarseJuntosResumenTexto))]
    [NotifyPropertyChangedFor(nameof(PrecioTotal))]
    [NotifyPropertyChangedFor(nameof(PrecioTotalTexto))]
    private bool _sentarseJuntos = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrecioExtras))]
    [NotifyPropertyChangedFor(nameof(PrecioExtrasTexto))]
    [NotifyPropertyChangedFor(nameof(PrecioTotal))]
    [NotifyPropertyChangedFor(nameof(PrecioTotalTexto))]
    private bool _seguroViaje = false;

    // Validación pasajeros
    [ObservableProperty]
    private string _mensajeErrorDatos = string.Empty;

    [ObservableProperty]
    private bool _mostrarErrorDatos = false;

    // ============================================================
    // PROPIEDADES COMPUTADAS
    // ============================================================

    public string TextoPasajeros
    {
        get
        {
            var total = Adultos + Ninos + Bebes;
            return total == 1 ? "1 Persona" : $"{total} Personas";
        }
    }

    public string FechaIdaTexto => FechaIda.ToString("dd/MM/yyyy");
    public string FechaVueltaTexto => FechaVuelta.ToString("dd/MM/yyyy");
    public string TextoCheckSoloIda => SoloIda ? "✓ Solo ida" : "Solo ida";

    public string TituloMes1 => MesActualCalendario.ToString("MMMM yyyy");
    public string TituloMes2 => MesActualCalendario.AddMonths(1).ToString("MMMM yyyy");

    public string TituloRutaBusqueda => OrigenSeleccionado != null && DestinoSeleccionado != null
        ? $"VUELO {OrigenSeleccionado.Ciudad} - {DestinoSeleccionado.Ciudad}"
        : "VUELO";

    public string TituloResultadosIda => OrigenSeleccionado != null && DestinoSeleccionado != null
        ? $"{OrigenSeleccionado.Ciudad} - {DestinoSeleccionado.Ciudad}"
        : "";

    public bool TieneVueloIdaSeleccionado => VueloIdaSeleccionado != null;
    public bool TieneVueloVueltaSeleccionado => VueloVueltaSeleccionado != null;

    public string ResumenPasajeros => $"{Adultos + Ninos + Bebes} personas";

    public string ResumenVueloIda => VueloIdaSeleccionado != null
        ? $"{FechaIda:dd/MM/yyyy} · {VueloIdaSeleccionado.HoraSalida} - {VueloIdaSeleccionado.HoraLlegada}\n{VueloIdaSeleccionado.OrigenCodigo} - {VueloIdaSeleccionado.DestinoCodigo}"
        : "";

    public string ResumenVueloVuelta => VueloVueltaSeleccionado != null
        ? $"{FechaVuelta:dd/MM/yyyy} · {VueloVueltaSeleccionado.HoraSalida} - {VueloVueltaSeleccionado.HoraLlegada}\n{VueloVueltaSeleccionado.OrigenCodigo} - {VueloVueltaSeleccionado.DestinoCodigo}"
        : "";

    public int TotalPasajeros => Adultos + Ninos + Bebes;

    public decimal PrecioBilletes
    {
        get
        {
            decimal total = 0;
            if (VueloIdaSeleccionado != null)
                total += VueloIdaSeleccionado.PrecioSeleccionado * TotalPasajeros;
            if (VueloVueltaSeleccionado != null)
                total += VueloVueltaSeleccionado.PrecioSeleccionado * TotalPasajeros;
            return total;
        }
    }

    // Precios equipaje
    private const decimal PrecioPorMaleta = 35m; // por maleta por trayecto por pasajero
    private const decimal PrecioSentarseJuntosPorTrayecto = 9.95m;
    private const decimal PrecioSeguroPorPersona = 19.90m;

    public int NumTrayectos => SoloIda ? 1 : 2;

    public decimal PrecioEquipaje => MaletasFacturadas * PrecioPorMaleta * TotalPasajeros * NumTrayectos;

    public decimal PrecioExtras
    {
        get
        {
            decimal total = 0;
            if (SentarseJuntos)
                total += PrecioSentarseJuntosPorTrayecto * TotalPasajeros * NumTrayectos;
            if (SeguroViaje)
                total += PrecioSeguroPorPersona * TotalPasajeros;
            return total;
        }
    }

    public decimal PrecioTotal => PrecioBilletes + PrecioEquipaje + PrecioExtras;
    public string PrecioTotalTexto => $"{PrecioTotal:N2}€";
    public string PrecioBilletesTexto => $"{PrecioBilletes:N2}€";

    public string PrecioEquipajeTexto => MaletasFacturadas == 0
        ? "0€"
        : $"{PrecioEquipaje:N2}€";

    public string PrecioExtrasTexto
    {
        get
        {
            var partes = new System.Collections.Generic.List<string>();
            if (SentarseJuntos) partes.Add("asientos");
            if (SeguroViaje) partes.Add("seguro");
            return partes.Count == 0 ? "0€" : $"{PrecioExtras:N2}€";
        }
    }

    // Textos para vista de extras
    public string PrecioUnaMaletaTexto => $"{PrecioPorMaleta * TotalPasajeros * NumTrayectos:N2}€";
    public string PrecioDosMaletasTexto => $"{2 * PrecioPorMaleta * TotalPasajeros * NumTrayectos:N2}€";
    public string PrecioSentarseJuntosTexto => $"{PrecioSentarseJuntosPorTrayecto * TotalPasajeros * NumTrayectos:N2}€";
    public string PrecioSeguroTexto => $"{PrecioSeguroPorPersona * TotalPasajeros:N2}€";
    public string PrecioSentarseJuntosResumenTexto => SentarseJuntos ? $"{PrecioSentarseJuntosPorTrayecto * TotalPasajeros * NumTrayectos:N2}€" : "0€";

    public string EquipajeSeleccionadoTexto => MaletasFacturadas switch
    {
        0 => "&#x2713; Solo equipaje de mano (incluido)",
        1 => $"&#x2713; 1 maleta facturada por pasajero - {PrecioEquipaje:N2}€",
        2 => $"&#x2713; 2 maletas facturadas por pasajero - {PrecioEquipaje:N2}€",
        _ => ""
    };

    // ============================================================
    // INICIALIZACIÓN Y CARGA DE DATOS
    // ============================================================

    private async Task IntentarCargarDesdeBDAsync()
    {
        try
        {
            await CargarAeropuertosAsync();
        }
        catch
        {
            // Si falla la BD, los datos de ejemplo ya estan cargados
        }
    }

    private async Task CargarAeropuertosAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, codigo, ciudad, pais, nombre_aeropuerto FROM aeropuertos ORDER BY pais, ciudad",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        var lista = new ObservableCollection<AeropuertoItem>();

        while (await reader.ReadAsync())
        {
            lista.Add(new AeropuertoItem
            {
                Id = reader.GetInt32(0),
                Codigo = reader.GetString(1),
                Ciudad = reader.GetString(2),
                Pais = reader.GetString(3),
                NombreAeropuerto = reader.GetString(4)
            });
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Aeropuertos = lista;
            AeropuertosFiltradosOrigen = new ObservableCollection<AeropuertoItem>(lista);
            AeropuertosFiltradosDestino = new ObservableCollection<AeropuertoItem>(lista);
        });
    }

    private void CargarAeropuertosEjemplo()
    {
        var lista = new ObservableCollection<AeropuertoItem>
        {
            new() { Codigo = "BCN", Ciudad = "Barcelona", Pais = "España", NombreAeropuerto = "Barcelona-El Prat" },
            new() { Codigo = "MAD", Ciudad = "Madrid", Pais = "España", NombreAeropuerto = "Madrid Adolfo Suárez-Barajas" },
            new() { Codigo = "SDQ", Ciudad = "Santo Domingo", Pais = "República Dominicana", NombreAeropuerto = "Las Américas" },
            new() { Codigo = "CDG", Ciudad = "París", Pais = "Francia", NombreAeropuerto = "Charles de Gaulle" },
            new() { Codigo = "LHR", Ciudad = "Londres", Pais = "Reino Unido", NombreAeropuerto = "London Heathrow" },
            new() { Codigo = "FCO", Ciudad = "Roma", Pais = "Italia", NombreAeropuerto = "Leonardo da Vinci-Fiumicino" },
        };
        Aeropuertos = lista;
        AeropuertosFiltradosOrigen = new ObservableCollection<AeropuertoItem>(lista);
        AeropuertosFiltradosDestino = new ObservableCollection<AeropuertoItem>(lista);
    }

    public async Task CargarPreciosCalendarioAsync()
    {
        if (OrigenSeleccionado == null || DestinoSeleccionado == null) return;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT fecha, precio_minimo FROM precios_calendario
                WHERE origen_codigo = @origen AND destino_codigo = @destino
                  AND fecha >= @fechaInicio AND fecha < @fechaFin
                ORDER BY fecha", conn);

            var mesInicio = MesActualCalendario;
            var mesFin = MesActualCalendario.AddMonths(2);

            cmd.Parameters.AddWithValue("@origen", OrigenSeleccionado.Codigo);
            cmd.Parameters.AddWithValue("@destino", DestinoSeleccionado.Codigo);
            cmd.Parameters.AddWithValue("@fechaInicio", mesInicio);
            cmd.Parameters.AddWithValue("@fechaFin", mesFin);

            await using var reader = await cmd.ExecuteReaderAsync();
            var precios1 = new ObservableCollection<PrecioCalendarioItem>();
            var precios2 = new ObservableCollection<PrecioCalendarioItem>();

            while (await reader.ReadAsync())
            {
                var item = new PrecioCalendarioItem
                {
                    Fecha = reader.GetDateTime(0),
                    PrecioMinimo = reader.GetDecimal(1)
                };

                if (item.Fecha.Month == mesInicio.Month && item.Fecha.Year == mesInicio.Year)
                    precios1.Add(item);
                else
                    precios2.Add(item);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PreciosCalendarioMes1 = precios1;
                PreciosCalendarioMes2 = precios2;
            });
        }
        catch { /* Sin precios calendario, generar datos de ejemplo */ }

        // Si no hay datos de BD, generar calendario de ejemplo
        if (PreciosCalendarioMes1.Count == 0 && PreciosCalendarioMes2.Count == 0)
        {
            GenerarCalendarioEjemplo();
        }
    }

    private void GenerarCalendarioEjemplo()
    {
        var rng = new Random(42);
        var mes1 = MesActualCalendario;
        var mes2 = MesActualCalendario.AddMonths(1);

        var precios1 = new ObservableCollection<PrecioCalendarioItem>();
        var precios2 = new ObservableCollection<PrecioCalendarioItem>();

        // Agregar dias vacios para alinear con el dia de la semana
        var primerDiaSemana1 = ((int)mes1.DayOfWeek + 6) % 7; // lun=0
        for (int i = 0; i < primerDiaSemana1; i++)
            precios1.Add(new PrecioCalendarioItem { Fecha = DateTime.MinValue, PrecioMinimo = 0 });

        for (int d = 1; d <= DateTime.DaysInMonth(mes1.Year, mes1.Month); d++)
        {
            precios1.Add(new PrecioCalendarioItem
            {
                Fecha = new DateTime(mes1.Year, mes1.Month, d),
                PrecioMinimo = rng.Next(45, 320)
            });
        }

        var primerDiaSemana2 = ((int)mes2.DayOfWeek + 6) % 7;
        for (int i = 0; i < primerDiaSemana2; i++)
            precios2.Add(new PrecioCalendarioItem { Fecha = DateTime.MinValue, PrecioMinimo = 0 });

        for (int d = 1; d <= DateTime.DaysInMonth(mes2.Year, mes2.Month); d++)
        {
            precios2.Add(new PrecioCalendarioItem
            {
                Fecha = new DateTime(mes2.Year, mes2.Month, d),
                PrecioMinimo = rng.Next(45, 320)
            });
        }

        PreciosCalendarioMes1 = precios1;
        PreciosCalendarioMes2 = precios2;
    }

    public async Task BuscarVuelosAsync()
    {
        if (OrigenSeleccionado == null || DestinoSeleccionado == null) return;

        CargandoVuelos = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT v.id, v.origen_codigo, v.destino_codigo, v.aerolinea, v.numero_vuelo,
                       v.hora_salida, v.hora_llegada, v.duracion, v.escalas, v.ciudad_escala,
                       v.tiempo_conexion, v.terminal_origen, v.terminal_destino, v.avion,
                       v.precio_turista, v.precio_turista_premium, v.precio_business,
                       v.disponible_turista, v.disponible_premium, v.disponible_business,
                       v.equipaje_incluido,
                       ao.ciudad as origen_ciudad, ao.nombre_aeropuerto as origen_aeropuerto,
                       ad.ciudad as destino_ciudad, ad.nombre_aeropuerto as destino_aeropuerto
                FROM vuelos v
                JOIN aeropuertos ao ON v.origen_codigo = ao.codigo
                JOIN aeropuertos ad ON v.destino_codigo = ad.codigo
                WHERE v.origen_codigo = @origen AND v.destino_codigo = @destino
                  AND v.estado = 'Activo'
                ORDER BY v.hora_salida", conn);

            cmd.Parameters.AddWithValue("@origen", OrigenSeleccionado.Codigo);
            cmd.Parameters.AddWithValue("@destino", DestinoSeleccionado.Codigo);

            await using var reader = await cmd.ExecuteReaderAsync();
            var lista = new ObservableCollection<VueloItem>();

            while (await reader.ReadAsync())
            {
                lista.Add(new VueloItem
                {
                    Id = reader.GetInt32(0),
                    OrigenCodigo = reader.GetString(1),
                    DestinoCodigo = reader.GetString(2),
                    Aerolinea = reader.GetString(3),
                    NumeroVuelo = reader.GetString(4),
                    HoraSalida = reader.GetString(5),
                    HoraLlegada = reader.GetString(6),
                    Duracion = reader.GetString(7),
                    Escalas = reader.GetInt32(8),
                    CiudadEscala = reader.IsDBNull(9) ? null : reader.GetString(9),
                    TiempoConexion = reader.IsDBNull(10) ? null : reader.GetString(10),
                    TerminalOrigen = reader.IsDBNull(11) ? null : reader.GetString(11),
                    TerminalDestino = reader.IsDBNull(12) ? null : reader.GetString(12),
                    Avion = reader.IsDBNull(13) ? null : reader.GetString(13),
                    PrecioTurista = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                    PrecioTuristaPremium = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                    PrecioBusiness = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                    DisponibleTurista = reader.GetBoolean(17),
                    DisponiblePremium = reader.GetBoolean(18),
                    DisponibleBusiness = reader.GetBoolean(19),
                    EquipajeIncluido = reader.IsDBNull(20) ? "23kg" : reader.GetString(20),
                    OrigenCiudad = reader.GetString(21),
                    OrigenAeropuerto = reader.GetString(22),
                    DestinoCiudad = reader.GetString(23),
                    DestinoAeropuerto = reader.GetString(24)
                });
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                VuelosDisponibles = lista;
            });

            // Si la BD no tiene vuelos para esta ruta, usar ejemplos
            if (VuelosDisponibles.Count == 0)
                CargarVuelosEjemplo();
        }
        catch
        {
            CargarVuelosEjemplo();
        }
        finally
        {
            CargandoVuelos = false;
        }
    }

    private void CargarVuelosEjemplo()
    {
        var origen = OrigenSeleccionado?.Codigo ?? "BCN";
        var destino = DestinoSeleccionado?.Codigo ?? "SDQ";
        var origenCiudad = OrigenSeleccionado?.Ciudad ?? "Barcelona";
        var destinoCiudad = DestinoSeleccionado?.Ciudad ?? "Santo Domingo";
        var origenAeropuerto = OrigenSeleccionado?.NombreAeropuerto ?? "Barcelona-El Prat";
        var destinoAeropuerto = DestinoSeleccionado?.NombreAeropuerto ?? "Las Américas";

        VuelosDisponibles = new ObservableCollection<VueloItem>
        {
            new()
            {
                Id = 1, OrigenCodigo = origen, DestinoCodigo = destino, Aerolinea = "Iberia", NumeroVuelo = "IB428",
                HoraSalida = "08:00", HoraLlegada = "12:35", Duracion = "12h 35min", Escalas = 1,
                CiudadEscala = "Madrid", TiempoConexion = "2h 20min",
                TerminalOrigen = "Terminal 1", TerminalDestino = "Terminal 4", Avion = "Airbus A320",
                PrecioTurista = 315m, PrecioTuristaPremium = 497m, PrecioBusiness = 890m,
                DisponibleTurista = true, DisponiblePremium = true, DisponibleBusiness = true,
                OrigenCiudad = origenCiudad, DestinoCiudad = destinoCiudad,
                OrigenAeropuerto = origenAeropuerto, DestinoAeropuerto = destinoAeropuerto
            },
            new()
            {
                Id = 2, OrigenCodigo = origen, DestinoCodigo = destino, Aerolinea = "Iberia", NumeroVuelo = "IB502",
                HoraSalida = "10:30", HoraLlegada = "15:05", Duracion = "12h 35min", Escalas = 1,
                CiudadEscala = "Madrid", TiempoConexion = "1h 45min",
                TerminalOrigen = "Terminal 1", TerminalDestino = "Terminal 4", Avion = "Airbus A321",
                PrecioTurista = 285m, PrecioTuristaPremium = 460m, PrecioBusiness = null,
                DisponibleTurista = true, DisponiblePremium = true, DisponibleBusiness = false,
                OrigenCiudad = origenCiudad, DestinoCiudad = destinoCiudad,
                OrigenAeropuerto = origenAeropuerto, DestinoAeropuerto = destinoAeropuerto
            },
            new()
            {
                Id = 3, OrigenCodigo = origen, DestinoCodigo = destino, Aerolinea = "Air Europa", NumeroVuelo = "UX1024",
                HoraSalida = "14:00", HoraLlegada = "18:50", Duracion = "10h 50min", Escalas = 0,
                CiudadEscala = null, TiempoConexion = null,
                TerminalOrigen = "Terminal 1", TerminalDestino = "Terminal 2", Avion = "Boeing 787 Dreamliner",
                PrecioTurista = 395m, PrecioTuristaPremium = 580m, PrecioBusiness = 1120m,
                DisponibleTurista = true, DisponiblePremium = true, DisponibleBusiness = true,
                OrigenCiudad = origenCiudad, DestinoCiudad = destinoCiudad,
                OrigenAeropuerto = origenAeropuerto, DestinoAeropuerto = destinoAeropuerto,
                EquipajeIncluido = "2x23kg"
            },
            new()
            {
                Id = 4, OrigenCodigo = origen, DestinoCodigo = destino, Aerolinea = "Vueling", NumeroVuelo = "VY3210",
                HoraSalida = "06:15", HoraLlegada = "11:40", Duracion = "13h 25min", Escalas = 1,
                CiudadEscala = "París", TiempoConexion = "3h 00min",
                TerminalOrigen = "Terminal 1", TerminalDestino = "Terminal 3", Avion = "Airbus A320neo",
                PrecioTurista = 245m, PrecioTuristaPremium = null, PrecioBusiness = null,
                DisponibleTurista = true, DisponiblePremium = false, DisponibleBusiness = false,
                OrigenCiudad = origenCiudad, DestinoCiudad = destinoCiudad,
                OrigenAeropuerto = origenAeropuerto, DestinoAeropuerto = destinoAeropuerto
            },
            new()
            {
                Id = 5, OrigenCodigo = origen, DestinoCodigo = destino, Aerolinea = "Air France", NumeroVuelo = "AF1480",
                HoraSalida = "17:30", HoraLlegada = "22:15", Duracion = "11h 45min", Escalas = 1,
                CiudadEscala = "París", TiempoConexion = "2h 00min",
                TerminalOrigen = "Terminal 1", TerminalDestino = "Terminal E", Avion = "Boeing 777-300ER",
                PrecioTurista = 365m, PrecioTuristaPremium = 520m, PrecioBusiness = 980m,
                DisponibleTurista = true, DisponiblePremium = true, DisponibleBusiness = true,
                OrigenCiudad = origenCiudad, DestinoCiudad = destinoCiudad,
                OrigenAeropuerto = origenAeropuerto, DestinoAeropuerto = destinoAeropuerto,
                EquipajeIncluido = "2x23kg"
            }
        };
    }

    // ============================================================
    // NAVEGACIÓN ENTRE PASOS
    // ============================================================

    private void OcultarTodasLasVistas()
    {
        VistaBusqueda = false;
        VistaCalendario = false;
        VistaResultados = false;
        VistaDetalle = false;
        VistaExtras = false;
        VistaDatos = false;
    }

    [RelayCommand]
    private async Task IrACalendarioAsync()
    {
        if (OrigenSeleccionado == null || DestinoSeleccionado == null) return;

        OcultarTodasLasVistas();
        VistaCalendario = true;
        PasoActual = 1;
        await CargarPreciosCalendarioAsync();
    }

    [RelayCommand]
    private async Task IrAResultadosAsync()
    {
        OcultarTodasLasVistas();
        VistaResultados = true;
        SeleccionandoVuelta = false;
        PasoActual = 1;
        await BuscarVuelosAsync();
    }

    [RelayCommand]
    private void IrADetalle()
    {
        if (VueloIdaSeleccionado == null) return;
        OcultarTodasLasVistas();
        VistaDetalle = true;
        PasoActual = 1;
    }

    [RelayCommand]
    private void IrAExtras()
    {
        OcultarTodasLasVistas();
        VistaExtras = true;
        PasoActual = 2;
    }

    [RelayCommand]
    private void IrADatos()
    {
        OcultarTodasLasVistas();
        VistaDatos = true;
        PasoActual = 3;
        GenerarFormularioPasajeros();
    }

    [RelayCommand]
    private void VolverABusqueda()
    {
        OcultarTodasLasVistas();
        VistaBusqueda = true;
        PasoActual = 1;
    }

    [RelayCommand]
    private void VolverACalendario()
    {
        OcultarTodasLasVistas();
        VistaCalendario = true;
        PasoActual = 1;
    }

    [RelayCommand]
    private void VolverAResultados()
    {
        OcultarTodasLasVistas();
        VistaResultados = true;
        PasoActual = 1;
    }

    [RelayCommand]
    private void VolverADetalle()
    {
        OcultarTodasLasVistas();
        VistaDetalle = true;
        PasoActual = 1;
    }

    [RelayCommand]
    private void VolverAExtras()
    {
        OcultarTodasLasVistas();
        VistaExtras = true;
        PasoActual = 2;
    }

    [RelayCommand]
    private void ContinuarAPagos()
    {
        // Validar datos de pasajeros
        var errores = new System.Collections.Generic.List<string>();

        foreach (var p in Pasajeros)
        {
            string prefijo = $"Pasajero #{p.NumeroPasajero}";
            if (string.IsNullOrWhiteSpace(p.Nombre))
                errores.Add($"{prefijo}: Falta el nombre");
            if (string.IsNullOrWhiteSpace(p.Apellidos))
                errores.Add($"{prefijo}: Faltan los apellidos");
            if (p.FechaNacimiento == null)
                errores.Add($"{prefijo}: Falta la fecha de nacimiento");
            if (string.IsNullOrWhiteSpace(p.Nacionalidad))
                errores.Add($"{prefijo}: Falta la nacionalidad");
            if (string.IsNullOrWhiteSpace(p.NumeroDocumento))
                errores.Add($"{prefijo}: Falta el número de documento");
            if (p.CaducidadDocumento == null)
                errores.Add($"{prefijo}: Falta la caducidad del documento");
            if (string.IsNullOrWhiteSpace(p.Telefono))
                errores.Add($"{prefijo}: Falta el teléfono");
            if (string.IsNullOrWhiteSpace(p.Email))
                errores.Add($"{prefijo}: Falta el e-mail");
        }

        if (errores.Count > 0)
        {
            MensajeErrorDatos = "Completa todos los campos obligatorios:\n" + string.Join("\n", errores.Take(5));
            if (errores.Count > 5)
                MensajeErrorDatos += $"\n... y {errores.Count - 5} errores más";
            MostrarErrorDatos = true;
            return;
        }

        MostrarErrorDatos = false;
        MensajeErrorDatos = string.Empty;

        // TODO: Navegar a vista de pagos cuando esté implementada
        MensajeErrorDatos = "¡Datos correctos! La vista de pagos se implementará próximamente.";
        MostrarErrorDatos = true;
    }

    // ============================================================
    // LÓGICA DE BÚSQUEDA Y SELECCIÓN
    // ============================================================

    private bool _ignorarCambioBusqueda = false;

    partial void OnBusquedaOrigenChanged(string value)
    {
        if (_ignorarCambioBusqueda) return;

        if (string.IsNullOrWhiteSpace(value))
        {
            AeropuertosFiltradosOrigen = new ObservableCollection<AeropuertoItem>(Aeropuertos);
            MostrarDropdownOrigen = false;
            return;
        }

        var filtrados = Aeropuertos
            .Where(a => a.TextoCompleto.Contains(value, StringComparison.OrdinalIgnoreCase)
                     || a.Codigo.Contains(value, StringComparison.OrdinalIgnoreCase))
            .ToList();

        AeropuertosFiltradosOrigen = new ObservableCollection<AeropuertoItem>(filtrados);
        MostrarDropdownOrigen = filtrados.Count > 0;
        System.Diagnostics.Debug.WriteLine($"[VUELOS] Origen filtro '{value}': {filtrados.Count} resultados, Popup={MostrarDropdownOrigen}");
    }

    partial void OnBusquedaDestinoChanged(string value)
    {
        if (_ignorarCambioBusqueda) return;

        if (string.IsNullOrWhiteSpace(value))
        {
            AeropuertosFiltradosDestino = new ObservableCollection<AeropuertoItem>(Aeropuertos);
            MostrarDropdownDestino = false;
            return;
        }

        var filtrados = Aeropuertos
            .Where(a => a.TextoCompleto.Contains(value, StringComparison.OrdinalIgnoreCase)
                     || a.Codigo.Contains(value, StringComparison.OrdinalIgnoreCase))
            .ToList();

        AeropuertosFiltradosDestino = new ObservableCollection<AeropuertoItem>(filtrados);
        MostrarDropdownDestino = filtrados.Count > 0;
        System.Diagnostics.Debug.WriteLine($"[VUELOS] Destino filtro '{value}': {filtrados.Count} resultados, Popup={MostrarDropdownDestino}");
    }

    [RelayCommand]
    private void SeleccionarOrigen(AeropuertoItem aeropuerto)
    {
        _ignorarCambioBusqueda = true;
        OrigenSeleccionado = aeropuerto;
        BusquedaOrigen = aeropuerto.TextoCompleto;
        MostrarDropdownOrigen = false;
        _ignorarCambioBusqueda = false;
    }

    [RelayCommand]
    private void SeleccionarDestino(AeropuertoItem aeropuerto)
    {
        _ignorarCambioBusqueda = true;
        DestinoSeleccionado = aeropuerto;
        BusquedaDestino = aeropuerto.TextoCompleto;
        MostrarDropdownDestino = false;
        _ignorarCambioBusqueda = false;
    }

    [RelayCommand]
    private void IntercambiarOrigenDestino()
    {
        (OrigenSeleccionado, DestinoSeleccionado) = (DestinoSeleccionado, OrigenSeleccionado);
        (BusquedaOrigen, BusquedaDestino) = (BusquedaDestino, BusquedaOrigen);
    }

    [RelayCommand]
    private void ToggleSelectorPasajeros()
    {
        MostrarSelectorPasajeros = !MostrarSelectorPasajeros;
    }

    [RelayCommand]
    private void IncrementarAdultos()
    {
        if (Adultos < 9) { Adultos++; OnPropertyChanged(nameof(TextoPasajeros)); }
    }

    [RelayCommand]
    private void DecrementarAdultos()
    {
        if (Adultos > 1) { Adultos--; OnPropertyChanged(nameof(TextoPasajeros)); }
    }

    [RelayCommand]
    private void IncrementarNinos()
    {
        if (Ninos < 8) { Ninos++; OnPropertyChanged(nameof(TextoPasajeros)); }
    }

    [RelayCommand]
    private void DecrementarNinos()
    {
        if (Ninos > 0) { Ninos--; OnPropertyChanged(nameof(TextoPasajeros)); }
    }

    [RelayCommand]
    private void IncrementarBebes()
    {
        if (Bebes < Adultos) { Bebes++; OnPropertyChanged(nameof(TextoPasajeros)); }
    }

    [RelayCommand]
    private void DecrementarBebes()
    {
        if (Bebes > 0) { Bebes--; OnPropertyChanged(nameof(TextoPasajeros)); }
    }

    [RelayCommand]
    private void SeleccionarVuelo(VueloItem vuelo)
    {
        SeleccionarVueloConClase(vuelo, "turista");
    }

    private void SeleccionarVueloConClase(VueloItem vuelo, string clase)
    {
        // Deseleccionar todos
        foreach (var v in VuelosDisponibles)
        {
            v.EstaSeleccionado = false;
            v.ClaseSeleccionada = "";
        }

        vuelo.EstaSeleccionado = true;
        vuelo.ClaseSeleccionada = clase;

        if (!SeleccionandoVuelta)
            VueloIdaSeleccionado = vuelo;
        else
            VueloVueltaSeleccionado = vuelo;

        OnPropertyChanged(nameof(TieneVueloIdaSeleccionado));
        OnPropertyChanged(nameof(PrecioTotal));
        OnPropertyChanged(nameof(PrecioTotalTexto));
        OnPropertyChanged(nameof(PrecioBilletesTexto));
    }

    [RelayCommand]
    private void SeleccionarTurista(VueloItem vuelo)
    {
        SeleccionarVueloConClase(vuelo, "turista");
    }

    [RelayCommand]
    private void SeleccionarPremium(VueloItem vuelo)
    {
        SeleccionarVueloConClase(vuelo, "premium");
    }

    [RelayCommand]
    private void SeleccionarBusiness(VueloItem vuelo)
    {
        SeleccionarVueloConClase(vuelo, "business");
    }

    [RelayCommand]
    private void SeleccionarEquipajeFacturado(string cantidad)
    {
        if (int.TryParse(cantidad, out int num))
        {
            MaletasFacturadas = num;
        }
    }

    [RelayCommand]
    private void SeleccionarClase(string clase)
    {
        if (VueloIdaSeleccionado != null)
            VueloIdaSeleccionado.ClaseSeleccionada = clase;

        OnPropertyChanged(nameof(PrecioTotal));
        OnPropertyChanged(nameof(PrecioTotalTexto));
        OnPropertyChanged(nameof(PrecioBilletesTexto));
    }

    // Calendario - seleccionar fecha
    private bool _seleccionandoFechaVuelta = false;

    [RelayCommand]
    private void SeleccionarFechaCalendario(PrecioCalendarioItem item)
    {
        if (item == null || item.EsVacio) return;

        if (!_seleccionandoFechaVuelta)
        {
            FechaIda = new DateTimeOffset(item.Fecha);
            _seleccionandoFechaVuelta = true;
        }
        else
        {
            if (item.Fecha < FechaIda.DateTime)
            {
                // Si selecciona una fecha anterior, es nueva fecha de ida
                FechaIda = new DateTimeOffset(item.Fecha);
            }
            else
            {
                FechaVuelta = new DateTimeOffset(item.Fecha);
                _seleccionandoFechaVuelta = false;
            }
        }
    }

    // Calendario - navegar meses
    [RelayCommand]
    private async Task MesAnteriorAsync()
    {
        if (MesActualCalendario > DateTime.Today.AddMonths(-1))
        {
            MesActualCalendario = MesActualCalendario.AddMonths(-1);
            await CargarPreciosCalendarioAsync();
        }
    }

    [RelayCommand]
    private async Task MesSiguienteAsync()
    {
        MesActualCalendario = MesActualCalendario.AddMonths(1);
        await CargarPreciosCalendarioAsync();
    }

    // ============================================================
    // FORMULARIO DE PASAJEROS
    // ============================================================

    private void GenerarFormularioPasajeros()
    {
        Pasajeros.Clear();
        int num = 1;

        for (int i = 0; i < Adultos; i++)
            Pasajeros.Add(new PasajeroItem { TipoPasajero = "adulto", NumeroPasajero = num++ });

        for (int i = 0; i < Ninos; i++)
            Pasajeros.Add(new PasajeroItem { TipoPasajero = "niño", NumeroPasajero = num++ });

        for (int i = 0; i < Bebes; i++)
            Pasajeros.Add(new PasajeroItem { TipoPasajero = "bebé", NumeroPasajero = num++ });
    }
}
