using System.Diagnostics;
using NovaCore.Services;
using NovaCore.Views;

namespace NovaCore
{
    /// <summary>
    /// Shell principal, estilo app móvil de Steam dentro de una ventana de
    /// tamaño fijo. Estructura:
    ///   - Header: ícono NOVACORE, buscador, carrito, avatar y
    ///     tabs de navegación (MENÚ ▾ / DESEOS / BILLETERA)
    ///   - ContentPanel: aquí se intercambian los UserControls (páginas)
    ///   - BottomNav: 5 íconos fijos (Tienda, Noticias, Biblioteca, Notificaciones, Menú)
    /// El botón ☰ (BottomNav) abre el menú principal (MenuPageView) y se
    /// comporta como un "toggle": si ya está abierto, volver a presionarlo lo
    /// cierra y regresa a la vista anterior.
    /// </summary>
    public class MainForm : Form
    {
        // Header
        private readonly Panel _headerPanel = new();
        private readonly PictureBox _avatarTopBar = new();
        private readonly Button _btnCart = new();
        private readonly PictureBox _logoMarca = new();
        private static readonly string RutaLogo = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        private Image? _logoImagen;

        // Tabs (MENÚ / DESEOS / BILLETERA)
        private Button _btnTabMenu = new();

        // Contenido (panel con doble buffer para que las transiciones no parpadeen)
        private readonly Panel _contentPanel = new ContentPanelNoFlicker();

        // Bottom nav
        private readonly Panel _bottomNav = new();
        private readonly Dictionary<string, Button> _botonesNav = new();
        private string _navActiva = "store";

        // --- Animación de transición entre vistas (slide-up + desplazamiento de la anterior) ---
        private readonly System.Windows.Forms.Timer _animadorVistas = new() { Interval = 16 };
        private readonly Stopwatch _cronometroAnimacion = new();
        private UserControl? _vistaEntrante;
        private Bitmap? _capturaVistaSaliente;
        private const int DuracionTransicionMs = 240;

        /// <summary>Vistas que forman parte de la interfaz principal del programa:
        /// la barra inferior se mantiene visible únicamente en ellas.</summary>
        private static readonly Type[] VistasPrincipales =
        {
            typeof(StoreView), typeof(NewsView), typeof(LibraryView),
            typeof(NotificationsView), typeof(MenuPageView)
        };

        // --- Escalado responsivo (adapta el contenido al tipo/tamaño de pantalla) ---
        private const int AnchoDiseno = 400;
        private const int AltoDiseno = 820;
        private const float EscalaMinima = 0.55f;
        private const float EscalaMaxima = 2.5f;
        private float _escalaUI = 1f;
        private bool _uiConstruido;
        private readonly Dictionary<UserControl, float> _escalaAplicada = new();

        /// <summary>Panel con doble buffer para evitar flicker durante las animaciones.</summary>
        private sealed class ContentPanelNoFlicker : Panel
        {
            public ContentPanelNoFlicker() { DoubleBuffered = true; }
        }

        // --- Estado del toggle del menú (☰ / "MENU ▾") ---
        private bool _menuAbierto = false;
        private UserControl? _vistaAntesDelMenu;
        private string _navActivaAntesDelMenu = "store";

        // --- Dropdown de categorías del header ---
        private readonly ContextMenuStrip _menuCategorias = new()
        {
            BackColor = UiTheme.PanelDark,
            ForeColor = UiTheme.TextPrimary,
            ShowImageMargin = false,
            AutoSize = true,
            Renderer = new ToolStripProfessionalRenderer(new NovaCoreColorTable())
        };

        public MainForm()
        {
            _animadorVistas.Tick += AnimadorVistas_Tick;
            CargarLogo();
            ConfigurarFormulario();
            ConstruirContentPanel();
            ConstruirHeader();
            ConstruirBottomNav();

            MostrarLogin();

            // Una vez construida toda la UI, se adapta el tamaño a la pantalla actual.
            _uiConstruido = true;
            AjustarEscalaAPantalla();
        }

