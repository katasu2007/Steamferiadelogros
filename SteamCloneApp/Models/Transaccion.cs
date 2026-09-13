namespace SteamCloneApp.Models
{
    public enum EstadoTransaccion
    {
        Completada,
        Fallida,
        Reembolsada
    }

    public class Transaccion
    {
        public int TransaccionId { get; set; }
        public int UsuarioId { get; set; }
        public int JuegoId { get; set; }
        public string JuegoTitulo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public EstadoTransaccion Estado { get; set; } = EstadoTransaccion.Completada;
    }
}
