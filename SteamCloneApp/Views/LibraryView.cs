using SteamCloneApp.Models;
using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>
    /// Biblioteca del usuario: lista lateral de juegos adquiridos. Al seleccionar
    /// uno se muestra su detalle con un botón "Jugar" que simula ejecución y
    /// acumula horas jugadas mientras el estado está "En ejecución".
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
            ConstruirUI();
            CargarLista();
        }

        private void ConstruirUI()
        {
            var lblTitulo = new Label
            {
                Text = "Biblioteca",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 45
            };

            _listaJuegos.Dock = DockStyle.Left;
            _listaJuegos.Width = 260;
            _listaJuegos.BackColor = UiTheme.PanelDark;
            _listaJuegos.ForeColor = UiTheme.TextPrimary;
            _listaJuegos.BorderStyle = BorderStyle.None;
            _listaJuegos.Font = UiTheme.FontBody;
            _listaJuegos.ItemHeight = 32;
            _listaJuegos.DrawMode = DrawMode.OwnerDrawFixed;
            _listaJuegos.DrawItem += ListaJuegos_DrawItem;
            _listaJuegos.SelectedIndexChanged += (s, e) => MostrarDetalleSeleccionado();

            _panelDetalle.Dock = DockStyle.Fill;
            _panelDetalle.Padding = new Padding(20, 0, 0, 0);

            Controls.Add(_panelDetalle);
            Controls.Add(_listaJuegos);
            Controls.Add(lblTitulo);
        }

        private void ListaJuegos_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var item = (BibliotecaItem)_listaJuegos.Items[e.Index];
            var juego = _juegoService.ObtenerPorId(item.JuegoId);

            bool seleccionado = e.State.HasFlag(DrawItemState.Selected);
            using var back = new SolidBrush(seleccionado ? UiTheme.AccentBlue : UiTheme.PanelDark);
            e.Graphics.FillRectangle(back, e.Bounds);

            using var textBrush = new SolidBrush(UiTheme.TextPrimary);
            e.Graphics.DrawString(juego?.Titulo ?? "Juego eliminado", UiTheme.FontBody, textBrush, e.Bounds.X + 10, e.Bounds.Y + 7);

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
                Location = new Point(20, 20)
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

            var lblTitulo = new Label { Text = juego.Titulo, Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Location = new Point(20, 20), AutoSize = true };
            var lblCompra = new Label { Text = $"Adquirido el {item.FechaCompra:dd/MM/yyyy}", ForeColor = UiTheme.TextSecondary, Location = new Point(20, 60), AutoSize = true };

            _lblHoras = new Label
            {
                Text = $"Horas jugadas: {item.HorasJugadas:0.0} h",
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(20, 85),
                AutoSize = true
            };

            _lblEstado = new Label
            {
                Text = item.EnEjecucion ? "● En ejecución" : "○ Detenido",
                ForeColor = item.EnEjecucion ? UiTheme.AccentGreen : UiTheme.TextSecondary,
                Font = UiTheme.FontSubtitle,
                Location = new Point(20, 115),
                AutoSize = true
            };

            _btnJugar = UiTheme.CreateStyledButton(item.EnEjecucion ? "Detener" : "▶  Jugar", item.EnEjecucion ? UiTheme.AccentRed : UiTheme.AccentGreen);
            _btnJugar.Location = new Point(20, 150);
            _btnJugar.Width = 180;
            _btnJugar.Click += BtnJugar_Click;

            var lblDesc = new Label { Text = juego.Descripcion, ForeColor = UiTheme.TextSecondary, Location = new Point(20, 200), Size = new Size(500, 80) };

            _panelDetalle.Controls.AddRange(new Control[] { lblTitulo, lblCompra, _lblHoras, _lblEstado, _btnJugar, lblDesc });
        }

        private void BtnJugar_Click(object? sender, EventArgs e)
        {
            if (_itemActivo == null) return;

            if (!_itemActivo.EnEjecucion)
                IniciarSimulacion();
            else
                DetenerSimulacionSiCorresponde();
        }

        /// <summary>Simula el inicio de un juego: cambia el estado y arranca un timer
        /// que suma horas jugadas cada segundo (a escala acelerada para fines demostrativos).</summary>
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

            _timerJuego = new System.Windows.Forms.Timer { Interval = 1000 }; // 1 segundo real = 0.1 h simuladas
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