        private void ConfigurarFormulario()
        {
            Text = "NovaCore";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(AnchoDiseno, AltoDiseno); // proporción tipo teléfono
            BackColor = UiTheme.BackgroundDark;
            Font = UiTheme.FontBody;
            MinimumSize = new Size((int)(AnchoDiseno * EscalaMinima) + 16, (int)(AltoDiseno * EscalaMinima) + 39);
            Resize += MainForm_Resize;
        }

        // ============================= HEADER =============================

        private void ConstruirHeader()
        {
            _headerPanel.Dock = DockStyle.Top;
            _headerPanel.Height = 96;
            _headerPanel.BackColor = UiTheme.PanelDark;

            // Degradado "galáctico": resplandor naranja nova en la esquina superior
            // que se disuelve hacia el negro del panel.
            _headerPanel.Paint += (s, e) =>
            {
                var area = _headerPanel.ClientRectangle;
                if (area.Width <= 0 || area.Height <= 0) return;
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    area, Color.FromArgb(70, 32, 12), UiTheme.PanelDark, 65f);
                e.Graphics.FillRectangle(brush, area);
                using var linea = new Pen(Color.FromArgb(90, UiTheme.AccentPrimary), 1f);
                e.Graphics.DrawLine(linea, 0, area.Height - 1, area.Width, area.Height - 1);
            };

            // Posiciones calculadas de derecha a izquierda para garantizar que
            // ningún control se superponga con otro.
            const int margen = 12;
            const int anchoIcono = 34;
            const int separacion = 8;

            int xAvatar = 400 - margen - anchoIcono;                 // avatar, extremo derecho
            int xCarrito = xAvatar - separacion - anchoIcono;        // carrito (🛒)
            int xBuscar = xCarrito - separacion - anchoIcono;        // lupa (🔍)
            int anchoMarca = xBuscar - separacion - margen;          // resto: marca "NOVACORE"

            _logoMarca.Location = new Point(margen, 17);
            _logoMarca.Size = new Size(30, 30);
            _logoMarca.BackColor = Color.Transparent;
            _logoMarca.Paint += (s, e) =>
            {
                if (_logoImagen != null)
                {
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(_logoImagen, _logoMarca.ClientRectangle);
                }
                else
                {
                    DibujarNova(e.Graphics, _logoMarca.ClientRectangle);
                }
            };

            var lblMarca = new Label
            {
                Text = "NOVACORE",
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                Location = new Point(margen + 36, 20),
                Size = new Size(Math.Max(10, anchoMarca - 36), 24),
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

            _avatarTopBar.Size = new Size(anchoIcono, anchoIcono);
            _avatarTopBar.Location = new Point(xAvatar, 10);
            _avatarTopBar.SizeMode = PictureBoxSizeMode.StretchImage;
            _avatarTopBar.Cursor = Cursors.Hand;
            _avatarTopBar.Click += (s, e) => { if (SessionManager.HaySesionActiva) MostrarVista(new ProfileView(this)); };

            // --- Fila 2: tabs MENÚ / DESEOS / BILLETERA ---
            _btnTabMenu = CrearBotonTab("MENÚ ▾", 12);
            var btnTabWishlist = CrearBotonTab("DESEOS", 130);
            var btnTabWallet = CrearBotonTab("BILLETERA", 260);
            _btnTabMenu.Click += (s, e) => MostrarMenuDesplegable();
            btnTabWishlist.Click += (s, e) => MostrarVista(new WishlistView(this));
            btnTabWallet.Click += (s, e) => MostrarVista(new WalletView(this));

            _headerPanel.Controls.Add(_logoMarca);
            _headerPanel.Controls.Add(lblMarca);
            _headerPanel.Controls.Add(btnBuscar);
            _headerPanel.Controls.Add(_btnCart);
            _headerPanel.Controls.Add(_avatarTopBar);
            _headerPanel.Controls.Add(_btnTabMenu);
            _headerPanel.Controls.Add(btnTabWishlist);
            _headerPanel.Controls.Add(btnTabWallet);

            Controls.Add(_headerPanel);
        }

