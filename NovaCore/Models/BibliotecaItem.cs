using System.Text.Json.Serialization;

namespace NovaCore.Models
{
    /// <summary>
    /// Relación entre un usuario y un juego que ha adquirido, incluyendo horas jugadas.
    /// </summary>
    public class BibliotecaItem
    {
        public int BibliotecaId { get; set; }
        public int UsuarioId { get; set; }
        public int JuegoId { get; set; }
        public DateTime FechaCompra { get; set; } = DateTime.Now;
        public double HorasJugadas { get; set; } = 0;
        public DateTime? UltimaSesion { get; set; }

        /// <summary>Estado transitorio en memoria, no persistido: indica si el juego está "en ejecución" ahora mismo.</summary>
        [JsonIgnore]
        public bool EnEjecucion { get; set; } = false;
    }
}
