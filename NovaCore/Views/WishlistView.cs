using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Lista de deseos del usuario: muestra los juegos guardados con el botón
    /// "♡ Añadir a la lista de deseos" de la ficha de cada juego. Permite
    /// agregarlos al carrito o quitarlos de la lista.
    /// </summary>
    public class WishlistView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly DeseoService _deseoService = new();
        private readonly JuegoService _juegoService = new();
        private readonly CarritoService _carritoService = new();
        private readonly BibliotecaService _bibliotecaService = new();
        private readonly Panel _barraSuperior;

        public WishlistView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;

            _barraSuperior = UiTheme.CreateBackBar("Lista de deseos", () => _mainForm.MostrarVista(new StoreView(_mainForm)));
            Reconstruir(null);
        }

        /// <summary>Reconstruye la página completa desde cero cada vez que cambia la lista.</summary>
        private void Reconstruir(string? mensaje, bool esError = false)
        {
            Controls.Clear();
            Controls.Add(_barraSuperior);

            var usuario = SessionManager.UsuarioActual!;
            var deseos = _deseoService.ObtenerDeseosDeUsuario(usuario.UsuarioId);

            int y = 64;

            if (deseos.Count == 0)
            {
                Controls.Add(new Label
                {
                    Text = "Tu lista de deseos está vacía.\nEntra a un juego y toca \"♡ Añadir a la lista de deseos\" para guardarlo aquí.",
                    ForeColor = UiTheme.TextSecondary,
                    Location = new Point(14, y),
                    AutoSize = true,
                    MaximumSize = new Size(370, 0)
                });
                return;
            }

            foreach (var deseo in deseos)
            {
                var juego = _juegoService.ObtenerPorId(deseo.JuegoId);
                if (juego == null) continue;

                var card = new Panel { Location = new Point(14, y), Size = new Size(370, 76), BackColor = UiTheme.PanelDark };

                var lblTitulo = new Label
                {
                    Text = juego.Titulo,
                    ForeColor = UiTheme.TextPrimary,
                    Font = UiTheme.FontSubtitle,
                    Location = new Point(10, 8),
                    AutoSize = true,
                    MaximumSize = new Size(196, 0)
                };

                var lblPrecio = new Label
                {
                    Text = juego.Precio.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) + " USD",
                    ForeColor = UiTheme.AccentGreen,
                    Location = new Point(10, 48),
                    AutoSize = true
                };

                bool yaLoTiene = _bibliotecaService.PoseeJuego(usuario.UsuarioId, juego.JuegoId);
                var btnCarrito = UiTheme.CreateStyledButton(yaLoTiene ? "En biblioteca" : "🛒 Al carrito", yaLoTiene ? UiTheme.PanelDark : UiTheme.AccentGreen);
                btnCarrito.Size = new Size(112, 30);
                btnCarrito.Location = new Point(214, 22);
                btnCarrito.Enabled = !yaLoTiene;

                var juegoId = juego.JuegoId; // captura local para el closure
                btnCarrito.Click += (s, e) =>
                {
                    var (exito, msg) = _carritoService.AgregarAlCarrito(usuario.UsuarioId, juego);
                    _mainForm.ActualizarBadgeCarrito();
                    Reconstruir(msg, !exito);
                };

                var btnQuitar = new Button
                {
                    Text = "🗑",
                    Size = new Size(34, 30),
                    Location = new Point(330, 22),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = UiTheme.PanelMid,
                    ForeColor = UiTheme.AccentRed,
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                btnQuitar.FlatAppearance.BorderSize = 0;
                btnQuitar.Click += (s, e) =>
                {
                    _deseoService.QuitarDeDeseos(usuario.UsuarioId, juegoId);
                    Reconstruir("Juego quitado de tu lista de deseos.");
                };

                card.Controls.AddRange(new Control[] { lblTitulo, lblPrecio, btnCarrito, btnQuitar });
                Controls.Add(card);
                y += 84;
            }

            if (!string.IsNullOrEmpty(mensaje))
            {
                Controls.Add(new Label
                {
                    Text = mensaje,
                    ForeColor = esError ? UiTheme.AccentRed : UiTheme.AccentGreen,
                    Location = new Point(14, y),
                    AutoSize = true,
                    MaximumSize = new Size(370, 0)
                });
            }
        }
    }
}
