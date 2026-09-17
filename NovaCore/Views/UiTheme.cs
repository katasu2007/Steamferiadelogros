namespace NovaCore.Views
{
    /// <summary>
    /// Paleta de colores y helpers de estilo compartidos por todas las vistas.
    /// Tema "nova": negros espaciales con matiz violeta, textos blanco cálido y
    /// un acento naranja incandescente (explosión de una nova).
    /// </summary>
    public static class UiTheme
    {
        /// <summary>
        /// Factor de escala actual de la UI (1 = tamaño de diseño, 400x820).
        /// MainForm lo actualiza al redimensionar/adaptar la ventana y las vistas
        /// lo usan para ajustar los textos dibujados a mano (Graphics.DrawString).
        /// </summary>
        public static float EscalaUI = 1f;

        public static readonly Color BackgroundDark = Color.FromArgb(8, 7, 13);       // negro espacial (matiz violeta)
        public static readonly Color PanelDark = Color.FromArgb(18, 16, 26);           // panel profundo
        public static readonly Color PanelMid = Color.FromArgb(29, 25, 39);            // superficie elevada
        public static readonly Color BorderColor = Color.FromArgb(47, 41, 61);         // bordes
        public static readonly Color TextPrimary = Color.FromArgb(246, 241, 234);      // blanco cálido
        public static readonly Color TextSecondary = Color.FromArgb(167, 159, 181);    // lavanda tenue
        public static readonly Color AccentPrimary = Color.FromArgb(255, 122, 26);     // naranja nova (acento principal)
        public static readonly Color AccentGlow = Color.FromArgb(255, 176, 74);        // resplandor dorado
        public static readonly Color AccentGreen = Color.FromArgb(79, 209, 165);       // éxito (esmeralda)
        public static readonly Color AccentRed = Color.FromArgb(229, 72, 77);          // peligro
        public static readonly Font FontTitle = new("Segoe UI", 16, FontStyle.Bold);
        public static readonly Font FontSubtitle = new("Segoe UI", 11, FontStyle.Bold);
        public static readonly Font FontBody = new("Segoe UI", 9.5f);

        /// <summary>Crea un botón con estilo plano, coloreado con el acento indicado.</summary>
        public static Button CreateStyledButton(string text, Color backColor, Color? foreColor = null)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor ?? Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = FontSubtitle,
                Cursor = Cursors.Hand,
                Height = 38
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.15f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.1f);
            return btn;
        }

        /// <summary>
        /// Botón "volver" (←) estilo app móvil para las pantallas secundarias,
        /// ubicado en la esquina superior izquierda.
        /// </summary>
        public static Button CreateBackButton(Action alVolver)
        {
            var btn = new Button
            {
                Text = "←",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Location = new Point(8, 8),
                Size = new Size(34, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = PanelDark,
                ForeColor = TextPrimary,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => alVolver();
            return btn;
        }

        /// <summary>
        /// Barra superior fija con botón "volver" + título, para las pantallas
        /// secundarias cuyo contenido puede hacer scroll (la barra no se mueve).
        /// </summary>
        public static Panel CreateBackBar(string titulo, Action alVolver)
        {
            var barra = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = PanelDark
            };

            var btn = CreateBackButton(alVolver);
            btn.Location = new Point(8, 7);
            btn.Size = new Size(34, 34);
            barra.Controls.Add(btn);

            var lbl = new Label
            {
                Text = titulo,
                ForeColor = TextPrimary,
                Font = FontSubtitle,
                Location = new Point(54, 13),
                AutoSize = true
            };
            barra.Controls.Add(lbl);

            return barra;
        }

        /// <summary>Aplica estilo oscuro consistente a un DataGridView.</summary>
        public static void StyleDataGridView(DataGridView grid)
        {
            grid.BackgroundColor = PanelDark;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = BorderColor;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersDefaultCellStyle.BackColor = PanelMid;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = FontSubtitle;
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.BackColor = PanelDark;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = AccentPrimary;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = FontBody;
            grid.RowTemplate.Height = 32;
        }

        // --- Imágenes de portada -------------------------------------------------

        // Caché de imágenes ya cargadas para no leer el disco en cada Paint.
        private static readonly Dictionary<string, Image?> _cacheImagenes = new();

        /// <summary>
        /// Carga (y memoriza) una imagen de portada desde disco. Devuelve null si
        /// la ruta está vacía, no existe o no es una imagen válida. La imagen
        /// devuelta es compartida: las vistas NO deben disponerla.
        /// </summary>
        public static Image? CargarImagen(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return null;
            if (_cacheImagenes.TryGetValue(ruta, out var cacheada)) return cacheada;

            Image? imagen = null;
            try
            {
                if (File.Exists(ruta))
                {
                    using var original = Image.FromFile(ruta);
                    imagen = new Bitmap(original); // copia para no dejar el archivo bloqueado
                }
            }
            catch
            {
                imagen = null;
            }

            _cacheImagenes[ruta] = imagen;
            return imagen;
        }

        /// <summary>
        /// Dibuja la imagen cubriendo todo el rectángulo (recorte centrado tipo
        /// "cover"), sin deformar su proporción.
        /// </summary>
        public static void DibujarImagenRecortada(Graphics g, Rectangle rect, Image imagen)
        {
            if (imagen.Width <= 0 || imagen.Height <= 0 || rect.Width <= 0 || rect.Height <= 0) return;

            float escala = Math.Max((float)rect.Width / imagen.Width, (float)rect.Height / imagen.Height);
            float ancho = imagen.Width * escala;
            float alto = imagen.Height * escala;
            float x = rect.X + (rect.Width - ancho) / 2f;
            float y = rect.Y + (rect.Height - alto) / 2f;

            var modoAnterior = g.InterpolationMode;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            var estado = g.Save();
            g.SetClip(rect);
            g.DrawImage(imagen, x, y, ancho, alto);
            g.Restore(estado);

            g.InterpolationMode = modoAnterior;
        }
    }
}
