using SteamCloneApp.Models;
using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>
    /// Ficha de producto de un juego, rediseñada para parecerse a la app móvil
    /// de Steam: header con imagen, developer/publisher/released, descripción,
    /// tags, reviews, caja de compra (Add to Cart), contenido maduro y
    /// requisitos de sistema (mínimos/recomendados). También permite reportar
    /// el juego.
    /// </summary>
    public class GameDetailView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly int _juegoId;
        private readonly JuegoService _juegoService = new();
        private readonly BibliotecaService _bibliotecaService = new();
        private readonly CarritoService _carritoService = new();
        private readonly ReporteService _reporteService = new();

        private Button _btnAgregarCarrito = new();
        private Label _lblMensaje = new();

        public GameDetailView(MainForm mainForm, int juegoId)
        {
            _mainForm = mainForm;
            _juegoId = juegoId;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var juego = _juegoService.ObtenerPorId(_juegoId);
            if (juego == null)
            {
                Controls.Add(new Label { Text = "El juego no existe.", ForeColor = UiTheme.TextPrimary, AutoSize = true, Location = new Point(14, 14) });
                return;
            }

            int y = 0;
            var contenedor = new Panel { Location = new Point(0, 0), Width = 400, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };

            // --- Header con back button + imagen ---
            var header = new Panel { Size = new Size(400, 200), Location = new Point(0, 0) };
            header.Paint += (s, e) => DibujarHeader(e.Graphics, header.ClientRectangle, juego.Titulo);
            var btnVolver = new Button { Text = "←", Font = new Font("Segoe UI", 14, FontStyle.Bold), Location = new Point(8, 8), Size = new Size(36, 36), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(120, 0, 0, 0), ForeColor = Color.White };
            btnVolver.FlatAppearance.BorderSize = 0;
            btnVolver.Click += (s, e) => _mainForm.MostrarVista(new StoreView(_mainForm));
            header.Controls.Add(btnVolver);
            contenedor.Controls.Add(header);
            y = 210;

            var lblTitulo = EtiquetaEn(contenedor, juego.Titulo, ref y, new Font("Segoe UI", 18, FontStyle.Bold), UiTheme.TextPrimary, 14);

            EtiquetaEn(contenedor, $"Developer   {juego.Desarrollador}", ref y, UiTheme.FontBody, UiTheme.TextSecondary, 14);
            EtiquetaEn(contenedor, $"Publisher    {juego.Publisher}", ref y, UiTheme.FontBody, UiTheme.TextSecondary, 14);
            EtiquetaEn(contenedor, $"Released    {juego.FechaPublicacion:d MMM, yyyy}", ref y, UiTheme.FontBody, UiTheme.TextSecondary, 14);
            y += 10;

            EtiquetaMultilinea(contenedor, juego.Descripcion, ref y, 370);
            y += 10;

            // --- Tags ---
            if (juego.TagsLista.Count > 0)
            {
                EtiquetaEn(contenedor, "TAGS", ref y, UiTheme.FontSubtitle, UiTheme.TextPrimary, 14);
                var flowTags = new FlowLayoutPanel { Location = new Point(14, y), Width = 370, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
                foreach (var tag in juego.TagsLista)
                {
                    flowTags.Controls.Add(new Label
                    {
                        Text = tag,
                        ForeColor = UiTheme.AccentBlue,
                        BackColor = UiTheme.PanelMid,
                        Padding = new Padding(8, 5, 8, 5),
                        Margin = new Padding(0, 0, 8, 8),
                        AutoSize = true
                    });
                }
                contenedor.Controls.Add(flowTags);
                y += flowTags.Height + 16;
            }

            // --- Reviews ---
            EtiquetaEn(contenedor, "REVIEWS", ref y, UiTheme.FontSubtitle, UiTheme.TextPrimary, 14);
            var panelReviews = new Panel { Location = new Point(14, y), Size = new Size(370, 40), BackColor = UiTheme.PanelMid };
            var colorReviews = juego.ReviewsPorcentaje >= 80 ? UiTheme.AccentGreen : (juego.ReviewsPorcentaje >= 60 ? UiTheme.AccentBlue : UiTheme.TextSecondary);
            var lblReviewsResumen = new Label
            {
                Text = juego.ReviewsCantidad > 0
                    ? $"ENGLISH REVIEWS   {juego.ReviewsResumen} ({juego.ReviewsPorcentaje}% of {juego.ReviewsCantidad:N0})"
                    : "Sin reseñas todavía",
                ForeColor = colorReviews,
                Location = new Point(10, 10),
                AutoSize = true
            };
            panelReviews.Controls.Add(lblReviewsResumen);
            contenedor.Controls.Add(panelReviews);
            y += 56;

            // --- Caja de compra ---
            var usuario = SessionManager.UsuarioActual!;
            bool yaLoTiene = _bibliotecaService.PoseeJuego(usuario.UsuarioId, juego.JuegoId);

            var cajaCompra = new Panel { Location = new Point(14, y), Size = new Size(370, 70), BackColor = UiTheme.PanelMid };
            var lblBuy = new Label { Text = $"Buy {juego.Titulo}", ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontSubtitle, Location = new Point(10, 8), AutoSize = true };
            var lblPrecioCaja = new Label { Text = juego.Precio.ToString("C") + " USD", ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontBody, Location = new Point(10, 40), AutoSize = true, BackColor = Color.Black, Padding = new Padding(6, 3, 6, 3) };

            _btnAgregarCarrito = UiTheme.CreateStyledButton(yaLoTiene ? "En tu biblioteca" : "Add to Cart", yaLoTiene ? UiTheme.PanelDark : UiTheme.AccentGreen);
            _btnAgregarCarrito.Location = new Point(230, 32);
            _btnAgregarCarrito.Size = new Size(130, 32);
            _btnAgregarCarrito.Enabled = !yaLoTiene;
            _btnAgregarCarrito.Click += BtnAgregarCarrito_Click;

            cajaCompra.Controls.AddRange(new Control[] { lblBuy, lblPrecioCaja, _btnAgregarCarrito });
            contenedor.Controls.Add(cajaCompra);
            y += 82;

            _lblMensaje.Location = new Point(14, y);
            _lblMensaje.AutoSize = true;
            _lblMensaje.MaximumSize = new Size(370, 0);
            contenedor.Controls.Add(_lblMensaje);
            y += 26;

            if (yaLoTiene)
            {
                var btnIrBiblioteca = UiTheme.CreateStyledButton("▶  Ir a mi biblioteca", UiTheme.AccentBlue);
                btnIrBiblioteca.Location = new Point(14, y);
                btnIrBiblioteca.Width = 370;
                btnIrBiblioteca.Click += (s, e) => _mainForm.MostrarVista(new LibraryView(_mainForm));
                contenedor.Controls.Add(btnIrBiblioteca);
                y += 46;
            }

            // --- Contenido maduro ---
            if (juego.TieneContenidoMaduro)
            {
                EtiquetaEn(contenedor, "Mature Content Description", ref y, UiTheme.FontSubtitle, UiTheme.TextPrimary, 14);
                EtiquetaEn(contenedor, "The developers describe the content like this:", ref y, UiTheme.FontBody, UiTheme.TextSecondary, 14);
                EtiquetaMultilinea(contenedor, juego.DescripcionContenidoMaduro, ref y, 370, italic: true);
                y += 10;
            }

            // --- Requisitos de sistema ---
            EtiquetaEn(contenedor, "System Requirements", ref y, UiTheme.FontSubtitle, UiTheme.TextPrimary, 14);
            EtiquetaEn(contenedor, "MINIMUM:", ref y, UiTheme.FontBody, UiTheme.TextPrimary, 14);
            EtiquetaMultilinea(contenedor, juego.RequisitosMinimos, ref y, 370);
            y += 6;
            EtiquetaEn(contenedor, "RECOMMENDED:", ref y, UiTheme.FontBody, UiTheme.TextPrimary, 14);
            EtiquetaMultilinea(contenedor, juego.RequisitosRecomendados, ref y, 370);
            y += 16;

            // --- Reportar juego ---
            var btnReportar = UiTheme.CreateStyledButton("🚩 Reportar este juego", UiTheme.PanelDark);
            btnReportar.ForeColor = UiTheme.TextSecondary;
            btnReportar.Location = new Point(14, y);
            btnReportar.Width = 370;
            btnReportar.Click += (s, e) => ReportarJuego(juego);
            contenedor.Controls.Add(btnReportar);
            y += 60;

            contenedor.Height = y;
            Controls.Add(contenedor);
        }

        private void BtnAgregarCarrito_Click(object? sender, EventArgs e)
        {
            var juego = _juegoService.ObtenerPorId(_juegoId)!;
            var usuario = SessionManager.UsuarioActual!;

            var (exito, mensaje) = _carritoService.AgregarAlCarrito(usuario.UsuarioId, juego);
            _lblMensaje.Text = mensaje;
            _lblMensaje.ForeColor = exito ? UiTheme.AccentGreen : UiTheme.AccentRed;

            if (exito)
            {
                _btnAgregarCarrito.Text = "Agregado ✓";
                _btnAgregarCarrito.Enabled = false;
                _mainForm.ActualizarBadgeCarrito();
            }
        }

        private void ReportarJuego(Juego juego)
        {
            using var dialogo = new Form
            {
                Text = "Reportar juego",
                Size = new Size(340, 220),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = UiTheme.PanelDark
            };

            var lbl = new Label { Text = $"¿Por qué reportas '{juego.Titulo}'?", ForeColor = UiTheme.TextPrimary, Location = new Point(15, 15), AutoSize = true };
            var txt = new TextBox { Location = new Point(15, 45), Width = 290, Height = 80, Multiline = true, BackColor = UiTheme.PanelMid, ForeColor = UiTheme.TextPrimary };
            var btnEnviar = UiTheme.CreateStyledButton("Enviar reporte", UiTheme.AccentRed);
            btnEnviar.Location = new Point(15, 135);
            btnEnviar.Width = 290;
            btnEnviar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    MessageBox.Show("Describe brevemente el motivo del reporte.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                _reporteService.ReportarJuego(SessionManager.UsuarioActual!, juego, txt.Text.Trim());
                MessageBox.Show("Gracias, tu reporte fue enviado al equipo de moderación.", "Reporte enviado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dialogo.Close();
            };

            dialogo.Controls.Add(lbl);
            dialogo.Controls.Add(txt);
            dialogo.Controls.Add(btnEnviar);
            dialogo.ShowDialog(this);
        }

        // ------------------------- Helpers de dibujo -------------------------

        private void DibujarHeader(Graphics g, Rectangle rect, string titulo)
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, UiTheme.AccentBlue, Color.FromArgb(20, 25, 35), 55f);
            g.FillRectangle(brush, rect);
            using var font = new Font("Segoe UI", 20, FontStyle.Bold);
            g.DrawString(titulo.ToUpper(), font, Brushes.White, 14, rect.Height - 50);
        }

        private Label EtiquetaEn(Control padre, string texto, ref int y, Font font, Color color, int x)
        {
            var lbl = new Label { Text = texto, Font = font, ForeColor = color, Location = new Point(x, y), AutoSize = true };
            padre.Controls.Add(lbl);
            y += lbl.PreferredHeight + 6;
            return lbl;
        }

        private void EtiquetaMultilinea(Control padre, string texto, ref int y, int ancho, bool italic = false)
        {
            var lbl = new Label
            {
                Text = texto,
                ForeColor = UiTheme.TextSecondary,
                Font = italic ? new Font("Segoe UI", 9, FontStyle.Italic) : UiTheme.FontBody,
                Location = new Point(14, y),
                MaximumSize = new Size(ancho, 0),
                AutoSize = true
            };
            padre.Controls.Add(lbl);
            y += lbl.PreferredHeight + 8;
        }
    }
}
