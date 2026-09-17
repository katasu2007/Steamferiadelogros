namespace NovaCore.Models
{
    /// <summary>
    /// Representa un título disponible en la tienda. Se amplió con campos
    /// inspirados en la ficha real de Steam (publisher, tags, reviews,
    /// requisitos mínimos/recomendados, contenido maduro) para que la vista
    /// de detalle pueda parecerse a la app oficial.
    /// </summary>
    public class Juego
    {
        public int JuegoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public string Desarrollador { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;

        /// <summary>Etiquetas separadas por coma, ej: "Early Access, FPS, Action, Military".</summary>
        public string Tags { get; set; } = string.Empty;
        public List<string> TagsLista => Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        // --- Reviews (simuladas, no hay usuarios reales calificando) ---
        public string ReviewsResumen { get; set; } = "Sin reseñas";
        public int ReviewsPorcentaje { get; set; } = 0;
        public int ReviewsCantidad { get; set; } = 0;

        // --- Requisitos de sistema, separados como en la ficha real de Steam ---
        public string RequisitosMinimos { get; set; } = string.Empty;
        public string RequisitosRecomendados { get; set; } = string.Empty;

        // --- Contenido maduro ---
        public bool TieneContenidoMaduro { get; set; } = false;
        public string DescripcionContenidoMaduro { get; set; } = string.Empty;

        /// <summary>Ruta local o URL de la imagen de portada. Si no existe, se dibuja un placeholder.</summary>
        public string? ImagenPortada { get; set; }
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;
    }
}
