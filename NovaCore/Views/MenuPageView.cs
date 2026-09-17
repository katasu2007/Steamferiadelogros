using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Pantalla de menú (equivalente a tocar las 3 barras / el kebab en la app
    /// real de Steam): lista de opciones de navegación. Si el usuario activo es
    /// Administrador, se agregan además los accesos a los paneles de gestión.
    /// </summary>
    public class MenuPageView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly ReporteService _reporteService = new();

        public MenuPageView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.PanelDark };
            var btnVolver = new Button { Text = "←", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(8, 6), Size = new Size(32, 32), FlatStyle = FlatStyle.Flat, BackColor = UiTheme.PanelDark, ForeColor = UiTheme.TextPrimary };
            btnVolver.FlatAppearance.BorderSize = 0;
            btnVolver.Click += (s, e) => _mainForm.MostrarVista(new StoreView(_mainForm));
            var lblTitulo = new Label { Text = "MENÚ", ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontSubtitle, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            header.Controls.Add(lblTitulo);
            header.Controls.Add(btnVolver);
            Controls.Add(header);

            int y = 55;
            AgregarOpcion("🏷️", "Tienda", ref y, () => _mainForm.MostrarVista(new StoreView(_mainForm)));
            AgregarOpcion("📰", "Noticias", ref y, () => _mainForm.MostrarVista(new NewsView(_mainForm)));
            AgregarOpcion("🔔", "Notificaciones", ref y, () => _mainForm.MostrarVista(new NotificationsView(_mainForm)));
            AgregarOpcion("📚", "Biblioteca", ref y, () => _mainForm.MostrarVista(new LibraryView(_mainForm)));
            AgregarOpcion("🛒", "Carrito", ref y, () => _mainForm.MostrarVista(new CartView(_mainForm)));
            AgregarOpcion("👥", "Comunidad (próximamente)", ref y, () => MessageBox.Show("Función en desarrollo.", "Próximamente"));
            AgregarOpcion("💬", "Chat (próximamente)", ref y, () => MessageBox.Show("Función en desarrollo.", "Próximamente"));
            AgregarOpcion("⚙️", "Configuración", ref y, () => MessageBox.Show("Configuraciones básicas próximamente.", "Configuración"));
            AgregarOpcion("❓", "Soporte", ref y, () => MessageBox.Show("Soporte: soporte@steamclone.com", "Soporte"));

            if (SessionManager.EsAdministrador)
            {
                AgregarSeparador("— ADMINISTRACIÓN —", ref y);
                AgregarOpcion("🎮", "Gestión de Juegos", ref y, () => _mainForm.MostrarVista(new AdminDashboardView(_mainForm)));
                AgregarOpcion("👤", "Gestión de Usuarios", ref y, () => _mainForm.MostrarVista(new UserManagementView(_mainForm)));
                AgregarOpcion("📊", "Reportes de Ventas", ref y, () => _mainForm.MostrarVista(new ReportsView(_mainForm)));
                var pendientes = _reporteService.ContarPendientes();
                AgregarOpcion("🚩", $"Moderación{(pendientes > 0 ? $" ({pendientes} pendientes)" : string.Empty)}", ref y, () => _mainForm.MostrarVista(new AdminReportsView(_mainForm)));
            }

            AgregarSeparador(string.Empty, ref y);
            AgregarOpcion("⎋", "Cerrar sesión", ref y, () => _mainForm.CerrarSesion(), UiTheme.AccentRed);
        }

        private void AgregarOpcion(string icono, string texto, ref int y, Action alHacerClick, Color? color = null)
        {
            var fila = new Panel { Location = new Point(0, y), Size = new Size(400, 52), Cursor = Cursors.Hand, BackColor = UiTheme.BackgroundDark };
            var lbl = new Label
            {
                Text = $"{icono}   {texto}",
                ForeColor = color ?? UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(16, 14),
                AutoSize = true
            };
            fila.Controls.Add(lbl);

            void Click(object? s, EventArgs e) => alHacerClick();
            fila.Click += Click;
            lbl.Click += Click;

            fila.MouseEnter += (s, e) => fila.BackColor = UiTheme.PanelMid;
            fila.MouseLeave += (s, e) => fila.BackColor = UiTheme.BackgroundDark;

            var linea = new Panel { Location = new Point(0, y + 51), Size = new Size(400, 1), BackColor = UiTheme.BorderColor };

            Controls.Add(fila);
            Controls.Add(linea);
            y += 52;
        }

        private void AgregarSeparador(string texto, ref int y)
        {
            y += 8;
            if (!string.IsNullOrEmpty(texto))
            {
                var lbl = new Label { Text = texto, ForeColor = UiTheme.TextSecondary, Font = new Font("Segoe UI", 8, FontStyle.Bold), Location = new Point(16, y), AutoSize = true };
                Controls.Add(lbl);
                y += 24;
            }
        }
    }
}
