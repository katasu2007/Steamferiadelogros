namespace NovaCore.Models
{
    /// <summary>Juego guardado por un usuario en su lista de deseos (wishlist).</summary>
    public class ItemDeseo
    {
        public int ItemDeseoId { get; set; }
        public int UsuarioId { get; set; }
        public int JuegoId { get; set; }
        public DateTime FechaAgregado { get; set; } = DateTime.Now;
    }
}
