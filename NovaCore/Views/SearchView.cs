using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Página de búsqueda dedicada (reemplaza al cuadro de texto diminuto que
    /// vivía dentro del header, poco confiable). Filtra el catálogo en tiempo
    /// real a medida que se escribe y cada resultado lleva al detalle del juego.
    /// </summary>
    public class SearchView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly JuegoService _juegoService = new();
        private readonly RecomendacionService _recomendacionService = new();
        private readonly TextBox _txtBuscar = new();
        private readonly FlowLayoutPanel _flowResultados = new();

        public SearchView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.PanelDark };

            var btnVolver = new Button
            {
                Text = "←",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(8, 13),
                Size = new Size(34, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.PanelDark,
                ForeColor = UiTheme.TextPrimary
            };
            btnVolver.FlatAppearance.BorderSize = 0;
            btnVolver.Click += (s, e) => _mainForm.MostrarVista(new StoreView(_mainForm));

            _txtBuscar.Location = new Point(50, 15);
            _txtBuscar.Width = 330;
            _txtBuscar.Font = new Font("Segoe UI", 11);
            _txtBuscar.BackColor = UiTheme.PanelMid;
            _txtBuscar.ForeColor = UiTheme.TextPrimary;
            _txtBuscar.BorderStyle = BorderStyle.FixedSingle;
            _txtBuscar.PlaceholderText = "Buscar juegos...";
            _txtBuscar.TextChanged += (s, e) => CargarResultados();

            header.Controls.Add(btnVolver);
            header.Controls.Add(_txtBuscar);
            Controls.Add(header);

            _flowResultados.Location = new Point(0, 64);
            _flowResultados.Width = 400;
            _flowResultados.FlowDirection = FlowDirection.TopDown;
            _flowResultados.WrapContents = false;
            _flowResultados.AutoSize = true;
            _flowResultados.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Controls.Add(_flowResultados);

            CargarResultados();

            // Foco inmediato en el cuadro de búsqueda al abrir la página.
            this.HandleCreated += (s, e) => _txtBuscar.Focus();
        }

        private void CargarResultados()
        {
            _flowResultados.Controls.Clear();

            var texto = _txtBuscar.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(texto))
            {
                _flowResultados.Controls.Add(new Label
                {
                    Text = "Escribe el nombre de un juego para buscar.",
                    ForeColor = UiTheme.TextSecondary,
                    Location = new Point(14, 20),
                    AutoSize = true
                });
                return;
            }

            var resultados = _juegoService.ObtenerCatalogo(texto, null);

            if (resultados.Count == 0)
            {
                _flowResultados.Controls.Add(new Label
                {
                    Text = $"No se encontraron juegos para \"{texto}\".",
                    ForeColor = UiTheme.TextSecondary,
                    Location = new Point(14, 20),
                    AutoSize = true
                });
                return;
            }

            foreach (var juego in resultados)
                _flowResultados.Controls.Add(CrearFilaResultado(juego));
        }

        private Panel CrearFilaResultado(Juego juego)
        {
            var fila = new Panel { Size = new Size(376, 74), Margin = new Padding(12, 0, 12, 10), BackColor = UiTheme.PanelDark, Cursor = Cursors.Hand };

            var miniPortada = new Panel { Size = new Size(64, 54), Location = new Point(10, 10) };
            miniPortada.Paint += (s, e) =>
            {
                var imagen = UiTheme.CargarImagen(juego.ImagenPortada);
                if (imagen != null)
                {
                    UiTheme.DibujarImagenRecortada(e.Graphics, miniPortada.ClientRectangle, imagen);
                    return;
                }

                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(miniPortada.ClientRectangle, UiTheme.AccentPrimary, UiTheme.PanelMid, 45f);
                e.Graphics.FillRectangle(brush, miniPortada.ClientRectangle);
            };

            var lblTitulo = new Label { Text = juego.Titulo, ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontSubtitle, Location = new Point(84, 12), AutoSize = true };
            var lblCategoria = new Label { Text = juego.CategoriaNombre, ForeColor = UiTheme.TextSecondary, Location = new Point(84, 36), AutoSize = true };
            var lblPrecio = new Label { Text = juego.Precio.ToString("C"), ForeColor = UiTheme.AccentGreen, Font = UiTheme.FontSubtitle, Location = new Point(fila.Width - 80, 24), AutoSize = true };

            fila.Controls.AddRange(new Control[] { miniPortada, lblTitulo, lblCategoria, lblPrecio });

            void AbrirDetalle(object? s, EventArgs e)
            {
                var usuario = SessionManager.UsuarioActual;
                if (usuario != null)
                    _recomendacionService.RegistrarBusqueda(usuario.UsuarioId, _txtBuscar.Text?.Trim() ?? string.Empty);
                _mainForm.MostrarVista(new GameDetailView(_mainForm, juego.JuegoId));
            }
            fila.Click += AbrirDetalle;
            miniPortada.Click += AbrirDetalle;
            lblTitulo.Click += AbrirDetalle;
            lblCategoria.Click += AbrirDetalle;

            return fila;
        }
    }
}
