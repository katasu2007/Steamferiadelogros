using SteamCloneApp.Models;
using System.Text.RegularExpressions;

namespace SteamCloneApp.Services
{
    /// <summary>
    /// Lógica de negocio relacionada con usuarios: login, registro y administración.
    /// </summary>
    public class UsuarioService
    {
        private readonly DataStore _db = DataStore.Instance;

        private static readonly Regex RegexCorreo = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        public Usuario? Login(string nombreUsuario, string password)
        {
            var usuario = _db.Usuarios.FirstOrDefault(u =>
                u.NombreUsuario.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase) &&
                u.PasswordHash == password);

            if (usuario == null) return null;
            if (!usuario.Activo) throw new InvalidOperationException("Esta cuenta ha sido desactivada por un administrador.");

            return usuario;
        }

        public (bool exito, string mensaje) Registrar(string nombreUsuario, string correo, string password)
        {
            nombreUsuario = nombreUsuario.Trim();
            correo = correo.Trim();

            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
                return (false, "Todos los campos son obligatorios.");

            if (nombreUsuario.Length < 3 || nombreUsuario.Length > 20)
                return (false, "El nombre de usuario debe tener entre 3 y 20 caracteres.");

            if (!NombreUsuarioValido(nombreUsuario))
                return (false, "El nombre de usuario solo puede contener letras, números y guion bajo (_).");

            if (nombreUsuario.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return (false, "Ese nombre de usuario está reservado.");

            if (correo.Length > 100 || !EsCorreoValido(correo))
                return (false, "Ingresa un correo electrónico válido (ej. nombre@dominio.com).");

            if (password.Length < 4 || password.Length > 64)
                return (false, "La contraseña debe tener entre 4 y 64 caracteres.");

            if (!ContraseniaValida(password))
                return (false, "La contraseña debe combinar letras y números.");

            if (_db.Usuarios.Any(u => u.NombreUsuario.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase)))
                return (false, "Ese nombre de usuario ya existe.");

            if (_db.Usuarios.Any(u => u.Correo.Equals(correo, StringComparison.OrdinalIgnoreCase)))
                return (false, "Ese correo ya está registrado.");

            var nuevo = new Usuario
            {
                UsuarioId = _db.SiguienteUsuarioId(),
                NombreUsuario = nombreUsuario,
                Correo = correo,
                PasswordHash = password, // NOTA: en producción usar hashing (BCrypt/PBKDF2)
                Rol = RolUsuario.Usuario,
                Saldo = 50m, // saldo de bienvenida simulado
                Activo = true
            };

            _db.Usuarios.Add(nuevo);
            PersistenciaService.GuardarUsuarios(_db.Usuarios);
            return (true, "Cuenta creada exitosamente.");
        }

        private static bool NombreUsuarioValido(string nombre) =>
            nombre.All(c => char.IsLetterOrDigit(c) || c == '_');

        private static bool EsCorreoValido(string correo) =>
            RegexCorreo.IsMatch(correo);

        private static bool ContraseniaValida(string password) =>
            password.Any(char.IsLetter) && password.Any(char.IsDigit);

        public List<Usuario> ObtenerTodos() => _db.Usuarios.ToList();

