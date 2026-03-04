using System;

namespace Allva.Desktop.Models
{
    /// <summary>
    /// Define un item del menú hamburguesa
    /// Cada panel en la carpeta MenuHamburguesa debe registrarse con esta información
    /// </summary>
    public class MenuHamburguesaItem
    {
        /// <summary>
        /// Identificador único del módulo (ej: "apis", "configuracion")
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Texto que se muestra en el menú
        /// </summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Descripción corta del módulo (opcional)
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Orden de aparición en el menú (menor = primero)
        /// </summary>
        public int Orden { get; set; } = 100;

        /// <summary>
        /// Indica si el item está habilitado
        /// </summary>
        public bool Habilitado { get; set; } = true;

        /// <summary>
        /// Indica si requiere permisos especiales
        /// </summary>
        public bool RequierePermisoEspecial { get; set; } = false;

        /// <summary>
        /// Nombre del permiso requerido (si RequierePermisoEspecial = true)
        /// </summary>
        public string? PermisoRequerido { get; set; }

        /// <summary>
        /// Tipo de la vista asociada (se usa para crear instancias)
        /// </summary>
        public Type? TipoVista { get; set; }
    }
}