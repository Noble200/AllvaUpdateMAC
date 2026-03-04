using System.Collections.ObjectModel;

namespace Allva.Desktop.Models;

/// <summary>
/// Wrapper de CuentaBancaria que incluye el nombre del modulo al que pertenece
/// para mostrar en la vista del Front Office
/// </summary>
public class CuentaBancariaConModulo
{
    public CuentaBancaria Cuenta { get; set; } = new();
    public string NombreModulo { get; set; } = "";
    public string ColorModulo { get; set; } = "#0b5394";
    public string ColorFondoModulo { get; set; } = "#EBF5FF";

    // Propiedades de acceso directo
    public string Id => Cuenta.Id;
    public string NombreBanco => Cuenta.NombreBanco;
    public string Titular => Cuenta.Titular;
    public string IBAN => Cuenta.IBAN;
    public string IBANSinEspacios => Cuenta.IBANSinEspacios;
    public string CodigoPais => Cuenta.CodigoPais;
    public string DigitoControlPais => Cuenta.DigitoControlPais;
    public string CodigoBanco => Cuenta.CodigoBanco;
    public string CodigoSucursal => Cuenta.CodigoSucursal;
    public string DigitoControl => Cuenta.DigitoControl;
    public string NumeroCuentaCliente => Cuenta.NumeroCuentaCliente;

    /// <summary>
    /// Texto completo para copiar todos los datos
    /// </summary>
    public string TextoCompleto => $"Banco: {NombreBanco}\nTitular: {Titular}\nIBAN: {IBANSinEspacios}";
}

/// <summary>
/// Grupo de cuentas bancarias agrupadas por modulo
/// para mostrar con titulo de seccion en el Front Office
/// </summary>
public class GrupoCuentasBancarias
{
    public string NombreGrupo { get; set; } = "";
    public string NombreModulo { get; set; } = "";
    public string DescripcionGrupo { get; set; } = "";
    public string ColorTitulo { get; set; } = "#0b5394";
    public string ColorFondo { get; set; } = "#EBF5FF";
    public string IconoPath { get; set; } = "";
    public ObservableCollection<CuentaBancariaConModulo> Cuentas { get; set; } = new();
}
