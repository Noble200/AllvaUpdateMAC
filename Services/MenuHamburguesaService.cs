using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Allva.Desktop.Models;
using Allva.Desktop.Views.Admin.MenuHamburguesa;
using Allva.Desktop.Views.Admin;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio que gestiona los paneles disponibles en el menu hamburguesa
    /// Centraliza el registro de todos los modulos de configuracion
    /// </summary>
    public class MenuHamburguesaService
    {
        private static MenuHamburguesaService? _instance;
        private readonly List<MenuHamburguesaItem> _items;

        public static MenuHamburguesaService Instance => _instance ??= new MenuHamburguesaService();

        private MenuHamburguesaService()
        {
            _items = new List<MenuHamburguesaItem>();
            RegistrarPanelesDisponibles();
        }

        /// <summary>
        /// Registra todos los paneles disponibles en la carpeta MenuHamburguesa
        /// IMPORTANTE: Agregar aqui cada nuevo panel que se cree
        /// </summary>
        private void RegistrarPanelesDisponibles()
        {
            // Panel de Facturas
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "facturas",
                Titulo = "Facturas",
                Descripcion = "Gestión de facturas",
                Orden = 1,
                Habilitado = true,
                TipoVista = typeof(FacturasView)
            });

            // Panel de Automatización Correo
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "automatizacion_correo",
                Titulo = "Automatización Correo",
                Descripcion = "Configuración de correos automáticos",
                Orden = 3,
                Habilitado = true,
                TipoVista = typeof(AutomatizacionCorreoView)
            });

            // Panel de Usuarios Allva (Administradores)
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "usuarios_allva",
                Titulo = "Usuarios Allva",
                Descripcion = "Gestión de administradores Allva",
                Orden = 4,
                Habilitado = true,
                TipoVista = typeof(UsuariosAllvaView)
            });

            // Panel de Alta Inscripción
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "alta_inscripcion",
                Titulo = "Alta Inscripción",
                Descripcion = "Alta de nuevas inscripciones",
                Orden = 5,
                Habilitado = true,
                TipoVista = typeof(AltaInscripcionView)
            });

            // Panel de Contraseña Admin Front
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "contrasena_admin_front",
                Titulo = "Contraseña Admin. Front",
                Descripcion = "Gestión de contraseñas de administradores FrontOffice",
                Orden = 6,
                Habilitado = true,
                TipoVista = typeof(ContrasenaAdminFrontView)
            });

            // Panel de Movimientos Usuarios
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "movimientos_usuarios",
                Titulo = "Movimientos Usuarios",
                Descripcion = "Registro de movimientos de usuarios",
                Orden = 7,
                Habilitado = true,
                TipoVista = typeof(MovimientosUsuariosView)
            });

            // Panel de APIs
            RegistrarItem(new MenuHamburguesaItem
            {
                Id = "apis",
                Titulo = "APIs",
                Descripcion = "Configuración de APIs externas",
                Orden = 8,
                Habilitado = true,
                TipoVista = typeof(APIsConfigView)
            });
        }

        public void RegistrarItem(MenuHamburguesaItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
                throw new ArgumentException("El Id del item no puede estar vacio");

            if (_items.Any(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
            {
                var existente = _items.First(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
                var index = _items.IndexOf(existente);
                _items[index] = item;
            }
            else
            {
                _items.Add(item);
            }
        }

        public List<MenuHamburguesaItem> ObtenerItemsHabilitados()
        {
            return _items
                .Where(x => x.Habilitado)
                .OrderBy(x => x.Orden)
                .ToList();
        }

        public List<MenuHamburguesaItem> ObtenerTodosLosItems()
        {
            return _items.OrderBy(x => x.Orden).ToList();
        }

        public MenuHamburguesaItem? ObtenerItemPorId(string id)
        {
            return _items.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public UserControl? CrearVistaParaItem(string id)
        {
            var item = ObtenerItemPorId(id);
            
            if (item?.TipoVista == null)
                return null;

            try
            {
                return Activator.CreateInstance(item.TipoVista) as UserControl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear vista para '{id}': {ex.Message}");
                return null;
            }
        }

        public UserControl? CrearVistaParaItem(MenuHamburguesaItem item)
        {
            if (item?.TipoVista == null)
                return null;

            try
            {
                return Activator.CreateInstance(item.TipoVista) as UserControl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear vista para '{item.Id}': {ex.Message}");
                return null;
            }
        }

        public string ObtenerTituloModulo(string id)
        {
            var item = ObtenerItemPorId(id);
            return item?.Titulo?.ToUpper() ?? "CONFIGURACION";
        }

        public bool EsModuloMenuHamburguesa(string id)
        {
            return _items.Any(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public void SetHabilitado(string id, bool habilitado)
        {
            var item = ObtenerItemPorId(id);
            if (item != null)
            {
                item.Habilitado = habilitado;
            }
        }
    }
}