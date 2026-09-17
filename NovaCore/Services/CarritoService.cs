using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// Maneja el carrito de compras por usuario (equivalente al flujo
    /// "Añadir al carrito" -> "Checkout" de Steam). El checkout reutiliza
    /// TransaccionService para descontar saldo y entregar los juegos.
    /// </summary>
    public class CarritoService
    {
        private readonly DataStore _db = DataStore.Instance;
        private readonly BibliotecaService _bibliotecaService = new();
        private readonly TransaccionService _transaccionService = new();

        private List<ItemCarrito> ObtenerListaInterna(int usuarioId)
        {
            if (!_db.Carritos.TryGetValue(usuarioId, out var lista))
            {
                lista = new List<ItemCarrito>();
                _db.Carritos[usuarioId] = lista;
            }
            return lista;
        }

        public List<ItemCarrito> ObtenerCarrito(int usuarioId) => ObtenerListaInterna(usuarioId).ToList();

        public (bool exito, string mensaje) AgregarAlCarrito(int usuarioId, Juego juego)
        {
            if (_bibliotecaService.PoseeJuego(usuarioId, juego.JuegoId))
                return (false, "Ya tienes este juego en tu biblioteca.");

            var carrito = ObtenerListaInterna(usuarioId);
            if (carrito.Any(i => i.JuegoId == juego.JuegoId))
                return (false, "Este juego ya está en tu carrito.");

            carrito.Add(new ItemCarrito { JuegoId = juego.JuegoId, Titulo = juego.Titulo, Precio = juego.Precio });
            return (true, $"'{juego.Titulo}' se agregó al carrito.");
        }

        public void QuitarDelCarrito(int usuarioId, int juegoId)
        {
            ObtenerListaInterna(usuarioId).RemoveAll(i => i.JuegoId == juegoId);
        }

        public decimal ObtenerTotal(int usuarioId) => ObtenerListaInterna(usuarioId).Sum(i => i.Precio);

        /// <summary>Procesa el pago de todos los items del carrito. Si algún ítem falla
        /// (ej. saldo insuficiente a mitad de camino) se detiene y reporta qué se compró.</summary>
        public (bool exito, string mensaje) Checkout(Usuario usuario)
        {
            var carrito = ObtenerListaInterna(usuario.UsuarioId);
            if (carrito.Count == 0) return (false, "Tu carrito está vacío.");

            var juegoService = new JuegoService();
            int comprados = 0;

            foreach (var item in carrito.ToList())
            {
                var juego = juegoService.ObtenerPorId(item.JuegoId);
                if (juego == null) continue;

                var (exito, _) = _transaccionService.ProcesarCompra(usuario, juego);
                if (!exito)
                {
                    return (comprados > 0,
                        $"Se compraron {comprados} juego(s). No se pudo completar la compra de '{juego.Titulo}' (saldo insuficiente).");
                }

                comprados++;
                carrito.Remove(item);
            }

            return (true, $"¡Compra exitosa! Se agregaron {comprados} juego(s) a tu biblioteca.");
        }
    }
}
