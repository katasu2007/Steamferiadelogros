using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Recomendaciones personalizadas: juegos que el usuario todavía no posee
    /// pero que encajan con las categorías que ya tiene y con sus búsquedas
    /// recientes. Si aún no hay datos de interés, muestra los títulos más
    /// populares del catálogo.
    /// </summary>
    public class RecomendacionesView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly RecomendacionService _recomendacionService = new();
        private readonly BibliotecaService _bibliotecaService = new();

        public RecomendacionesView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var usuario = SessionManager.UsuarioActual!;

            var header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.PanelDark };
            var btnVolver = new Button
            {
                Text = "←",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(36, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 0, 0, 0),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnVolver.FlatAppearance.BorderSize = 0;
            btnVolver.Click += (s, e) => _mainForm.AbrirMenuPrincipal();
            var lblTitulo = new Label { Text = "RECOMENDACIONES", ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontSubtitle, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            header.Controls.Add(lblTitulo);
            header.Controls.Add(btnVolver);

            void ReposicionarFlecha()
            {
                btnVolver.Left = Math.Max(4, header.ClientSize.Width - btnVolver.Width - 8);
                btnVolver.Top = Math.Max(4, (header.ClientSize.Height - btnVolver.Height) / 2);
            }
            header.Resize += (s, e) => ReposicionarFlecha();
            ReposicionarFlecha();

            Controls.Add(header);

            int cantidadPoseidos = _bibliotecaService.ObtenerBibliotecaDeUsuario(usuario.UsuarioId).Count;
            var recomendaciones = _recomendacionService.ObtenerRecomendaciones(usuario.UsuarioId, cantidad: 8);

            var descripcion = new Label
            {
                Text = cantidadPoseidos > 0
                    ? "Basadas en los juegos que tienes y tus búsquedas recientes."
                    : "Basadas en las categorías y juegos más populares del catálogo.",
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(14, 52),
                AutoSize = true
            };
            Controls.Add(descripcion);

            var flow = new FlowLayoutPanel
            {
                Location = new Point(0, 80),
                Width = 400,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            if (recomendaciones.Count == 0)
            {
                Controls.Add(new Label
                {
                    Text = "No hay juegos disponibles para recomendar todavía.",
                    ForeColor = UiTheme.TextSecondary,
                    Location = new Point(14, 80),
                    AutoSize = true
                });
                return;
            }

            foreach (var juego in recomendaciones)
                flow.Controls.Add(CrearCard(juego));

            Controls.Add(flow);
        }

        private Panel CrearCard(Juego juego)
        {
            var card = new Panel { Size = new Size(376, 230), Margin = new Padding(12, 0, 12, 16), BackColor = UiTheme.PanelDark, Cursor = Cursors.Hand };

            var portada = new Panel { Size = new Size(376, 150), Location = new Point(0, 0) };
            portada.Paint += (s, e) =>
            {
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(portada.ClientRectangle, UiTheme.AccentPrimary, UiTheme.PanelDark, 45f);
                e.Graphics.FillRectangle(brush, portada.ClientRectangle);
                using var font = new Font("Segoe UI", 20f * UiTheme.EscalaUI, FontStyle.Bold);
                var size = e.Graphics.MeasureString(juego.Titulo, font);
                float x = Math.Max(8, (portada.Width - size.Width) / 2);
                e.Graphics.DrawString(juego.Titulo, font, Brushes.White, x, portada.Height / 2f - size.Height / 2f);
            };

            var lblTitulo = new Label { Text = juego.Titulo, ForeColor = UiTheme.TextPrimary, Font = new Font("Segoe UI", 13, FontStyle.Bold), Location = new Point(12, 158), AutoSize = true };
            var lblCategoria = new Label { Text = juego.CategoriaNombre, ForeColor = UiTheme.AccentPrimary, Location = new Point(12, 186), AutoSize = true };

            var lblPrecio = new Label
            {
                Text = juego.Precio.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) + " USD",
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = UiTheme.PanelMid,
                Padding = new Padding(6, 3, 6, 3),
                AutoSize = true,
                Location = new Point(0, 186)
            };
            lblPrecio.Location = new Point(card.Width - lblPrecio.PreferredWidth - 16, 186);

            card.Controls.AddRange(new Control[] { portada, lblTitulo, lblCategoria, lblPrecio });

            void AbrirDetalle(object? s, EventArgs e) => _mainForm.MostrarVista(new GameDetailView(_mainForm, juego.JuegoId));
            card.Click += AbrirDetalle;
            portada.Click += AbrirDetalle;
            lblTitulo.Click += AbrirDetalle;

            return card;
        }
    }
}