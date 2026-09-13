using SteamCloneApp.Models;

namespace SteamCloneApp.Services
{
    /// <summary>
    /// Gestiona la biblioteca de juegos adquiridos por cada usuario.
    /// </summary>
    public class BibliotecaService
    {
        private readonly DataStore _db = DataStore.Instance;

        public bool PoseeJuego(int usuarioId, int juegoId) =>
            _db.Biblioteca.Any(b => b.UsuarioId == usuarioId && b.JuegoId == juegoId);

        public List<BibliotecaItem> ObtenerBibliotecaDeUsuario(int usuarioId) =>
            _db.Biblioteca.Where(b => b.UsuarioId == usuarioId).ToList();

        public BibliotecaItem AgregarJuego(int usuarioId, int juegoId)
        {
            var item = new BibliotecaItem
            {
                BibliotecaId = _db.SiguienteBibliotecaId(),
                UsuarioId = usuarioId,
                JuegoId = juegoId,
                FechaCompra = DateTime.Now,
                HorasJugadas = 0
            };
            _db.Biblioteca.Add(item);
            return item;
        }

        public void RegistrarSesionJugada(int bibliotecaId, double horasSumar)
        {
            var item = _db.Biblioteca.FirstOrDefault(b => b.BibliotecaId == bibliotecaId);
            if (item == null) return;
            item.HorasJugadas += horasSumar;
            item.UltimaSesion = DateTime.Now;
        }
    }
}
