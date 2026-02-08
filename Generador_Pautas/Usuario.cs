using System;

namespace Generador_Pautas
{
    /// <summary>
    /// Representa un usuario del sistema
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; }
        public string Contrasena { get; set; }
        public string Rol { get; set; }
        public string NombreCompleto { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Verifica si el usuario es administrador
        /// </summary>
        public bool EsAdministrador => Rol == "Administrador";

        /// <summary>
        /// Verifica si el usuario esta activo
        /// </summary>
        public bool EstaActivo => Estado == "Activo";
    }

    /// <summary>
    /// Roles disponibles en el sistema
    /// </summary>
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Usuario = "Usuario";
    }
}