        /// <summary>
        /// Carga el logo desde <c>Assets\logo.png</c> (junto al ejecutable) y lo
        /// usa como ícono de ventana. Si no existe, se dibuja una nova vectorial.
        /// </summary>
        private void CargarLogo()
        {
            try
            {
                if (!File.Exists(RutaLogo)) return;
                _logoImagen = Image.FromFile(RutaLogo);
                using var bmp = new Bitmap(RutaLogo);
                Icon = Icon.FromHandle(bmp.GetHicon());
            }
            catch
            {
                _logoImagen = null;
            }
        }

        /// <summary>Dibuja un destello de nova (resplandor + estrella de 4 puntas).</summary>
        private static void DibujarNova(Graphics g, Rectangle area)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float cx = area.Width / 2f, cy = area.Height / 2f;
            float r = Math.Min(area.Width, area.Height) / 2f;

            using var halo = new System.Drawing.Drawing2D.GraphicsPath();
            halo.AddEllipse(cx - r, cy - r, r * 2, r * 2);
            using var glow = new System.Drawing.Drawing2D.PathGradientBrush(halo)
            {
                CenterColor = Color.FromArgb(210, UiTheme.AccentGlow),
                SurroundColors = new[] { Color.FromArgb(0, UiTheme.AccentPrimary) }
            };
            g.FillPath(glow, halo);

            float pico = r * 0.95f, cintura = r * 0.26f;
            using var estrella = new System.Drawing.Drawing2D.GraphicsPath();
            estrella.AddPolygon(new[]
            {
                new PointF(cx, cy - pico), new PointF(cx + cintura, cy - cintura),
                new PointF(cx + pico, cy), new PointF(cx + cintura, cy + cintura),
                new PointF(cx, cy + pico), new PointF(cx - cintura, cy + cintura),
                new PointF(cx - pico, cy), new PointF(cx - cintura, cy - cintura)
            });
            using var relleno = new SolidBrush(UiTheme.AccentGlow);
            g.FillPath(relleno, estrella);

            using var nucleo = new SolidBrush(Color.FromArgb(255, 250, 240, 220));
            float nr = r * 0.26f;
            g.FillEllipse(nucleo, cx - nr, cy - nr, nr * 2, nr * 2);
        }

        /// <summary>
        /// Abre el menú desplegable del header con dos secciones funcionales:
        /// "Categorías" (submenú con el catálogo por categoría) y
        /// "Recomendaciones" (sugerencias personalizadas).
        /// </summary>
        private void MostrarMenuDesplegable()
        {
            _menuCategorias.Items.Clear();

            var menuCategorias = new ToolStripMenuItem("🎮  Categorías ▸")
            {
                ForeColor = UiTheme.TextPrimary,
                BackColor = UiTheme.PanelDark
            };

            var btnTodas = new ToolStripMenuItem("Todas las categorías")
            {
                ForeColor = UiTheme.TextPrimary,
                BackColor = UiTheme.PanelDark
            };
            btnTodas.Click += (s, e) => MostrarVista(new StoreView(this));
            menuCategorias.DropDownItems.Add(btnTodas);

            foreach (var categoria in new JuegoService().ObtenerCategorias())
            {
                int categoriaId = categoria.CategoriaId;
                var item = new ToolStripMenuItem(categoria.Nombre)
                {
                    ForeColor = UiTheme.TextPrimary,
                    BackColor = UiTheme.PanelDark
                };
                item.Click += (s, e) => MostrarVista(new StoreView(this, null, categoriaId));
                menuCategorias.DropDownItems.Add(item);
            }

            var btnRecomendaciones = new ToolStripMenuItem("✨  Recomendaciones")
            {
                ForeColor = UiTheme.TextPrimary,
                BackColor = UiTheme.PanelDark
            };
            btnRecomendaciones.Click += (s, e) => MostrarVista(new RecomendacionesView(this));

            _menuCategorias.Items.Add(menuCategorias);
            _menuCategorias.Items.Add(btnRecomendaciones);

            _menuCategorias.Show(_btnTabMenu, new Point(0, _btnTabMenu.Height));
        }

