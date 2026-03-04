using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels.Admin;

public partial class CuentasBancariasAdminViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CuentaBancaria> _cuentas = new();

    [ObservableProperty]
    private bool _mostrarFormulario = false;

    [ObservableProperty]
    private bool _estaEditando = false;

    private string _cuentaEditandoId = "";

    // Campos del formulario
    [ObservableProperty]
    private string _bancoSeleccionado = "";

    [ObservableProperty]
    private string _nombreBancoOtro = "";

    // Lista de bancos disponibles
    [ObservableProperty]
    private ObservableCollection<string> _bancosDisponibles = new()
    {
        "",
        "CaixaBank",
        "BBVA",
        "Banco Santander",
        "Banco Sabadell",
        "Otro"
    };

    // Propiedad calculada para obtener el nombre final del banco
    public string NombreBanco => BancoSeleccionado == "Otro" ? NombreBancoOtro : BancoSeleccionado;

    [ObservableProperty]
    private string _titular = "";

    [ObservableProperty]
    private string _ibanCompleto = "";

    // Modulos disponibles
    [ObservableProperty]
    private bool _disponibleCompraDivisas = false;

    [ObservableProperty]
    private bool _disponiblePackAlimentos = false;

    [ObservableProperty]
    private bool _disponibleBilletesAvion = false;

    [ObservableProperty]
    private bool _disponiblePackViajes = false;

    private readonly FrontOfficeConfigService _configService;

    public CuentasBancariasAdminViewModel()
    {
        _configService = FrontOfficeConfigService.Instance;
        CargarCuentas();
    }

    private void CargarCuentas()
    {
        Cuentas.Clear();
        foreach (var cuenta in _configService.ObtenerCuentasBancarias())
        {
            Cuentas.Add(cuenta);
        }
    }

    [RelayCommand]
    private void MostrarFormularioAgregar()
    {
        LimpiarFormulario();
        EstaEditando = false;
        MostrarFormulario = true;
    }

    [RelayCommand]
    private void EditarCuenta(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;

        var cuenta = Cuentas.FirstOrDefault(c => c.Id == id);
        if (cuenta == null) return;

        // Cargar datos en el formulario
        // Verificar si el banco está en la lista predefinida
        if (BancosDisponibles.Contains(cuenta.NombreBanco))
        {
            BancoSeleccionado = cuenta.NombreBanco;
            NombreBancoOtro = "";
        }
        else
        {
            // Es un banco personalizado, seleccionar "Otro"
            BancoSeleccionado = "Otro";
            NombreBancoOtro = cuenta.NombreBanco;
        }

        Titular = cuenta.Titular;
        IbanCompleto = cuenta.IBAN;
        DisponibleCompraDivisas = cuenta.DisponibleCompraDivisas;
        DisponiblePackAlimentos = cuenta.DisponiblePackAlimentos;
        DisponibleBilletesAvion = cuenta.DisponibleBilletesAvion;
        DisponiblePackViajes = cuenta.DisponiblePackViajes;

        _cuentaEditandoId = id;
        EstaEditando = true;
        MostrarFormulario = true;
    }

    [RelayCommand]
    private void CancelarFormulario()
    {
        LimpiarFormulario();
        MostrarFormulario = false;
    }

    [RelayCommand]
    private void GuardarCuenta()
    {
        // Validar que hay datos minimos
        if (string.IsNullOrWhiteSpace(NombreBanco) || string.IsNullOrWhiteSpace(IbanCompleto))
        {
            return;
        }

        if (EstaEditando)
        {
            // Actualizar cuenta existente
            var cuenta = Cuentas.FirstOrDefault(c => c.Id == _cuentaEditandoId);
            if (cuenta != null)
            {
                cuenta.NombreBanco = NombreBanco;
                cuenta.Titular = Titular;
                cuenta.ParseIBAN(IbanCompleto);
                cuenta.DisponibleCompraDivisas = DisponibleCompraDivisas;
                cuenta.DisponiblePackAlimentos = DisponiblePackAlimentos;
                cuenta.DisponibleBilletesAvion = DisponibleBilletesAvion;
                cuenta.DisponiblePackViajes = DisponiblePackViajes;
            }
        }
        else
        {
            // Crear nueva cuenta
            var nuevaCuenta = new CuentaBancaria
            {
                NombreBanco = NombreBanco,
                Titular = Titular,
                DisponibleCompraDivisas = DisponibleCompraDivisas,
                DisponiblePackAlimentos = DisponiblePackAlimentos,
                DisponibleBilletesAvion = DisponibleBilletesAvion,
                DisponiblePackViajes = DisponiblePackViajes
            };
            nuevaCuenta.ParseIBAN(IbanCompleto);
            Cuentas.Add(nuevaCuenta);
        }

        // Guardar en BD
        _configService.GuardarCuentasBancarias(Cuentas.ToList());

        // Recargar desde BD para sincronizar IDs
        CargarCuentas();

        LimpiarFormulario();
        MostrarFormulario = false;
    }

    [RelayCommand]
    private void EliminarCuenta(string? id)
    {
        if (string.IsNullOrEmpty(id)) return;

        var cuenta = Cuentas.FirstOrDefault(c => c.Id == id);
        if (cuenta != null)
        {
            Cuentas.Remove(cuenta);
            _configService.GuardarCuentasBancarias(Cuentas.ToList());
        }
    }

    private void LimpiarFormulario()
    {
        BancoSeleccionado = "";
        NombreBancoOtro = "";
        Titular = "";
        IbanCompleto = "";
        DisponibleCompraDivisas = false;
        DisponiblePackAlimentos = false;
        DisponibleBilletesAvion = false;
        DisponiblePackViajes = false;
        _cuentaEditandoId = "";
    }
}
