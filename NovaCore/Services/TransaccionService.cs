using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// Procesa compras: descuenta saldo, registra la transacción y entrega
    /// el juego a la biblioteca del usuario. También expone datos para reportes.
    /// </summary>
    public class TransaccionService
    {
        private readonly DataStore _db = DataStore.Instance;
        private readonly BibliotecaService _bibliotecaService = new();

        public (bool exito, string mensaje) ProcesarCompra(Usuario usuario, Juego juego)
        {
            if (_bibliotecaService.PoseeJuego(usuario.UsuarioId, juego.JuegoId))
                return (false, "Ya tienes este juego en tu biblioteca.");

            if (usuario.Saldo < juego.Precio)
                return (false, "Saldo insuficiente en tu billetera virtual.");

            usuario.Saldo -= juego.Precio;

            var transaccion = new Transaccion
            {
                TransaccionId = _db.SiguienteTransaccionId(),
                UsuarioId = usuario.UsuarioId,
                JuegoId = juego.JuegoId,
                JuegoTitulo = juego.Titulo,
                Monto = juego.Precio,
                Fecha = DateTime.Now,
                Estado = EstadoTransaccion.Completada
            };
            _db.Transacciones.Add(transaccion);
            PersistenciaService.GuardarTransacciones(_db.Transacciones);

            _bibliotecaService.AgregarJuego(usuario.UsuarioId, juego.JuegoId);

            new NotificacionService().Crear(
                usuario.UsuarioId,
                TipoNotificacion.Compra,
                "Compra realizada",
                $"Compraste '{juego.Titulo}' por {juego.Precio:C}.");

            return (true, $"¡Compra exitosa! '{juego.Titulo}' se añadió a tu biblioteca.");
        }

        public List<Transaccion> ObtenerTodas() => _db.Transacciones.OrderByDescending(t => t.Fecha).ToList();

        public decimal ObtenerIngresosTotales() =>
            _db.Transacciones.Where(t => t.Estado == EstadoTransaccion.Completada).Sum(t => t.Monto);

        public int ContarVentas() =>
            _db.Transacciones.Count(t => t.Estado == EstadoTransaccion.Completada);

        public List<(string Titulo, int Ventas, decimal Ingresos)> ObtenerRankingJuegos() =>
            _db.Transacciones
               .Where(t => t.Estado == EstadoTransaccion.Completada)
               .GroupBy(t => t.JuegoTitulo)
               .Select(g => (Titulo: g.Key, Ventas: g.Count(), Ingresos: g.Sum(x => x.Monto)))
               .OrderByDescending(x => x.Ingresos)
               .ToList();
    }
}
