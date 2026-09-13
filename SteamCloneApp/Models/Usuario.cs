namespace SteamCloneApp.Models
{
    public enum RolUsuario
    {
        Usuario,
        Administrador
    }

    /// <summary>
    /// Representa a un usuario registrado en la plataforma.
    /// </summary>
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public RolUsuario Rol { get; set; } = RolUsuario.Usuario;
        public decimal Saldo { get; set; } = 0;
        public bool Activo { get; set; } = true;
        public string? AvatarPath { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        /// <summary>Lista de amigos (nombres de usuario) agregados manualmente desde el Perfil.</summary>
        public List<string> Amigos { get; set; } = new();
    }
}
