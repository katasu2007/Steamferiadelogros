using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Página principal de la tienda, rediseñada para parecerse a la app móvil
    /// de Steam: banner destacado arriba y una lista de cards "Destacados y
    /// recomendados" debajo. Cada card lleva al detalle del juego al tocarla.
    /// </summary>
    public class StoreView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly JuegoService _juegoService = new();
        private readonly FlowLayoutPanel _flowDestacados = new();
        private readonly string? _filtroInicial;
        private readonly int? _categoriaId;

        public StoreView(MainForm mainForm, string? filtroInicial = null, int? categoriaId = null)
        {
            _mainForm = mainForm;
            _filtroInicial = filtroInicial;
            _categoriaId = categoriaId;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var contenedor = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            var juegos = _juegoService.ObtenerCatalogo(_filtroInicial, _categoriaId);
            var destacado = juegos.FirstOrDefault();

            var nombreCategoria = _juegoService.ObtenerCategorias().FirstOrDefault(c => c.CategoriaId == _categoriaId)?.Nombre;

            // --- Banner grande (imagen destacada del primer juego del catálogo) ---
            var banner = new Panel { Location = new Point(0, 0), Size = new Size(400, 220) };
            banner.Paint += (s, e) => DibujarBanner(e.Graphics, banner.ClientRectangle, destacado, nombreCategoria);
            banner.Cursor = Cursors.Hand;
            if (destacado != null)
                banner.Click += (s, e) => _mainForm.MostrarVista(new GameDetailView(_mainForm, destacado.JuegoId));

            var lblSeccion = new Label
            {
                Text = nombreCategoria != null
                    ? $"CATEGORÍA — {nombreCategoria.ToUpper()}"
                    : "Destacados y recomendados",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                Location = new Point(14, 236),
                AutoSize = true
            };

            contenedor.Controls.Add(banner);

            if (nombreCategoria != null)
            {
                var btnVolver = new Button
                {
                    Text = "←",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    Location = new Point(6, 6),
                    Size = new Size(34, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(140, 0, 0, 0),
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                btnVolver.FlatAppearance.BorderSize = 0;
                btnVolver.Click += (s, e) => _mainForm.MostrarVista(new StoreView(_mainForm));
                banner.Controls.Add(btnVolver);
            }

            contenedor.Controls.Add(lblSeccion);
            contenedor.Height = 270;

            _flowDestacados.Location = new Point(0, 270);
            _flowDestacados.FlowDirection = FlowDirection.TopDown;
            _flowDestacados.WrapContents = false;
            _flowDestacados.AutoSize = true;
            _flowDestacados.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _flowDestacados.Width = 400;

            if (string.IsNullOrWhiteSpace(_filtroInicial))
                foreach (var j in juegos) _flowDestacados.Controls.Add(CrearCard(j));
            else
                foreach (var j in juegos) _flowDestacados.Controls.Add(CrearCard(j));

            if (juegos.Count == 0)
                _flowDestacados.Controls.Add(new Label { Text = "No se encontraron juegos.", ForeColor = UiTheme.TextSecondary, Location = new Point(14, 10), AutoSize = true });

            Controls.Add(_flowDestacados);
            Controls.Add(contenedor);
        }

        private void DibujarBanner(Graphics g, Rectangle rect, Juego? juego, string? nombreCategoria = null)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var imagen = juego != null ? UiTheme.CargarImagen(juego.ImagenPortada) : null;
            if (imagen != null)
            {
                UiTheme.DibujarImagenRecortada(g, rect, imagen);
                // Velo oscuro para que el texto blanco se lea sobre la foto.
                using var velo = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect, Color.FromArgb(205, 8, 7, 13), Color.FromArgb(40, 8, 7, 13), 60f);
                g.FillRectangle(velo, rect);
            }
            else
            {
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, UiTheme.AccentPrimary, Color.FromArgb(16, 12, 22), 60f);
                g.FillRectangle(brush, rect);
            }

            float s = UiTheme.EscalaUI;

            if (juego == null)
            {
                if (!string.IsNullOrWhiteSpace(nombreCategoria))
                {
                    using var fontCat = new Font("Segoe UI", 20f * s, FontStyle.Bold);
                    g.DrawString(nombreCategoria.ToUpper(), fontCat, Brushes.White, 14, rect.Height - 60);
                    using var fontVictoria = new Font("Segoe UI", 10f * s, FontStyle.Italic);
                    g.DrawString("Oferta destacada", fontVictoria, Brushes.White, 14, rect.Height - 30);
                }
                return;
            }

            using var fontTitulo = new Font("Segoe UI", 22f * s, FontStyle.Bold);
            using var fontSub = new Font("Segoe UI", 10f * s, FontStyle.Italic);
            var sizeTitulo = g.MeasureString(juego.Titulo.ToUpper(), fontTitulo);
            float x = (rect.Width - sizeTitulo.Width) / 2;
            g.DrawString(juego.Titulo.ToUpper(), fontTitulo, Brushes.White, x, rect.Height - 70);
            g.DrawString(nombreCategoria != null ? nombreCategoria.ToUpper() : "Oferta destacada", fontSub, Brushes.White, 14, rect.Height - 30);
        }

        /// <summary>Genera una card horizontal ancha (imagen + título + reviews + precio), similar a
        /// las tarjetas de "Destacados y recomendados" de la app móvil de Steam.</summary>
        private Panel CrearCard(Juego juego)
        {
            var card = new Panel { Size = new Size(376, 230), Margin = new Padding(12, 0, 12, 16), BackColor = UiTheme.PanelDark, Cursor = Cursors.Hand };

            var portada = new Panel { Size = new Size(376, 150), Location = new Point(0, 0) };
            portada.Paint += (s, e) => DibujarPortada(e.Graphics, portada.ClientRectangle, juego);

            var lblTitulo = new Label { Text = juego.Titulo, ForeColor = UiTheme.TextPrimary, Font = new Font("Segoe UI", 13, FontStyle.Bold), Location = new Point(12, 158), AutoSize = true };

            var colorReviews = juego.ReviewsPorcentaje >= 80 ? UiTheme.AccentGreen : (juego.ReviewsPorcentaje >= 60 ? UiTheme.AccentPrimary : UiTheme.TextSecondary);
            var lblReviews = new Label
            {
                Text = juego.ReviewsCantidad > 0 ? $"{juego.ReviewsResumen} ({juego.ReviewsCantidad:N0} reseñas)" : "Sin reseñas todavía",
                ForeColor = colorReviews,
                Font = new Font("Segoe UI", 9),
                Location = new Point(12, 186),
                AutoSize = true
            };

            var lblPrecio = new Label
            {
                Text = juego.Precio.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) + " USD",
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = UiTheme.PanelMid,
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true
            };
            lblPrecio.Location = new Point(card.Width - lblPrecio.PreferredWidth - 16, 186);

            card.Controls.AddRange(new Control[] { portada, lblTitulo, lblReviews, lblPrecio });

            void AbrirDetalle(object? s, EventArgs e) => _mainForm.MostrarVista(new GameDetailView(_mainForm, juego.JuegoId));
            card.Click += AbrirDetalle;
            portada.Click += AbrirDetalle;
            lblTitulo.Click += AbrirDetalle;

            return card;
        }

        private void DibujarPortada(Graphics g, Rectangle rect, Juego juego)
        {
            var imagen = UiTheme.CargarImagen(juego.ImagenPortada);
            if (imagen != null)
            {
                UiTheme.DibujarImagenRecortada(g, rect, imagen);
                return;
            }

            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, UiTheme.AccentPrimary, UiTheme.PanelDark, 45f);
            g.FillRectangle(brush, rect);
            using var font = new Font("Segoe UI", 20f * UiTheme.EscalaUI, FontStyle.Bold);
            var size = g.MeasureString(juego.Titulo, font);
            float x = Math.Max(8, (rect.Width - size.Width) / 2);
            g.DrawString(juego.Titulo, font, Brushes.White, x, rect.Height / 2f - size.Height / 2f);
        }
    }
}
