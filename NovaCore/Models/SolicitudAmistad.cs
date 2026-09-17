namespace NovaCore.Models
{
    public enum EstadoSolicitud
    {
        Pendiente,
        Aceptada,
        Rechazada
    }

    /// <summary>
    /// Solicitud de amistad entre dos usuarios. Nace "Pendiente", luego el
    /// destinatario la acepta o rechaza; el remitente ve el resultado en su
    /// pantalla de notificaciones.
    /// </summary>
    public class SolicitudAmistad
    {
        public int SolicitudId { get; set; }
        public int RemitenteId { get; set; }
        public string RemitenteNombre { get; set; } = string.Empty;
        public int DestinatarioId { get; set; }
        public string DestinatarioNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    }
}