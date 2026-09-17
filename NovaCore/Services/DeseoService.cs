using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// Gestiona la lista de deseos (wishlist) de cada usuario. Igual que la
    /// biblioteca, los deseos se guardan en disco para que no se pierdan al
    /// cerrar la aplicación.
    /// </summary>
    public class DeseoService
    {
        private readonly DataStore _db = DataStore.Instance;
        private readonly BibliotecaService _bibliotecaService = new();

        public bool EstaEnDeseos(int usuarioId, int juegoId) =>
            _db.Deseos.Any(d => d.UsuarioId == usuarioId && d.JuegoId == juegoId);

        public List<ItemDeseo> ObtenerDeseosDeUsuario(int usuarioId) =>
            _db.Deseos
               .Where(d => d.UsuarioId == usuarioId)
               .OrderByDescending(d => d.FechaAgregado)
               .ToList();

        public (bool exito, string mensaje) AgregarADeseos(Usuario usuario, Juego juego)
        {
            if (_bibliotecaService.PoseeJuego(usuario.UsuarioId, juego.JuegoId))
                return (false, "Ya tienes este juego en tu biblioteca.");

            if (EstaEnDeseos(usuario.UsuarioId, juego.JuegoId))
                return (false, "Este juego ya está en tu lista de deseos.");

            _db.Deseos.Add(new ItemDeseo
            {
                ItemDeseoId = _db.SiguienteDeseoId(),
                UsuarioId = usuario.UsuarioId,
                JuegoId = juego.JuegoId,
                FechaAgregado = DateTime.Now
            });
            PersistenciaService.GuardarDeseos(_db.Deseos);
            return (true, $"'{juego.Titulo}' se agregó a tu lista de deseos.");
        }

        public void QuitarDeDeseos(int usuarioId, int juegoId)
        {
            _db.Deseos.RemoveAll(d => d.UsuarioId == usuarioId && d.JuegoId == juegoId);
            PersistenciaService.GuardarDeseos(_db.Deseos);
        }
    }
}
