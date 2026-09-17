using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// Mantiene el estado de la sesión activa (usuario autenticado) durante
    /// la ejecución de la aplicación. Es consultado por MainForm para decidir
    /// qué vistas/menús mostrar según el rol.
    /// </summary>
    public static class SessionManager
    {
        public static Usuario? UsuarioActual { get; private set; }

        public static bool HaySesionActiva => UsuarioActual != null;
        public static bool EsAdministrador => UsuarioActual?.Rol == RolUsuario.Administrador;

        public static void IniciarSesion(Usuario usuario) => UsuarioActual = usuario;

        public static void CerrarSesion() => UsuarioActual = null;
    }
}