        /// <summary>
        /// Abre el menú principal (MenuPageView) desde cualquier vista, sin
        /// romper el comportamiento toggle del botón ☰.
        /// </summary>
        public void AbrirMenuPrincipal()
        {
            if (!_menuAbierto) AlternarMenu();
        }

        /// <summary>Paleta de colores oscura para el dropdown de categorías.</summary>
        private class NovaCoreColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => UiTheme.PanelMid;
            public override Color MenuItemSelectedGradientBegin => UiTheme.PanelMid;
            public override Color MenuItemSelectedGradientEnd => UiTheme.PanelMid;
            public override Color MenuItemBorder => UiTheme.PanelMid;
            public override Color ToolStripDropDownBackground => UiTheme.PanelDark;
            public override Color MenuBorder => UiTheme.BorderColor;
            public override Color ImageMarginGradientBegin => UiTheme.PanelDark;
            public override Color ImageMarginGradientMiddle => UiTheme.PanelDark;
            public override Color ImageMarginGradientEnd => UiTheme.PanelDark;
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
            _avatarTopBar.BackColor = UiTheme.AccentPrimary;

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

        /// <summary>Muestra en la campana de notificaciones cuántas notificaciones sin leer hay.</summary>
        public void ActualizarBadgeNotificaciones()
        {
            if (!_botonesNav.TryGetValue("notif", out var btn)) return;

            if (SessionManager.UsuarioActual == null)
            {
                btn.Text = "🔔";
                return;
            }

            var pendientes = new NotificacionService().ContarNoLeidas(SessionManager.UsuarioActual.UsuarioId);
            btn.Text = pendientes > 0 ? $"🔔{pendientes}" : "🔔";
        }

        // ============================= CONTENIDO =============================

        private void ConstruirContentPanel()
        {
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = UiTheme.BackgroundDark;
            _contentPanel.Paint += Contenido_Paint;
            Controls.Add(_contentPanel);
        }

        /// <summary>
        /// Dibuja la vista saliente (capturada como imagen) mientras la vista nueva
        /// sube deslizándose. La vista anterior se desplaza un poco hacia arriba
        /// para dar sensación de "la página sigue viajando" durante la transición.
        /// </summary>
        private void Contenido_Paint(object? sender, PaintEventArgs e)
        {
            if (_capturaVistaSaliente == null) return;

            double progreso = Math.Min(1.0, _cronometroAnimacion.Elapsed.TotalMilliseconds / DuracionTransicionMs);
            int y = -(int)(_capturaVistaSaliente.Height * 0.15 * progreso);
            e.Graphics.DrawImage(_capturaVistaSaliente, 0, y);
        }

        /// <summary>
        /// Punto único de navegación. Si la vista que se está mostrando NO es el
        /// menú, se considera que el usuario "navegó lejos" y el estado de
        /// menú-abierto se resetea (así, si luego vuelve a tocar el kebab, se
        /// abre de nuevo en vez de quedar en un estado inconsistente).
        /// Cambia de página con una transición suave (slide-up con ease-out y
        /// desplazamiento de la vista anterior) para que la navegación se sienta
        /// fluida y nada brusca.
        /// </summary>
        public void MostrarVista(UserControl vista)
        {
            _menuAbierto = vista is MenuPageView;
            ActualizarVisibilidadNavInferior(vista);

            _animadorVistas.Stop();
            _capturaVistaSaliente?.Dispose();
            _capturaVistaSaliente = null;
            _vistaEntrante = null;

            // Si ya se está mostrando exactamente la misma instancia, no hacer nada.
            if (_contentPanel.Controls.Count > 0 && ReferenceEquals(_contentPanel.Controls[0], vista))
                return;

            // Las vistas se construyen siempre al tamaño de diseño; se escalan aquí
            // a la escala actual para que el contenido se adapte a la pantalla.
            EscalarVista(vista);

            bool hayVistaPrevia = _contentPanel.Controls.Count > 0;
            bool animar = false;

            if (hayVistaPrevia && _contentPanel.ClientSize.Height > 40)
            {
                animar = _contentPanel.Controls[0] is UserControl saliente && CapturarSaliente(saliente);
            }

            if (!animar)
            {
                _contentPanel.Controls.Clear();
                RemoverVistaAntigua();
                vista.Dock = DockStyle.Fill;
                _contentPanel.Controls.Add(vista);
                return;
            }

            vista.Dock = DockStyle.None;
            vista.Location = new Point(0, _contentPanel.ClientSize.Height);
            vista.Size = _contentPanel.ClientSize;
            _contentPanel.Controls.Clear();
            RemoverVistaAntigua();
            _contentPanel.Controls.Add(vista);
            _vistaEntrante = vista;
            _cronometroAnimacion.Restart();
            _animadorVistas.Start();
        }

