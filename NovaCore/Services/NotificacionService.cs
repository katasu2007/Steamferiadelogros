using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// Crea y administra las notificaciones de cada usuario. A diferencia del
    /// comportamiento anterior (secciones calculadas al vuelo que desaparecían
    /// al responder una solicitud), las notificaciones se persisten y solo se
    /// eliminan cuando el usuario las borra explícitamente.
    /// </summary>
    public class NotificacionService
    {
        private readonly DataStore _db = DataStore.Instance;

        public Notificacion Crear(int usuarioId, TipoNotificacion tipo, string titulo, string mensaje, int? solicitudId = null)
        {
            var notificacion = new Notificacion
            {
                NotificacionId = _db.SiguienteNotificacionId(),
                UsuarioId = usuarioId,
                Tipo = tipo,
                Titulo = titulo,
                Mensaje = mensaje,
                Fecha = DateTime.Now,
                Leida = false,
                SolicitudId = solicitudId
            };

            _db.Notificaciones.Add(notificacion);
            PersistenciaService.GuardarNotificaciones(_db.Notificaciones);
            return notificacion;
        }

        public List<Notificacion> ObtenerDeUsuario(int usuarioId) =>
            _db.Notificaciones
               .Where(n => n.UsuarioId == usuarioId)
               .OrderByDescending(n => n.Fecha)
               .ToList();

        public int ContarNoLeidas(int usuarioId) =>
            _db.Notificaciones.Count(n => n.UsuarioId == usuarioId && !n.Leida);

        public void MarcarTodasLeidas(int usuarioId)
        {
            bool huboCambios = false;
            foreach (var n in _db.Notificaciones.Where(n => n.UsuarioId == usuarioId && !n.Leida))
            {
                n.Leida = true;
                huboCambios = true;
            }

            if (huboCambios) PersistenciaService.GuardarNotificaciones(_db.Notificaciones);
        }

        public void Eliminar(int notificacionId)
        {
            _db.Notificaciones.RemoveAll(n => n.NotificacionId == notificacionId);
            PersistenciaService.GuardarNotificaciones(_db.Notificaciones);
        }

        public void EliminarTodas(int usuarioId)
        {
            _db.Notificaciones.RemoveAll(n => n.UsuarioId == usuarioId);
            PersistenciaService.GuardarNotificaciones(_db.Notificaciones);
        }
    }
}
