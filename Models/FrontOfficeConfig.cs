using System.Collections.Generic;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo para un telefono de contacto del Centro de Ayuda
/// </summary>
public class TelefonoContacto
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Nombre { get; set; } = "";
    public string Numero { get; set; } = "";
}

/// <summary>
/// Modelo para una cuenta bancaria visible en el Front Office
/// </summary>
public class CuentaBancaria
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string NombreBanco { get; set; } = "";
    public string Titular { get; set; } = "";

    // Campos IBAN desglosados (formato: ES00 0000 0000 00 0000000000)
    public string CodigoPais { get; set; } = "ES"; // 2 letras
    public string DigitoControlPais { get; set; } = ""; // 2 digitos
    public string CodigoBanco { get; set; } = ""; // 4 digitos
    public string CodigoSucursal { get; set; } = ""; // 4 digitos
    public string DigitoControl { get; set; } = ""; // 2 digitos
    public string NumeroCuentaCliente { get; set; } = ""; // 10 digitos

    // Modulos donde estara disponible esta cuenta
    public bool DisponibleCompraDivisas { get; set; } = false;
    public bool DisponiblePackAlimentos { get; set; } = false;
    public bool DisponibleBilletesAvion { get; set; } = false;
    public bool DisponiblePackViajes { get; set; } = false;

    // IBAN completo formateado (calculado)
    public string IBAN => $"{CodigoPais}{DigitoControlPais} {CodigoBanco} {CodigoSucursal} {DigitoControl} {NumeroCuentaCliente}".Trim();

    // IBAN sin espacios para BD
    public string IBANSinEspacios => $"{CodigoPais}{DigitoControlPais}{CodigoBanco}{CodigoSucursal}{DigitoControl}{NumeroCuentaCliente}";

    // Propiedad de compatibilidad
    public string NumeroCuenta
    {
        get => IBAN;
        set => ParseIBAN(value);
    }
    public string TipoCuenta { get; set; } = "IBAN";

    // Metodo para parsear un IBAN completo a sus componentes
    public void ParseIBAN(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return;

        // Limpiar espacios
        var clean = iban.Replace(" ", "").ToUpper();
        if (clean.Length < 24) return;

        CodigoPais = clean.Substring(0, 2);
        DigitoControlPais = clean.Substring(2, 2);
        CodigoBanco = clean.Substring(4, 4);
        CodigoSucursal = clean.Substring(8, 4);
        DigitoControl = clean.Substring(12, 2);
        NumeroCuentaCliente = clean.Substring(14, 10);
    }

    // Texto de modulos disponibles
    public string ModulosDisponiblesTexto
    {
        get
        {
            var modulos = new List<string>();
            if (DisponibleCompraDivisas) modulos.Add("Compra de Divisas");
            if (DisponiblePackAlimentos) modulos.Add("Packs de Alimentos");
            if (DisponibleBilletesAvion) modulos.Add("Billetes de Avion");
            if (DisponiblePackViajes) modulos.Add("Packs de Viajes");
            return modulos.Count > 0 ? string.Join(", ", modulos) : "Ninguno";
        }
    }
}

/// <summary>
/// Configuracion del Front Office editable desde Admin
/// </summary>
public class FrontOfficeConfig
{
    public List<TelefonoContacto> Telefonos { get; set; } = new();
    public List<CuentaBancaria> CuentasBancarias { get; set; } = new();
    public string UrlManualUsuario { get; set; } = "";
}
