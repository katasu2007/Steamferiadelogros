namespace NovaCore.Models
{
    /// <summary>Representa un juego agregado al carrito, pendiente de pagar.</summary>
    public class ItemCarrito
    {
        public int JuegoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public decimal Precio { get; set; }
    }
}
