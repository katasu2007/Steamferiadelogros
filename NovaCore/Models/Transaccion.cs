namespace NovaCore.Models
{
    public enum EstadoTransaccion
    {
        Completada,
        Fallida,
        Reembolsada
    }

    /// <summary>
    /// Distingue el movimiento dentro de la billetera: una compra descuenta
    /// saldo, mientras que una recarga (tarjeta, PayPal o gift card) lo suma.
    /// </summary>
    public enum TipoTransaccion
    {
        Compra,
        Recarga
    }

    /// <summary>
    /// Representa un movimiento del usuario: ya sea la compra de un juego
    /// (descuenta saldo) o una recarga de billetera (suma saldo). Se persiste
    /// en <c>transacciones.json</c> y alimenta el historial de la Billetera y
    /// los reportes de ventas.
    /// </summary>
    public class Transaccion
    {
        public int TransaccionId { get; set; }
        public int UsuarioId { get; set; }

        /// <summary>Movimiento de billetera: compra o recarga.</summary>
        public TipoTransaccion Tipo { get; set; } = TipoTransaccion.Compra;

        /// <summary>JuegoId = 0 en las recargas (no están asociadas a un juego).</summary>
        public int JuegoId { get; set; }

        /// <summary>Título del juego comprado o descripción de la recarga.</summary>
        public string JuegoTitulo { get; set; } = string.Empty;

        /// <summary>Método usado (ej. "Tarjeta Visa", "PayPal", "Gift Card STEAM50").</summary>
        public string MetodoPago { get; set; } = string.Empty;

        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public EstadoTransaccion Estado { get; set; } = EstadoTransaccion.Completada;
    }
}