        /// <summary>Escala una vista recién creada solo si aún no está a la escala actual.</summary>
        private void EscalarVista(UserControl vista)
        {
            float aplicada = _escalaAplicada.TryGetValue(vista, out float prev) ? prev : 1f;
            if (Math.Abs(_escalaUI - aplicada) < 0.0001f) return;

            EscalarArbol(vista, _escalaUI / aplicada);
            _escalaAplicada[vista] = _escalaUI;
        }

        /// <summary>Limpia el registro de escala de la vista que se está reemplazando.</summary>
        private void RemoverVistaAntigua()
        {
            if (_contentPanel.Controls.Count > 0 && _contentPanel.Controls[0] is UserControl vieja)
                _escalaAplicada.Remove(vieja);
        }

        /// <summary>Captura la vista actual como imagen para poder dibujarla durante la transición.</summary>
        private bool CapturarSaliente(UserControl saliente)
        {
            if (saliente.Width <= 0 || saliente.Height <= 0) return false;
            try
            {
                var bmp = new Bitmap(saliente.Width, saliente.Height);
                saliente.DrawToBitmap(bmp, new Rectangle(0, 0, saliente.Width, saliente.Height));
                _capturaVistaSaliente = bmp;
                return true;
            }
            catch
            {
                _capturaVistaSaliente = null;
                return false;
            }
        }

        /// <summary>
        /// Oculta la barra inferior al entrar a pantallas secundarias (búsqueda,
        /// carrito, perfil, detalle, administración...) y la mantiene siempre
        /// visible en la interfaz principal del programa (las 5 pestañas).
        /// </summary>
        private void ActualizarVisibilidadNavInferior(UserControl vista)
        {
            bool esPrincipal = Array.IndexOf(VistasPrincipales, vista.GetType()) >= 0;
            _bottomNav.Visible = _headerPanel.Visible && esPrincipal;
        }

        /// <summary>
        /// Avanza la animación de transición: sube la vista nueva con
        /// easing cubic-out y, al terminar, restaura el acomodo Dock completo.
        /// </summary>
        private void AnimadorVistas_Tick(object? sender, EventArgs e)
        {
            double t = _cronometroAnimacion.Elapsed.TotalMilliseconds / DuracionTransicionMs;

            if (t >= 1.0)
            {
                _animadorVistas.Stop();
                _capturaVistaSaliente?.Dispose();
                _capturaVistaSaliente = null;
                if (_vistaEntrante != null)
                {
                    _vistaEntrante.Dock = DockStyle.Fill;
                    _vistaEntrante = null;
                    _contentPanel.Invalidate();
                }
                return;
            }

            double suavizado = 1.0 - Math.Pow(1.0 - t, 3); // ease-out cúbico
            if (_vistaEntrante != null)
                _vistaEntrante.Location = new Point(0, (int)(_contentPanel.ClientSize.Height * (1.0 - suavizado)));
            _contentPanel.Invalidate();
        }

        // ============================= ESCALADO RESPONSIVO =============================

