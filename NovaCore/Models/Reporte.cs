namespace NovaCore.Models
{
    public enum TipoReporte
    {
        Juego,
        Usuario
    }

    public enum EstadoReporte
    {
        Pendiente,
        Revisado
    }

    /// <summary>
    /// Reporte enviado por un usuario contra un juego (contenido inapropiado,
    /// bug, etc.) o contra otro usuario (comportamiento, nombre ofensivo, etc.).
    /// Los administradores los revisan desde el panel de moderación.
    /// </summary>
    public class Reporte
    {
        public int ReporteId { get; set; }
        public TipoReporte Tipo { get; set; }
        public int ObjetivoId { get; set; }
        public string ObjetivoNombre { get; set; } = string.Empty;
        public int UsuarioReportanteId { get; set; }
        public string UsuarioReportanteNombre { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public EstadoReporte Estado { get; set; } = EstadoReporte.Pendiente;
    }
}
