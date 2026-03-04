using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models;

public partial class PasajeroItem : ObservableObject
{
    [ObservableProperty]
    private string _tipoPasajero = "adulto"; // adulto, niño, bebé

    [ObservableProperty]
    private int _numeroPasajero = 1;

    [ObservableProperty]
    private string _tratamiento = "Sr.";

    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _apellidos = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _fechaNacimiento;

    [ObservableProperty]
    private string _nacionalidad = string.Empty;

    [ObservableProperty]
    private string _tipoDocumento = "Pasaporte";

    [ObservableProperty]
    private string _numeroDocumento = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _caducidadDocumento;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    public string TituloFormulario => $"Pasajero #{NumeroPasajero}: {TipoPasajero}";
}
