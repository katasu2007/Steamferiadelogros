namespace NovaCore.Models
{
    public enum TipoNotificacion
    {
        SolicitudAmistad,
        SolicitudAceptada,
        SolicitudRechazada,
        Compra,
        Sistema
    }

    /// <summary>
    /// Aviso dirigido a un usuario (compra realizada, solicitud de amistad
    /// recibida o respondida, etc.). Se guarda en disco y se conserva hasta que
    /// el propio usuario decida eliminarlo (individualmente o borrando todas).
    /// </summary>
    public class Notificacion
    {
        public int NotificacionId { get; set; }
        public int UsuarioId { get; set; }
        public TipoNotificacion Tipo { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public bool Leida { get; set; }

        /// <summary>Solicitud de amistad asociada (si aplica), para ofrecer Aceptar/Rechazar.</summary>
        public int? SolicitudId { get; set; }
    }
}
