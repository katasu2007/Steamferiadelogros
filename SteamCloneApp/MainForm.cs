using SteamCloneApp.Services;
using SteamCloneApp.Views;

namespace SteamCloneApp
{
    /// <summary>
    /// Shell principal, estilo app móvil de Steam dentro de una ventana de
    /// tamaño fijo. Estructura:
    ///   - Header: ícono STEAMCLONE, buscador, carrito, kebab y avatar + tabs
    ///   - ContentPanel: aquí se intercambian los UserControls (páginas)
    ///   - BottomNav: 5 íconos fijos (Tienda, Noticias, Biblioteca, Notificaciones, Menú)
    /// El menú (MenuPageView) se comporta como un "toggle": si ya está abierto,
    /// volver a presionar el mismo botón lo cierra y regresa a la vista anterior.
    /// </summary>
    public class MainForm : Form
    {
        // Header
        private readonly Panel _headerPanel = new();
        private readonly PictureBox _avatarTopBar = new();
        private readonly Button _btnCart = new();

        // Tabs (MENU / WISHLIST / WALLET)
        private Button _btnTabMenu = new();

        // Contenido
        private readonly Panel _contentPanel = new();

        // Bottom nav
        private readonly Panel _bottomNav = new();
        private readonly Dictionary<string, Button> _botonesNav = new();
        private string _navActiva = "store";

        // --- Estado del toggle del menú (kebab / "MENU ▾" / ☰) ---
        private bool _menuAbierto = false;
        private UserControl? _vistaAntesDelMenu;
        private string _navActivaAntesDelMenu = "store";

        public MainForm()
        {
            ConfigurarFormulario();
            ConstruirContentPanel();
            ConstruirHeader();
            ConstruirBottomNav();

            MostrarLogin();
        }

        private void ConfigurarFormulario()
        {
            Text = "SteamClone";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(400, 820); // proporción tipo teléfono
            BackColor = UiTheme.BackgroundDark;
            Font = UiTheme.FontBody;
        }

        // ============================= HEADER =============================

