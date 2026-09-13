using SteamCloneApp.Models;
using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>
    /// Formulario para crear o editar un juego, con todos los campos ampliados
    /// (publisher, tags, reviews, requisitos mínimos/recomendados, contenido
    /// maduro) para poder alimentar la ficha de detalle estilo Steam.
    /// </summary>
    public class GameFormView : UserControl
    {
        private readonly JuegoService _juegoService;
        private readonly Juego? _juegoExistente;

        private readonly TextBox _txtTitulo = new();
        private readonly TextBox _txtDescripcion = new() { Multiline = true };
        private readonly TextBox _txtPrecio = new();
        private readonly ComboBox _cmbCategoria = new();
        private readonly TextBox _txtDesarrollador = new();
        private readonly TextBox _txtPublisher = new();
        private readonly TextBox _txtTags = new();
        private readonly TextBox _txtImagenRuta = new();
        private readonly ComboBox _cmbReviewsResumen = new();
        private readonly TextBox _txtReviewsPorcentaje = new();
        private readonly TextBox _txtReviewsCantidad = new();
        private readonly TextBox _txtRequisitosMin = new() { Multiline = true };
        private readonly TextBox _txtRequisitosRec = new() { Multiline = true };
        private readonly CheckBox _chkContenidoMaduro = new();
        private readonly TextBox _txtDescripcionMaduro = new() { Multiline = true };
        private readonly Label _lblError = new();

        public event EventHandler? GuardadoExitoso;

        public GameFormView(JuegoService juegoService, Juego? juegoExistente)
        {
            _juegoService = juegoService;
            _juegoExistente = juegoExistente;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            ConstruirUI();
            if (_juegoExistente != null) CargarDatosExistentes();
        }

        private void ConstruirUI()
        {
            var lblTitulo = new Label
            {
                Text = _juegoExistente == null ? "Nuevo Juego" : $"Editar: {_juegoExistente.Titulo}",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                Location = new Point(14, 10),
                AutoSize = true
            };

            int y = 55;
            CampoLabel("Título *", ref y);
            _txtTitulo.Location = new Point(14, y); _txtTitulo.Width = 370; Estilizar(_txtTitulo); y += 38;

            CampoLabel("Categoría *", ref y);
            _cmbCategoria.Location = new Point(14, y); _cmbCategoria.Width = 200;
            _cmbCategoria.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbCategoria.BackColor = UiTheme.PanelMid; _cmbCategoria.ForeColor = UiTheme.TextPrimary; _cmbCategoria.FlatStyle = FlatStyle.Flat;
            foreach (var cat in _juegoService.ObtenerCategorias()) _cmbCategoria.Items.Add(cat);
            y += 38;

            CampoLabel("Precio (USD) *", ref y);
            _txtPrecio.Location = new Point(14, y); _txtPrecio.Width = 130; Estilizar(_txtPrecio); y += 38;

            CampoLabel("Desarrollador", ref y);
            _txtDesarrollador.Location = new Point(14, y); _txtDesarrollador.Width = 370; Estilizar(_txtDesarrollador); y += 38;

            CampoLabel("Publisher", ref y);
            _txtPublisher.Location = new Point(14, y); _txtPublisher.Width = 370; Estilizar(_txtPublisher); y += 38;

            CampoLabel("Tags (separados por coma)", ref y);
            _txtTags.Location = new Point(14, y); _txtTags.Width = 370; Estilizar(_txtTags); y += 38;

            CampoLabel("Ruta local o URL de portada", ref y);
            _txtImagenRuta.Location = new Point(14, y); _txtImagenRuta.Width = 370; Estilizar(_txtImagenRuta); y += 38;

            CampoLabel("Descripción", ref y);
            _txtDescripcion.Location = new Point(14, y); _txtDescripcion.Size = new Size(370, 60); Estilizar(_txtDescripcion); y += 70;

            CampoLabel("Resumen de reviews", ref y);
            _cmbReviewsResumen.Location = new Point(14, y); _cmbReviewsResumen.Width = 200;
            _cmbReviewsResumen.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbReviewsResumen.BackColor = UiTheme.PanelMid; _cmbReviewsResumen.ForeColor = UiTheme.TextPrimary; _cmbReviewsResumen.FlatStyle = FlatStyle.Flat;
            _cmbReviewsResumen.Items.AddRange(new object[] { "Sin reseñas", "Mixed", "Positive", "Very Positive", "Overwhelmingly Positive" });
            y += 38;

            CampoLabel("% Positivas", ref y);
            _txtReviewsPorcentaje.Location = new Point(14, y); _txtReviewsPorcentaje.Width = 90; Estilizar(_txtReviewsPorcentaje);
            CampoLabelEnLinea("Cantidad de reviews", 130, y - 22);
            _txtReviewsCantidad.Location = new Point(130, y); _txtReviewsCantidad.Width = 120; Estilizar(_txtReviewsCantidad);
            y += 38;

            CampoLabel("Requisitos mínimos", ref y);
            _txtRequisitosMin.Location = new Point(14, y); _txtRequisitosMin.Size = new Size(370, 70); Estilizar(_txtRequisitosMin); y += 80;

            CampoLabel("Requisitos recomendados", ref y);
            _txtRequisitosRec.Location = new Point(14, y); _txtRequisitosRec.Size = new Size(370, 70); Estilizar(_txtRequisitosRec); y += 80;

            _chkContenidoMaduro.Text = "Contiene contenido maduro";
            _chkContenidoMaduro.ForeColor = UiTheme.TextPrimary;
            _chkContenidoMaduro.Location = new Point(14, y);
            _chkContenidoMaduro.AutoSize = true;
            _chkContenidoMaduro.CheckedChanged += (s, e) => _txtDescripcionMaduro.Enabled = _chkContenidoMaduro.Checked;
            y += 30;

            CampoLabel("Descripción del contenido maduro", ref y);
            _txtDescripcionMaduro.Location = new Point(14, y); _txtDescripcionMaduro.Size = new Size(370, 50); Estilizar(_txtDescripcionMaduro); y += 60;

            _lblError.Location = new Point(14, y);
            _lblError.ForeColor = UiTheme.AccentRed;
            _lblError.AutoSize = true;
            _lblError.MaximumSize = new Size(370, 0);
            y += 30;

            var btnGuardar = UiTheme.CreateStyledButton("Guardar", UiTheme.AccentBlue);
            btnGuardar.Location = new Point(14, y); btnGuardar.Width = 370;
            btnGuardar.Click += BtnGuardar_Click;
            y += 60;

            Controls.AddRange(new Control[]
            {
                lblTitulo, _txtTitulo, _cmbCategoria, _txtPrecio, _txtDesarrollador, _txtPublisher, _txtTags,
                _txtImagenRuta, _txtDescripcion, _cmbReviewsResumen, _txtReviewsPorcentaje, _txtReviewsCantidad,
                _txtRequisitosMin, _txtRequisitosRec, _chkContenidoMaduro, _txtDescripcionMaduro, _lblError, btnGuardar
            });

            AutoScrollMinSize = new Size(400, y);
        }

        private void CampoLabel(string texto, ref int y)
        {
            var lbl = new Label { Text = texto, ForeColor = UiTheme.TextSecondary, Location = new Point(14, y), AutoSize = true };
            Controls.Add(lbl);
            y += 20;
        }

        private void CampoLabelEnLinea(string texto, int x, int y)
        {
            var lbl = new Label { Text = texto, ForeColor = UiTheme.TextSecondary, Location = new Point(x, y), AutoSize = true };
            Controls.Add(lbl);
        }

        private void CargarDatosExistentes()
        {
            var j = _juegoExistente!;
            _txtTitulo.Text = j.Titulo;
            _txtDescripcion.Text = j.Descripcion;
            _txtPrecio.Text = j.Precio.ToString("0.00");
            _txtDesarrollador.Text = j.Desarrollador;
            _txtPublisher.Text = j.Publisher;
            _txtTags.Text = j.Tags;
            _txtImagenRuta.Text = j.ImagenPortada ?? string.Empty;
            _cmbReviewsResumen.SelectedItem = j.ReviewsResumen;
            _txtReviewsPorcentaje.Text = j.ReviewsPorcentaje.ToString();
            _txtReviewsCantidad.Text = j.ReviewsCantidad.ToString();
            _txtRequisitosMin.Text = j.RequisitosMinimos;
            _txtRequisitosRec.Text = j.RequisitosRecomendados;
            _chkContenidoMaduro.Checked = j.TieneContenidoMaduro;
            _txtDescripcionMaduro.Text = j.DescripcionContenidoMaduro;
            _txtDescripcionMaduro.Enabled = j.TieneContenidoMaduro;

            foreach (var item in _cmbCategoria.Items)
                if (item is Categoria c && c.CategoriaId == j.CategoriaId) { _cmbCategoria.SelectedItem = item; break; }
        }

        private void BtnGuardar_Click(object? sender, EventArgs e)
        {
            _lblError.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(_txtTitulo.Text)) { _lblError.Text = "El título es obligatorio."; return; }
            if (_cmbCategoria.SelectedItem is not Categoria categoria) { _lblError.Text = "Selecciona una categoría."; return; }
            if (!decimal.TryParse(_txtPrecio.Text, out var precio) || precio < 0) { _lblError.Text = "Ingresa un precio válido (ej: 19.99)."; return; }

            int.TryParse(_txtReviewsPorcentaje.Text, out var porcentaje);
            int.TryParse(_txtReviewsCantidad.Text, out var cantidad);

            var juego = new Juego
            {
                JuegoId = _juegoExistente?.JuegoId ?? 0,
                Titulo = _txtTitulo.Text.Trim(),
                Descripcion = _txtDescripcion.Text.Trim(),
                Precio = precio,
                CategoriaId = categoria.CategoriaId,
                Desarrollador = _txtDesarrollador.Text.Trim(),
                Publisher = _txtPublisher.Text.Trim(),
                Tags = _txtTags.Text.Trim(),
                ImagenPortada = string.IsNullOrWhiteSpace(_txtImagenRuta.Text) ? null : _txtImagenRuta.Text.Trim(),
                ReviewsResumen = _cmbReviewsResumen.SelectedItem?.ToString() ?? "Sin reseñas",
                ReviewsPorcentaje = Math.Clamp(porcentaje, 0, 100),
                ReviewsCantidad = Math.Max(0, cantidad),
                RequisitosMinimos = _txtRequisitosMin.Text.Trim(),
                RequisitosRecomendados = _txtRequisitosRec.Text.Trim(),
                TieneContenidoMaduro = _chkContenidoMaduro.Checked,
                DescripcionContenidoMaduro = _txtDescripcionMaduro.Text.Trim()
            };

            if (_juegoExistente == null)
                _juegoService.Crear(juego);
            else
                _juegoService.Actualizar(juego);

            GuardadoExitoso?.Invoke(this, EventArgs.Empty);
            MessageBox.Show("Juego guardado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void Estilizar(Control ctrl)
        {
            ctrl.BackColor = UiTheme.PanelMid;
            ctrl.ForeColor = UiTheme.TextPrimary;
            if (ctrl is TextBox txt) txt.BorderStyle = BorderStyle.FixedSingle;
            ctrl.Font = UiTheme.FontBody;
        }
    }
}
