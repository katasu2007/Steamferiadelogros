using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// CRUD de juegos y consultas para el catálogo de la tienda.
    /// </summary>
    public class JuegoService
    {
        private readonly DataStore _db = DataStore.Instance;

        public List<Categoria> ObtenerCategorias() => _db.Categorias.ToList();

        public List<Juego> ObtenerCatalogo(string? filtroTexto = null, int? categoriaId = null)
        {
            var query = _db.Juegos.Where(j => j.Activo);

            if (categoriaId.HasValue && categoriaId.Value > 0)
                query = query.Where(j => j.CategoriaId == categoriaId.Value);

            if (!string.IsNullOrWhiteSpace(filtroTexto))
                query = query.Where(j => j.Titulo.Contains(filtroTexto, StringComparison.OrdinalIgnoreCase));

            return query.OrderBy(j => j.Titulo).ToList();
        }

        public Juego? ObtenerPorId(int juegoId) => _db.Juegos.FirstOrDefault(j => j.JuegoId == juegoId);

        public List<Juego> ObtenerTodosIncluyendoInactivos() => _db.Juegos.ToList();

        public Juego Crear(Juego juego)
        {
            juego.JuegoId = _db.SiguienteJuegoId();
            juego.CategoriaNombre = _db.Categorias.FirstOrDefault(c => c.CategoriaId == juego.CategoriaId)?.Nombre ?? string.Empty;
            juego.FechaPublicacion = DateTime.Now;
            juego.Activo = true;
            juego.ImagenPortada = PrepararImagenPortada(juego.ImagenPortada);
            _db.Juegos.Add(juego);
            return juego;
        }

        public bool Actualizar(Juego juegoActualizado)
        {
            var existente = _db.Juegos.FirstOrDefault(j => j.JuegoId == juegoActualizado.JuegoId);
            if (existente == null) return false;

            existente.Titulo = juegoActualizado.Titulo;
            existente.Descripcion = juegoActualizado.Descripcion;
            existente.Precio = juegoActualizado.Precio;
            existente.CategoriaId = juegoActualizado.CategoriaId;
            existente.CategoriaNombre = _db.Categorias.FirstOrDefault(c => c.CategoriaId == juegoActualizado.CategoriaId)?.Nombre ?? string.Empty;
            existente.Desarrollador = juegoActualizado.Desarrollador;
            existente.Publisher = juegoActualizado.Publisher;
            existente.Tags = juegoActualizado.Tags;
            existente.ReviewsResumen = juegoActualizado.ReviewsResumen;
            existente.ReviewsPorcentaje = juegoActualizado.ReviewsPorcentaje;
            existente.ReviewsCantidad = juegoActualizado.ReviewsCantidad;
            existente.RequisitosMinimos = juegoActualizado.RequisitosMinimos;
            existente.RequisitosRecomendados = juegoActualizado.RequisitosRecomendados;
            existente.TieneContenidoMaduro = juegoActualizado.TieneContenidoMaduro;
            existente.DescripcionContenidoMaduro = juegoActualizado.DescripcionContenidoMaduro;
            existente.ImagenPortada = PrepararImagenPortada(juegoActualizado.ImagenPortada);
            return true;
        }

        /// <summary>
        /// Copia la imagen elegida a la carpeta de portadas de la app (para que
        /// persista entre reinicios) y devuelve la nueva ruta. Si la ruta ya está
        /// dentro de esa carpeta, no la duplica; si no es un archivo local, la
        /// devuelve sin cambios.
        /// </summary>
        public string? PrepararImagenPortada(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return null;

            try
            {
                if (!File.Exists(ruta)) return ruta;

                var carpeta = PersistenciaService.CarpetaPortadas;
                Directory.CreateDirectory(carpeta);

                var rutaCompleta = Path.GetFullPath(ruta);
                var carpetaCompleta = Path.GetFullPath(carpeta);
                if (Path.GetDirectoryName(rutaCompleta)?.Equals(carpetaCompleta, StringComparison.OrdinalIgnoreCase) == true)
                    return rutaCompleta;

                var ext = Path.GetExtension(ruta);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

                var destino = Path.Combine(carpeta, $"portada_{DateTime.Now.Ticks}{ext}");
                File.Copy(ruta, destino);
                return destino;
            }
            catch
            {
                return ruta;
            }
        }

        /// <summary>Eliminado lógico: se marca como inactivo en lugar de borrarse físicamente.</summary>
        public bool Eliminar(int juegoId)
        {
            var existente = _db.Juegos.FirstOrDefault(j => j.JuegoId == juegoId);
            if (existente == null) return false;
            existente.Activo = false;
            return true;
        }
    }
}