        private void ConstruirHeader()
        {
            _headerPanel.Dock = DockStyle.Top;
            _headerPanel.Height = 96;
            _headerPanel.BackColor = UiTheme.PanelDark;

            // Posiciones calculadas de derecha a izquierda para garantizar que
            // ningún control se superponga con otro.
            const int margen = 12;
            const int anchoIcono = 34;
            const int separacion = 8;

            int xAvatar = 400 - margen - anchoIcono;                 // avatar, extremo derecho
            int xKebab = xAvatar - separacion - anchoIcono;          // kebab (⋮)
            int xCarrito = xKebab - separacion - anchoIcono;         // carrito (🛒)
            int xBuscar = xCarrito - separacion - anchoIcono;        // lupa (🔍)
            int anchoMarca = xBuscar - separacion - margen;          // resto: marca "STEAMCLONE"

            var lblMarca = new Label
            {
                Text = "🎮 STEAMCLONE",
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(margen, 20),
                Size = new Size(anchoMarca, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnBuscar = CrearBotonIcono("🔍", xBuscar, anchoIcono);
            btnBuscar.Click += (s, e) => MostrarVista(new SearchView(this));

            _btnCart.Text = "🛒";
            _btnCart.Size = new Size(anchoIcono, anchoIcono);
            _btnCart.Location = new Point(xCarrito, 10);
            _btnCart.Font = new Font("Segoe UI Emoji", 12);
            _btnCart.FlatStyle = FlatStyle.Flat;
            _btnCart.BackColor = UiTheme.PanelDark;
            _btnCart.ForeColor = UiTheme.AccentGreen;
            _btnCart.FlatAppearance.BorderSize = 0;
            _btnCart.Click += (s, e) => MostrarVista(new CartView(this));

            var btnKebab = CrearBotonIcono("⋮", xKebab, anchoIcono);
            btnKebab.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            btnKebab.Click += (s, e) => AlternarMenu();

            _avatarTopBar.Size = new Size(anchoIcono, anchoIcono);
            _avatarTopBar.Location = new Point(xAvatar, 10);
            _avatarTopBar.SizeMode = PictureBoxSizeMode.StretchImage;
            _avatarTopBar.Cursor = Cursors.Hand;
            _avatarTopBar.Click += (s, e) => { if (SessionManager.HaySesionActiva) MostrarVista(new ProfileView(this)); };

            // --- Fila 2: tabs MENU / WISHLIST / WALLET ---
            _btnTabMenu = CrearBotonTab("MENU ▾", 12);
            var btnTabWishlist = CrearBotonTab("WISHLIST", 130);
            var btnTabWallet = CrearBotonTab("WALLET", 260);
            _btnTabMenu.Click += (s, e) => AlternarMenu();
            btnTabWishlist.Click += (s, e) => MostrarVista(new WishlistView(this));
            btnTabWallet.Click += (s, e) => MostrarVista(new WalletView(this));

            _headerPanel.Controls.Add(lblMarca);
            _headerPanel.Controls.Add(btnBuscar);
            _headerPanel.Controls.Add(_btnCart);
            _headerPanel.Controls.Add(btnKebab);
            _headerPanel.Controls.Add(_avatarTopBar);
            _headerPanel.Controls.Add(_btnTabMenu);
            _headerPanel.Controls.Add(btnTabWishlist);
            _headerPanel.Controls.Add(btnTabWallet);

            Controls.Add(_headerPanel);
        }

        private Button CrearBotonIcono(string texto, int x, int ancho)
        {
            var btn = new Button
            {
                Text = texto,
                Location = new Point(x, 10),
                Size = new Size(ancho, ancho),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.PanelDark,
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI Emoji", 12)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CrearBotonTab(string texto, int x)
        {
            var btn = new Button
            {
                Text = texto,
                Location = new Point(x, 56),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.PanelDark,
                ForeColor = UiTheme.TextSecondary,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        /// <summary>Actualiza el avatar del top bar leyendo la ruta guardada del usuario activo.</summary>
        public void ActualizarAvatarTopBar()
        {
            _avatarTopBar.Image?.Dispose();
            _avatarTopBar.Image = null;
            _avatarTopBar.BackColor = UiTheme.AccentBlue;

            var usuario = SessionManager.UsuarioActual;
            if (usuario == null) return;

            if (!string.IsNullOrWhiteSpace(usuario.AvatarPath) && File.Exists(usuario.AvatarPath))
            {
                try { _avatarTopBar.Image = Image.FromFile(usuario.AvatarPath); return; }
                catch { /* si falla, se deja el color de fondo como placeholder */ }
            }
        }

        public void ActualizarBadgeCarrito()
        {
            if (SessionManager.UsuarioActual == null) return;
            var carrito = new CarritoService().ObtenerCarrito(SessionManager.UsuarioActual.UsuarioId);
            _btnCart.Text = carrito.Count > 0 ? $"🛒{carrito.Count}" : "🛒";
        }

        // ============================= CONTENIDO =============================

        private void ConstruirContentPanel()
        {
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = UiTheme.BackgroundDark;
            Controls.Add(_contentPanel);
        }

        /// <summary>
        /// Punto único de navegación. Si la vista que se está mostrando NO es el
        /// menú, se considera que el usuario "navegó lejos" y el estado de
        /// menú-abierto se resetea (así, si luego vuelve a tocar el kebab, se
        /// abre de nuevo en vez de quedar en un estado inconsistente).
        /// </summary>
        public void MostrarVista(UserControl vista)
        {
            _menuAbierto = vista is MenuPageView;

            _contentPanel.Controls.Clear();
            vista.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(vista);
        }

        // ============================= BOTTOM NAV =============================

        private void ConstruirBottomNav()
        {
            _bottomNav.Dock = DockStyle.Bottom;
            _bottomNav.Height = 58;
            _bottomNav.BackColor = UiTheme.PanelDark;

            var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
            for (int i = 0; i < 5; i++) tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            AgregarBotonNav(tabla, "store", "🏷️", () => MostrarVista(new StoreView(this)));
            AgregarBotonNav(tabla, "news", "📰", () => MostrarVista(new NewsView(this)));
            AgregarBotonNav(tabla, "library", "📚", () => MostrarVista(new LibraryView(this))); // reemplaza el ícono de escudo
            AgregarBotonNav(tabla, "notif", "🔔", () => MostrarVista(new NotificationsView(this)));
            AgregarBotonNav(tabla, "menu", "☰", AlternarMenu);

            _bottomNav.Controls.Add(tabla);
            Controls.Add(_bottomNav);
        }

        private void AgregarBotonNav(TableLayoutPanel tabla, string clave, string icono, Action alHacerClick)
        {
            var btn = new Button
            {
                Text = icono,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Emoji", 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.PanelDark,
                ForeColor = UiTheme.TextSecondary
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                // El botón "menu" maneja su propio estado de nav activa dentro de AlternarMenu().
                if (clave != "menu") MarcarNavActiva(clave);
                alHacerClick();
            };
            _botonesNav[clave] = btn;
            tabla.Controls.Add(btn);
        }

        private void MarcarNavActiva(string clave)
        {
            _navActiva = clave;
            foreach (var kvp in _botonesNav)
                kvp.Value.ForeColor = kvp.Key == clave ? UiTheme.AccentBlue : UiTheme.TextSecondary;
        }

        /// <summary>
        /// Abre o cierra la pantalla de menú. Si ya está abierta (_menuAbierto),
        /// la segunda pulsación del mismo botón la cierra y regresa exactamente
        /// a la vista y al estado de navegación que había antes de abrirla.
        /// </summary>
        private void AlternarMenu()
        {
            if (_menuAbierto)
            {
                MarcarNavActiva(_navActivaAntesDelMenu);
                MostrarVista(_vistaAntesDelMenu ?? new StoreView(this));
            }
            else
            {
                _navActivaAntesDelMenu = _navActiva;
                _vistaAntesDelMenu = _contentPanel.Controls.Count > 0 ? _contentPanel.Controls[0] as UserControl : null;
                MarcarNavActiva("menu");
                MostrarVista(new MenuPageView(this));
            }
        }

        // ============================= SESIÓN =============================

        public void MostrarLogin()
        {
            _headerPanel.Visible = false;
            _bottomNav.Visible = false;
            MostrarVista(new LoginView(this));
        }

        public void MostrarRegistro()
        {
            _headerPanel.Visible = false;
            _bottomNav.Visible = false;
            MostrarVista(new RegisterView(this));
        }

        public void AlIniciarSesionExitoso()
        {
            _headerPanel.Visible = true;
            _bottomNav.Visible = true;
            _menuAbierto = false;
            _vistaAntesDelMenu = null;
            ActualizarAvatarTopBar();
            ActualizarBadgeCarrito();
            MarcarNavActiva("store");
            MostrarVista(new StoreView(this));
        }

        public void CerrarSesion()
        {
            SessionManager.CerrarSesion();
            _menuAbierto = false;
            _vistaAntesDelMenu = null;
            MostrarLogin();
        }
    }
}
