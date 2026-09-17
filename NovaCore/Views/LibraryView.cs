using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Biblioteca del usuario: lista de juegos adquiridos con su detalle.
    /// Layout vertical adaptable: lista arriba, detalle abajo con scroll.
    /// </summary>
    public class LibraryView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly JuegoService _juegoService = new();
        private readonly BibliotecaService _bibliotecaService = new();
        private readonly ListBox _listaJuegos = new();
        private readonly Panel _panelDetalle = new();

        // Estado de simulación de ejecución
        private System.Windows.Forms.Timer? _timerJuego;
        private BibliotecaItem? _itemActivo;
        private DateTime _inicioSesion;
        private Label _lblEstado = new();
        private Label _lblHoras = new();
        private Button _btnJugar = new();

        public LibraryView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = false;
            ConstruirUI();
            CargarLista();
        }

        private void ConstruirUI()
        {
            var tabla = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = UiTheme.BackgroundDark
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // título
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); // lista
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); // detalle

            var lblTitulo = new Label
            {
                Text = "Biblioteca",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Fill,
                Height = 45,
                Padding = new Padding(12, 10, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = UiTheme.BackgroundDark
            };

            _listaJuegos.Dock = DockStyle.Fill;
            _listaJuegos.BackColor = UiTheme.PanelDark;
            _listaJuegos.ForeColor = UiTheme.TextPrimary;
            _listaJuegos.BorderStyle = BorderStyle.None;
            _listaJuegos.Font = UiTheme.FontBody;
            _listaJuegos.IntegralHeight = false;
            _listaJuegos.DrawMode = DrawMode.OwnerDrawFixed;
            _listaJuegos.ItemHeight = 34;
            _listaJuegos.DrawItem += ListaJuegos_DrawItem;
            _listaJuegos.SelectedIndexChanged += (s, e) => MostrarDetalleSeleccionado();

            _panelDetalle.Dock = DockStyle.Fill;
            _panelDetalle.AutoScroll = true;
            _panelDetalle.BackColor = UiTheme.BackgroundDark;

            tabla.Controls.Add(lblTitulo, 0, 0);
            tabla.Controls.Add(_listaJuegos, 0, 1);
            tabla.Controls.Add(_panelDetalle, 0, 2);
            Controls.Add(tabla);
        }

        private void ListaJuegos_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var item = (BibliotecaItem)_listaJuegos.Items[e.Index];
            var juego = _juegoService.ObtenerPorId(item.JuegoId);

            bool seleccionado = e.State.HasFlag(DrawItemState.Selected);
            using var back = new SolidBrush(seleccionado ? UiTheme.AccentPrimary : UiTheme.PanelDark);
            e.Graphics.FillRectangle(back, e.Bounds);

            int textWidth = e.Bounds.Width - 40;
            if (textWidth < 20) textWidth = 20;
            var textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, textWidth, e.Bounds.Height);
            var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(UiTheme.TextPrimary);
            using var fuenteAjustada = new Font(UiTheme.FontBody.FontFamily, UiTheme.FontBody.SizeInPoints * UiTheme.EscalaUI, UiTheme.FontBody.Style);
            e.Graphics.DrawString(juego?.Titulo ?? "Juego eliminado", fuenteAjustada, textBrush, textRect, format);

            if (item.EnEjecucion)
            {
                using var dotBrush = new SolidBrush(UiTheme.AccentGreen);
                e.Graphics.FillEllipse(dotBrush, e.Bounds.Right - 20, e.Bounds.Y + 12, 8, 8);
            }
        }

        private void CargarLista()
        {
            var usuario = SessionManager.UsuarioActual!;
            var items = _bibliotecaService.ObtenerBibliotecaDeUsuario(usuario.UsuarioId);

            _listaJuegos.Items.Clear();
            foreach (var item in items) _listaJuegos.Items.Add(item);

            if (items.Count > 0) _listaJuegos.SelectedIndex = 0;
            else MostrarBibliotecaVacia();
        }

        private void MostrarBibliotecaVacia()
        {
            _panelDetalle.Controls.Clear();
            _panelDetalle.Controls.Add(new Label
            {
                Text = "Aún no tienes juegos. Visita la Tienda para comprar tu primer título.",
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(_panelDetalle.Width - 40, 0),
                Dock = DockStyle.Top,
                Padding = new Padding(20, 20, 20, 0)
            });
        }

        private void MostrarDetalleSeleccionado()
        {
            DetenerSimulacionSiCorresponde();
            _panelDetalle.Controls.Clear();

            if (_listaJuegos.SelectedItem is not BibliotecaItem item) return;
            var juego = _juegoService.ObtenerPorId(item.JuegoId);
            if (juego == null) return;

            _itemActivo = item;

            var tabla = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(16, 12, 16, 16),
                Margin = Padding.Empty
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 6; i++)
                tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTitulo = new Label
            {
                Text = juego.Titulo,
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8)
            };

            var lblCompra = new Label
            {
                Text = $"Adquirido el {item.FechaCompra:dd/MM/yyyy}",
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 4)
            };

            _lblHoras = new Label
            {
                Text = $"Horas jugadas: {item.HorasJugadas:0.0} h",
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 4)
            };

            _lblEstado = new Label
            {
                Text = item.EnEjecucion ? "● En ejecución" : "○ Detenido",
                ForeColor = item.EnEjecucion ? UiTheme.AccentGreen : UiTheme.TextSecondary,
                Font = UiTheme.FontSubtitle,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10)
            };

            _btnJugar = UiTheme.CreateStyledButton(
                item.EnEjecucion ? "Detener" : "▶  Jugar",
                item.EnEjecucion ? UiTheme.AccentRed : UiTheme.AccentGreen);
            _btnJugar.Dock = DockStyle.Fill;
            _btnJugar.MinimumSize = new Size(0, 38);
            _btnJugar.Margin = new Padding(0, 0, 0, 14);
            _btnJugar.Click += BtnJugar_Click;

            var lblDesc = new Label
            {
                Text = juego.Descripcion,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Dock = DockStyle.Fill,
                MaximumSize = new Size(int.MaxValue / 2, 0),
                Margin = Padding.Empty
            };

            tabla.Controls.Add(lblTitulo, 0, 0);
            tabla.Controls.Add(lblCompra, 0, 1);
            tabla.Controls.Add(_lblHoras, 0, 2);
            tabla.Controls.Add(_lblEstado, 0, 3);
            tabla.Controls.Add(_btnJugar, 0, 4);
            tabla.Controls.Add(lblDesc, 0, 5);

            _panelDetalle.Controls.Add(tabla);
        }

        private void BtnJugar_Click(object? sender, EventArgs e)
        {
            if (_itemActivo == null) return;

            if (!_itemActivo.EnEjecucion)
                IniciarSimulacion();
            else
                DetenerSimulacionSiCorresponde();
        }

        private void IniciarSimulacion()
        {
            if (_itemActivo == null) return;

            _itemActivo.EnEjecucion = true;
            _inicioSesion = DateTime.Now;

            _lblEstado.Text = "● En ejecución";
            _lblEstado.ForeColor = UiTheme.AccentGreen;
            _btnJugar.Text = "Detener";
            _btnJugar.BackColor = UiTheme.AccentRed;
            _listaJuegos.Invalidate();

            _timerJuego = new System.Windows.Forms.Timer { Interval = 1000 };
            _timerJuego.Tick += (s, e) =>
            {
                _bibliotecaService.RegistrarSesionJugada(_itemActivo.BibliotecaId, 0.1);
                _lblHoras.Text = $"Horas jugadas: {_itemActivo.HorasJugadas:0.0} h";
            };
            _timerJuego.Start();
        }

        private void DetenerSimulacionSiCorresponde()
        {
            if (_timerJuego != null)
            {
                _timerJuego.Stop();
                _timerJuego.Dispose();
                _timerJuego = null;
            }

            if (_itemActivo != null && _itemActivo.EnEjecucion)
            {
                _itemActivo.EnEjecucion = false;
                if (_lblEstado != null)
                {
                    _lblEstado.Text = "○ Detenido";
                    _lblEstado.ForeColor = UiTheme.TextSecondary;
                }
                if (_btnJugar != null)
                {
                    _btnJugar.Text = "▶  Jugar";
                    _btnJugar.BackColor = UiTheme.AccentGreen;
                }
                _listaJuegos.Invalidate();
            }
        }
    }
}
