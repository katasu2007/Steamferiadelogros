using NovaCore.Models;
using System.Text.RegularExpressions;

namespace NovaCore.Services
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

        /// <summary>
        /// Busca usuarios por nombre para enviarles una solicitud de amistad.
        /// Excluye a uno mismo, cuentas desactivadas, amigos actuales y usuarios
        /// con los que ya hay una solicitud pendiente en cualquier dirección.
        /// </summary>
        public List<Usuario> BuscarUsuarios(int usuarioId, string termino)
        {
            var usuario = _db.Usuarios.FirstOrDefault(u => u.UsuarioId == usuarioId);
            if (usuario == null) return new List<Usuario>();

            bool esAmigo(Usuario u) => usuario.Amigos.Any(a => a.Equals(u.NombreUsuario, StringComparison.OrdinalIgnoreCase));
            bool solicitudPendiente(Usuario u) => _db.SolicitudesAmistad.Any(s => s.Estado == EstadoSolicitud.Pendiente &&
                ((s.RemitenteId == usuarioId && s.DestinatarioId == u.UsuarioId) ||
                 (s.DestinatarioId == usuarioId && s.RemitenteId == u.UsuarioId)));

            IEnumerable<Usuario> resultados = _db.Usuarios
                .Where(u => u.UsuarioId != usuarioId)
                .Where(u => u.Activo)
                .Where(u => !esAmigo(u))
                .Where(u => !solicitudPendiente(u));

            if (!string.IsNullOrWhiteSpace(termino))
                resultados = resultados.Where(u => u.NombreUsuario.Contains(termino, StringComparison.OrdinalIgnoreCase));

            return resultados.OrderBy(u => u.NombreUsuario).ToList();
        }

        /// <summary>Envía una solicitud de amistad a otro usuario (queda pendiente hasta que la acepte o rechace).</summary>
        public (bool exito, string mensaje) EnviarSolicitud(int remitenteId, int destinatarioId)
        {
            var remitente = _db.Usuarios.FirstOrDefault(u => u.UsuarioId == remitenteId);
            var destinatario = _db.Usuarios.FirstOrDefault(u => u.UsuarioId == destinatarioId);
            if (remitente == null || destinatario == null)
                return (false, "Usuario no encontrado.");
            if (remitenteId == destinatarioId)
                return (false, "No puedes enviarte una solicitud a ti mismo.");
            if (!destinatario.Activo)
                return (false, $"'{destinatario.NombreUsuario}' no puede recibir solicitudes (cuenta desactivada).");
            if (remitente.Amigos.Any(a => a.Equals(destinatario.NombreUsuario, StringComparison.OrdinalIgnoreCase)))
                return (false, $"Ya tienes a '{destinatario.NombreUsuario}' agregado como amigo.");
            if (_db.SolicitudesAmistad.Any(s => s.Estado == EstadoSolicitud.Pendiente &&
                ((s.RemitenteId == remitenteId && s.DestinatarioId == destinatarioId) ||
                 (s.DestinatarioId == remitenteId && s.RemitenteId == destinatarioId))))
                return (false, "Ya tienes una solicitud pendiente con este usuario.");

            _db.SolicitudesAmistad.Add(new SolicitudAmistad
            {
                SolicitudId = _db.SiguienteSolicitudId(),
                RemitenteId = remitenteId,
                RemitenteNombre = remitente.NombreUsuario,
                DestinatarioId = destinatarioId,
                DestinatarioNombre = destinatario.NombreUsuario,
                Fecha = DateTime.Now,
                Estado = EstadoSolicitud.Pendiente
            });
            PersistenciaService.GuardarSolicitudes(_db.SolicitudesAmistad);

            var solicitud = _db.SolicitudesAmistad[^1];
            new NotificacionService().Crear(
                destinatarioId,
                TipoNotificacion.SolicitudAmistad,
                "Nueva solicitud de amistad",
                $"'{remitente.NombreUsuario}' te envió una solicitud de amistad.",
                solicitud.SolicitudId);

            return (true, $"Solicitud de amistad enviada a '{destinatario.NombreUsuario}'.");
        }

        public SolicitudAmistad? ObtenerSolicitud(int solicitudId) =>
            _db.SolicitudesAmistad.FirstOrDefault(s => s.SolicitudId == solicitudId);

        public List<SolicitudAmistad> ObtenerSolicitudesEntrantes(int usuarioId) =>
            _db.SolicitudesAmistad
               .Where(s => s.DestinatarioId == usuarioId && s.Estado == EstadoSolicitud.Pendiente)
               .OrderByDescending(s => s.Fecha)
               .ToList();

        public List<SolicitudAmistad> ObtenerSolicitudesEnviadas(int usuarioId) =>
            _db.SolicitudesAmistad
               .Where(s => s.RemitenteId == usuarioId)
               .OrderByDescending(s => s.Fecha)
               .ToList();

        public int ContarSolicitudesPendientes(int usuarioId) =>
            _db.SolicitudesAmistad.Count(s => s.DestinatarioId == usuarioId && s.Estado == EstadoSolicitud.Pendiente);

        public (bool exito, string mensaje) AceptarSolicitud(int solicitudId)
        {
            var s = _db.SolicitudesAmistad.FirstOrDefault(x => x.SolicitudId == solicitudId);
            if (s == null) return (false, "La solicitud ya no existe.");
            if (s.Estado != EstadoSolicitud.Pendiente) return (false, "Esta solicitud ya fue respondida.");

            var remitente = _db.Usuarios.FirstOrDefault(u => u.UsuarioId == s.RemitenteId);
            var destinatario = _db.Usuarios.FirstOrDefault(u => u.UsuarioId == s.DestinatarioId);
            if (remitente == null || destinatario == null)
                return (false, "Uno de los usuarios ya no existe.");
            if (!remitente.Activo)
                return (false, $"'{remitente.NombreUsuario}' ya no está activo.");

            s.Estado = EstadoSolicitud.Aceptada;

            if (!remitente.Amigos.Any(a => a.Equals(destinatario.NombreUsuario, StringComparison.OrdinalIgnoreCase)))
                remitente.Amigos.Add(destinatario.NombreUsuario);
            if (!destinatario.Amigos.Any(a => a.Equals(remitente.NombreUsuario, StringComparison.OrdinalIgnoreCase)))
                destinatario.Amigos.Add(remitente.NombreUsuario);

            PersistenciaService.GuardarSolicitudes(_db.SolicitudesAmistad);
            PersistenciaService.GuardarUsuarios(_db.Usuarios);

            new NotificacionService().Crear(
                s.RemitenteId,
                TipoNotificacion.SolicitudAceptada,
                "Solicitud aceptada",
                $"'{destinatario.NombreUsuario}' aceptó tu solicitud de amistad.",
                s.SolicitudId);

            return (true, $"Ahora eres amigo de '{remitente.NombreUsuario}'.");
        }

        public (bool exito, string mensaje) RechazarSolicitud(int solicitudId)
        {
            var s = _db.SolicitudesAmistad.FirstOrDefault(x => x.SolicitudId == solicitudId);
            if (s == null) return (false, "La solicitud ya no existe.");
            if (s.Estado != EstadoSolicitud.Pendiente) return (false, "Esta solicitud ya fue respondida.");

            s.Estado = EstadoSolicitud.Rechazada;
            PersistenciaService.GuardarSolicitudes(_db.SolicitudesAmistad);

            new NotificacionService().Crear(
                s.RemitenteId,
                TipoNotificacion.SolicitudRechazada,
                "Solicitud rechazada",
                $"'{s.DestinatarioNombre}' rechazó tu solicitud de amistad.",
                s.SolicitudId);

            return (true, $"Rechazaste la solicitud de '{s.RemitenteNombre}'.");
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
            _db.SolicitudesAmistad.RemoveAll(s => s.RemitenteId == usuarioId || s.DestinatarioId == usuarioId);
            _db.Deseos.RemoveAll(d => d.UsuarioId == usuarioId);
            _db.Notificaciones.RemoveAll(n => n.UsuarioId == usuarioId);

            PersistenciaService.GuardarUsuarios(_db.Usuarios);
            PersistenciaService.GuardarBiblioteca(_db.Biblioteca);
            PersistenciaService.GuardarSolicitudes(_db.SolicitudesAmistad);
            PersistenciaService.GuardarDeseos(_db.Deseos);
            PersistenciaService.GuardarNotificaciones(_db.Notificaciones);
            PersistenciaService.GuardarTransacciones(_db.Transacciones);
            return (true, "Tu cuenta fue eliminada correctamente.");
        }
    }
}