        /// <summary>
        /// Al arrancar, si el tamaño de diseño (400x820) no cabe en la pantalla
        /// actual, se reduce proporcionalmente para que el contenido se adapte
        /// al tipo de pantalla del programa (nunca se agranda por encima del diseño).
        /// </summary>
        private void AjustarEscalaAPantalla()
        {
            var areaTrabajo = Screen.FromControl(this).WorkingArea;
            float maxEncaje = Math.Min(
                (float)areaTrabajo.Width / AnchoDiseno,
                (float)areaTrabajo.Height / AltoDiseno);

            float escalaInicial = Math.Max(EscalaMinima, Math.Min(1f, maxEncaje));
            if (Math.Abs(escalaInicial - 1f) > 0.0001f)
                AplicarEscala(escalaInicial);
        }

        /// <summary>
        /// Mantiene la proporción "tipo teléfono" del diseño: al redimensionar la
        /// ventana recalcula la escala (la menor entre ancho y alto) y re-escala
        /// toda la UI de forma uniforme.
        /// </summary>
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (!_uiConstruido || WindowState == FormWindowState.Minimized) return;

            float escala = Math.Min(
                (float)ClientSize.Width / AnchoDiseno,
                (float)ClientSize.Height / AltoDiseno);
            float nueva = Math.Clamp(escala, EscalaMinima, EscalaMaxima);

            if (Math.Abs(nueva - _escalaUI) < 0.005f)
            {
                if (WindowState == FormWindowState.Normal)
                {
                    var objetivo = new Size((int)Math.Round(AnchoDiseno * _escalaUI), (int)Math.Round(AltoDiseno * _escalaUI));
                    if (ClientSize != objetivo) ClientSize = objetivo;
                }
                return;
            }

            AplicarEscala(nueva);
        }

        /// <summary>Aplica una nueva escala a toda la UI, escalando proporcionalmente
        /// tamaños, posiciones y fuentes.</summary>
        private void AplicarEscala(float nuevaEscala)
        {
            float factor = nuevaEscala / _escalaUI;
            if (factor <= 0f) return;

            _headerPanel.SuspendLayout();
            _contentPanel.SuspendLayout();
            _bottomNav.SuspendLayout();
            _menuCategorias.SuspendLayout();

            EscalarArbol(_headerPanel, factor);
            EscalarArbol(_contentPanel, factor);
            EscalarArbol(_bottomNav, factor);
            EscalarFuentes(_menuCategorias, factor);

            _headerPanel.ResumeLayout();
            _contentPanel.ResumeLayout();
            _bottomNav.ResumeLayout();
            _menuCategorias.ResumeLayout();

            _escalaUI = nuevaEscala;
            UiTheme.EscalaUI = nuevaEscala;

            if (_contentPanel.Controls.Count > 0 && _contentPanel.Controls[0] is UserControl tope)
                _escalaAplicada[tope] = nuevaEscala;

            if (WindowState == FormWindowState.Normal)
                ClientSize = new Size((int)Math.Round(AnchoDiseno * nuevaEscala), (int)Math.Round(AltoDiseno * nuevaEscala));
        }

        /// <summary>Escala un control y toda su jerarquía (tamaños/posiciones) y sus fuentes.</summary>
        private static void EscalarArbol(Control control, float factor)
        {
            control.Scale(new SizeF(factor, factor));
            EscalarFuentes(control, factor);
        }

        /// <summary>Re-escala el tamaño de las fuentes de un control y sus hijos.</summary>
        private static void EscalarFuentes(Control control, float factor)
        {
            var fuente = control.Font;
            control.Font = new Font(fuente.FontFamily, fuente.SizeInPoints * factor, fuente.Style, fuente.Unit);
            foreach (Control hijo in control.Controls)
                EscalarFuentes(hijo, factor);
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
                kvp.Value.ForeColor = kvp.Key == clave ? UiTheme.AccentPrimary : UiTheme.TextSecondary;
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
            ActualizarBadgeNotificaciones();
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