        public void CambiarRol(int usuarioId, RolUsuario nuevoRol)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u != null)
            {
                u.Rol = nuevoRol;
                PersistenciaService.GuardarUsuarios(_db.Usuarios);
            }
        }

        public void ToggleActivo(int usuarioId)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u != null)
            {
                u.Activo = !u.Activo;
                PersistenciaService.GuardarUsuarios(_db.Usuarios);
            }
        }

        public void RecargarSaldo(int usuarioId, decimal monto)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u != null)
            {
                u.Saldo += monto;
                PersistenciaService.GuardarUsuarios(_db.Usuarios);
            }
        }

        /// <summary>
        /// Copia la imagen elegida a la carpeta de avatares de la app y guarda
        /// su ruta en el usuario, para que la foto persista entre reinicios.
        /// </summary>
        public void ActualizarAvatar(int usuarioId, string rutaImagen)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u == null) return;

            var rutaAvatares = PersistenciaService.CarpetaAvatares;
            if (!Directory.Exists(rutaAvatares)) Directory.CreateDirectory(rutaAvatares);

            var ext = Path.GetExtension(rutaImagen);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

            var destino = Path.Combine(rutaAvatares, $"avatar_{usuarioId}_{DateTime.Now.Ticks}{ext}");
            File.Copy(rutaImagen, destino);

            u.AvatarPath = destino;
            PersistenciaService.GuardarUsuarios(_db.Usuarios);
        }

        public (bool exito, string mensaje) AgregarAmigo(int usuarioId, string nombreAmigo)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u == null) return (false, "Usuario no encontrado.");

            nombreAmigo = nombreAmigo.Trim();
            if (string.IsNullOrWhiteSpace(nombreAmigo)) return (false, "Escribe un nombre de usuario.");

            var objetivo = _db.Usuarios.FirstOrDefault(x =>
                x.NombreUsuario.Equals(nombreAmigo, StringComparison.OrdinalIgnoreCase));
            if (objetivo == null) return (false, "No existe un usuario registrado con ese nombre.");
            if (objetivo.UsuarioId == usuarioId) return (false, "No puedes agregarte a ti mismo.");
            if (!objetivo.Activo) return (false, $"'{objetivo.NombreUsuario}' no puede recibir solicitudes (cuenta desactivada).");
            if (u.Amigos.Any(a => a.Equals(objetivo.NombreUsuario, StringComparison.OrdinalIgnoreCase)))
                return (false, "Ya tienes agregado a este amigo.");

            u.Amigos.Add(objetivo.NombreUsuario);
            PersistenciaService.GuardarUsuarios(_db.Usuarios);
            return (true, $"Se agregó a '{objetivo.NombreUsuario}' como amigo.");
        }

        public (bool exito, string mensaje) EliminarAmigo(int usuarioId, string nombreAmigo)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u == null) return (false, "Usuario no encontrado.");

            var amigo = u.Amigos.FirstOrDefault(a => a.Equals(nombreAmigo.Trim(), StringComparison.OrdinalIgnoreCase));
            if (amigo == null) return (false, "Ese usuario no está en tu lista de amigos.");

            u.Amigos.Remove(amigo);
            PersistenciaService.GuardarUsuarios(_db.Usuarios);
            return (true, $"Se eliminó a '{amigo}' de tus amigos.");
        }

        /// <summary>
        /// Devuelve los usuarios reales correspondientes a la lista de amigos.
        /// Solo incluye cuentas que siguen registradas (nombres huérfanos se descartan).
        /// </summary>
        public List<Usuario> ObtenerAmigos(int usuarioId)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u == null) return new List<Usuario>();

            return u.Amigos
                .Select(nombre => _db.Usuarios.FirstOrDefault(x =>
                    x.NombreUsuario.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
                .Where(x => x != null)
                .ToList();
        }

        /// <summary>Lista de usuarios distintos al indicado, útil para el flujo de "Reportar usuario".</summary>
        public List<Usuario> ObtenerOtrosUsuarios(int usuarioIdExcluir) =>
            _db.Usuarios.Where(u => u.UsuarioId != usuarioIdExcluir).ToList();

        /// <summary>
        /// Elimina definitivamente la cuenta de un usuario no administrador,
        /// limpiando también su biblioteca, transacciones y carrito.
        /// </summary>
        public (bool exito, string mensaje) EliminarCuenta(int usuarioId)
        {
            var u = _db.Usuarios.FirstOrDefault(x => x.UsuarioId == usuarioId);
            if (u == null) return (false, "Usuario no encontrado.");
            if (u.Rol == RolUsuario.Administrador) return (false, "La cuenta de administrador no puede eliminarse.");

            _db.Usuarios.Remove(u);
            _db.Biblioteca.RemoveAll(b => b.UsuarioId == usuarioId);
            _db.Transacciones.RemoveAll(t => t.UsuarioId == usuarioId);
            _db.Carritos.Remove(usuarioId);

            PersistenciaService.GuardarUsuarios(_db.Usuarios);
            return (true, "Tu cuenta fue eliminada correctamente.");
        }
    }
}
