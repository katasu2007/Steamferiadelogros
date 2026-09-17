using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// Genera recomendaciones personalizadas según los juegos que el usuario
    /// posee y las búsquedas recientes: puntúa cada juego del catálogo por
    /// cercanía a sus categorías e intereses y ordena por afinidad y popularidad.
    /// </summary>
    public class RecomendacionService
    {
        private readonly DataStore _db = DataStore.Instance;

        /// <summary>Registra un término de búsqueda del usuario (máx. 10 recientes).</summary>
        public void RegistrarBusqueda(int usuarioId, string termino)
        {
            if (string.IsNullOrWhiteSpace(termino)) return;

            var terminoLimpio = termino.Trim();
            if (!_db.BusquedasRecientes.TryGetValue(usuarioId, out var lista))
            {
                lista = new List<string>();
                _db.BusquedasRecientes[usuarioId] = lista;
            }

            lista.RemoveAll(t => t.Equals(terminoLimpio, StringComparison.OrdinalIgnoreCase));
            lista.Insert(0, terminoLimpio);
            if (lista.Count > 10) lista.RemoveRange(10, lista.Count - 10);
        }

        public List<Juego> ObtenerRecomendaciones(int usuarioId, int cantidad = 8)
        {
            var poseidosIds = _db.Biblioteca
                .Where(b => b.UsuarioId == usuarioId)
                .Select(b => b.JuegoId)
                .ToHashSet();

            var juegosPoseidos = _db.Juegos.Where(j => poseidosIds.Contains(j.JuegoId)).ToList();

            // Peso por categoría según los juegos que el usuario ya tiene.
            var pesoCategoria = new Dictionary<int, int>();
            foreach (var j in juegosPoseidos)
                pesoCategoria[j.CategoriaId] = pesoCategoria.GetValueOrDefault(j.CategoriaId) + 3;

            var busquedas = _db.BusquedasRecientes.TryGetValue(usuarioId, out var listas)
                ? listas
                : new List<string>();

            var candidatos = _db.Juegos
                .Where(j => j.Activo && !poseidosIds.Contains(j.JuegoId))
                .Select(j => new
                {
                    Juego = j,
                    Puntaje = CalcularPuntaje(j, pesoCategoria, busquedas)
                })
                .OrderByDescending(x => x.Puntaje)
                .ThenByDescending(x => x.Juego.ReviewsCantidad)
                .Select(x => x.Juego)
                .ToList();

            return candidatos.Take(cantidad).ToList();
        }

        private static double CalcularPuntaje(Juego juego, Dictionary<int, int> pesoCategoria, List<string> busquedas)
        {
            double puntaje = pesoCategoria.GetValueOrDefault(juego.CategoriaId) * 10.0;

            foreach (var termino in busquedas)
            {
                if (juego.Titulo.Contains(termino, StringComparison.OrdinalIgnoreCase)) puntaje += 5;
                if (juego.Tags.Contains(termino, StringComparison.OrdinalIgnoreCase)) puntaje += 3;
                if (juego.CategoriaNombre.Contains(termino, StringComparison.OrdinalIgnoreCase)) puntaje += 2;
            }

            return puntaje;
        }
    }
}