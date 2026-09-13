using SteamCloneApp.Models;

namespace SteamCloneApp.Services
{
    /// <summary>
    /// Permite a cualquier usuario reportar un juego o a otro usuario, y a los
    /// administradores revisar / marcar como resueltos esos reportes.
    /// </summary>
    public class ReporteService
    {
        private readonly DataStore _db = DataStore.Instance;

        public Reporte ReportarJuego(Usuario reportante, Juego juego, string motivo)
        {
            var reporte = new Reporte
            {
                ReporteId = _db.SiguienteReporteId(),
                Tipo = TipoReporte.Juego,
                ObjetivoId = juego.JuegoId,
                ObjetivoNombre = juego.Titulo,
                UsuarioReportanteId = reportante.UsuarioId,
                UsuarioReportanteNombre = reportante.NombreUsuario,
                Motivo = motivo,
                Fecha = DateTime.Now,
                Estado = EstadoReporte.Pendiente
            };
            _db.Reportes.Add(reporte);
            return reporte;
        }

        public Reporte ReportarUsuario(Usuario reportante, Usuario objetivo, string motivo)
        {
            var reporte = new Reporte
            {
                ReporteId = _db.SiguienteReporteId(),
                Tipo = TipoReporte.Usuario,
                ObjetivoId = objetivo.UsuarioId,
                ObjetivoNombre = objetivo.NombreUsuario,
                UsuarioReportanteId = reportante.UsuarioId,
                UsuarioReportanteNombre = reportante.NombreUsuario,
                Motivo = motivo,
                Fecha = DateTime.Now,
                Estado = EstadoReporte.Pendiente
            };
            _db.Reportes.Add(reporte);
            return reporte;
        }

        public List<Reporte> ObtenerTodos() => _db.Reportes.OrderByDescending(r => r.Fecha).ToList();

        public int ContarPendientes() => _db.Reportes.Count(r => r.Estado == EstadoReporte.Pendiente);

        public void MarcarRevisado(int reporteId)
        {
            var reporte = _db.Reportes.FirstOrDefault(r => r.ReporteId == reporteId);
            if (reporte != null) reporte.Estado = EstadoReporte.Revisado;
        }
    }
}
