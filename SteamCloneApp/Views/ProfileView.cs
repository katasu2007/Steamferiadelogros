using SteamCloneApp.Models;
using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>
    /// Perfil del usuario, rediseñado para parecerse al perfil móvil de Steam:
    /// banner + avatar (editable, se guarda con OpenFileDialog), stats (Games /
    /// Friends / Add Funds), botón "Add friends" y la lista "MY CONTENT".
    /// </summary>
    public class ProfileView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly BibliotecaService _bibliotecaService = new();
        private readonly UsuarioService _usuarioService = new();
        private readonly ReporteService _reporteService = new();
        private PictureBox _avatar = new();

        public ProfileView(MainForm mainForm)
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

            // --- Banner + header ---
            var banner = new Panel { Location = new Point(0, 0), Size = new Size(400, 110) };
            banner.Paint += (s, e) =>
            {
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(banner.ClientRectangle, Color.FromArgb(90, 40, 20), UiTheme.BackgroundDark, 90f);
                e.Graphics.FillRectangle(brush, banner.ClientRectangle);
            };
            var btnVolver = new Button { Text = "←", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(8, 8), Size = new Size(32, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(120, 0, 0, 0), ForeColor = Color.White };
            btnVolver.FlatAppearance.BorderSize = 0;
            btnVolver.Click += (s, e) => _mainForm.MostrarVista(new StoreView(_mainForm));
            banner.Controls.Add(btnVolver);

            _avatar = new PictureBox { Size = new Size(78, 78), Location = new Point(20, 20), SizeMode = PictureBoxSizeMode.StretchImage, Cursor = Cursors.Hand, BackColor = UiTheme.AccentBlue };
            CargarAvatar(usuario);
            _avatar.Click += (s, e) => CambiarAvatar(usuario);
            banner.Controls.Add(_avatar);

            var lblNombre = new Label { Text = usuario.NombreUsuario, ForeColor = UiTheme.TextPrimary, Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(108, 32), AutoSize = true };
            var btnVerPerfil = UiTheme.CreateStyledButton("View Profile", UiTheme.PanelMid);
            btnVerPerfil.Location = new Point(108, 62);
            btnVerPerfil.Size = new Size(110, 28);
            btnVerPerfil.Click += (s, e) => MostrarResumenPerfil(usuario);
            banner.Controls.Add(lblNombre);
            banner.Controls.Add(btnVerPerfil);

            Controls.Add(banner);

            // --- Stats: Games / Friends / Add Funds ---
            int cantidadJuegos = _bibliotecaService.ObtenerBibliotecaDeUsuario(usuario.UsuarioId).Count;
            var statsPanel = new Panel { Location = new Point(0, 118), Size = new Size(400, 70) };
            statsPanel.Controls.Add(CrearStat(cantidadJuegos.ToString(), "Games", 8));
            statsPanel.Controls.Add(CrearStat(usuario.Amigos.Count.ToString(), "Friends", 140));
            var statWallet = CrearStat("Add Funds", "Wallet", 272);
            statWallet.Cursor = Cursors.Hand;
            statWallet.Click += (s, e) => _mainForm.MostrarVista(new WalletView(_mainForm));
            foreach (Control c in statWallet.Controls) c.Click += (s, e) => _mainForm.MostrarVista(new WalletView(_mainForm));
            statsPanel.Controls.Add(statWallet);
            Controls.Add(statsPanel);

            // --- Add friends ---
            var btnAddFriends = UiTheme.CreateStyledButton("👤➕  Add friends", UiTheme.AccentBlue);
            btnAddFriends.Location = new Point(16, 196);
            btnAddFriends.Width = 368;
            btnAddFriends.Click += (s, e) => AgregarAmigo(usuario);
            Controls.Add(btnAddFriends);

            // --- MY CONTENT ---
            var lblMyContent = new Label { Text = "MY CONTENT", ForeColor = UiTheme.TextSecondary, Font = new Font("Segoe UI", 8, FontStyle.Bold), Location = new Point(16, 246), AutoSize = true };
            Controls.Add(lblMyContent);

            int y = 274;
            AgregarItemMenu("Friend Activity", ref y, () => MessageBox.Show("Todavía no hay actividad de amigos.", "Friend Activity"));
            AgregarItemMenu("Friends", ref y, () => MostrarAmigos(usuario));
            AgregarItemMenu("Groups", ref y, () => MessageBox.Show("No perteneces a ningún grupo todavía.", "Groups"));
            AgregarItemMenu("Screenshots/Videos", ref y, () => MessageBox.Show("Aún no subiste capturas ni videos.", "Screenshots/Videos"));
            AgregarItemMenu("Badges", ref y, () => MessageBox.Show("Aún no tienes insignias.", "Badges"));
            AgregarItemMenu("Inventory", ref y, () => _mainForm.MostrarVista(new LibraryView(_mainForm)));
            AgregarItemMenu("Reportar un usuario", ref y, () => ReportarUsuario(usuario));
            AgregarItemMenu("Account Details", ref y, () => MessageBox.Show(
                $"Usuario: {usuario.NombreUsuario}\nCorreo: {usuario.Correo}\nRol: {usuario.Rol}\nSaldo: {usuario.Saldo:C}\nMiembro desde: {usuario.FechaRegistro:d}",
                "Account Details"));
            AgregarItemMenu("Change Account", ref y, () => _mainForm.CerrarSesion());
            AgregarItemMenu("Sign Out", ref y, () => _mainForm.CerrarSesion(), UiTheme.AccentRed);
        }

        private Panel CrearStat(string valor, string etiqueta, int x)
        {
            var panel = new Panel { Location = new Point(x, 0), Size = new Size(120, 60), BackColor = UiTheme.PanelDark };
            var lblValor = new Label { Text = valor, ForeColor = UiTheme.TextPrimary, Font = new Font("Segoe UI", 13, FontStyle.Bold), Location = new Point(0, 8), Width = 120, TextAlign = ContentAlignment.MiddleCenter };
            var lblEtiqueta = new Label { Text = etiqueta, ForeColor = UiTheme.TextSecondary, Location = new Point(0, 34), Width = 120, TextAlign = ContentAlignment.MiddleCenter };
            panel.Controls.Add(lblValor);
            panel.Controls.Add(lblEtiqueta);
            return panel;
        }

        private void AgregarItemMenu(string texto, ref int y, Action alHacerClick, Color? color = null)
        {
            var fila = new Panel { Location = new Point(0, y), Size = new Size(400, 46), Cursor = Cursors.Hand };
            var lbl = new Label { Text = texto, ForeColor = color ?? UiTheme.TextPrimary, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), Location = new Point(16, 12), AutoSize = true };
            fila.Controls.Add(lbl);
            void Click(object? s, EventArgs e) => alHacerClick();
            fila.Click += Click;
            lbl.Click += Click;
            fila.MouseEnter += (s, e) => fila.BackColor = UiTheme.PanelMid;
            fila.MouseLeave += (s, e) => fila.BackColor = UiTheme.BackgroundDark;

            var linea = new Panel { Location = new Point(0, y + 45), Size = new Size(400, 1), BackColor = UiTheme.BorderColor };
            Controls.Add(fila);
            Controls.Add(linea);
            y += 46;
        }

        private void CargarAvatar(Usuario usuario)
        {
            _avatar.Image?.Dispose();
            _avatar.Image = null;
            if (!string.IsNullOrWhiteSpace(usuario.AvatarPath) && File.Exists(usuario.AvatarPath))
            {
                try { _avatar.Image = Image.FromFile(usuario.AvatarPath); }
                catch { /* placeholder de color si falla */ }
            }
        }

        private void CambiarAvatar(Usuario usuario)
        {
            using var dialogo = new OpenFileDialog { Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp", Title = "Elegir foto de perfil" };
            if (dialogo.ShowDialog() == DialogResult.OK)
            {
                _usuarioService.ActualizarAvatar(usuario.UsuarioId, dialogo.FileName);
                CargarAvatar(usuario);
                _mainForm.ActualizarAvatarTopBar();
            }
        }

        private void AgregarAmigo(Usuario usuario)
        {
            var disponibles = _usuarioService.ObtenerTodos()
                .Where(u => u.UsuarioId != usuario.UsuarioId
                            && u.Activo
                            && !usuario.Amigos.Any(a => a.Equals(u.NombreUsuario, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(u => u.NombreUsuario)
                .ToList();

            if (disponibles.Count == 0)
            {
                MessageBox.Show("No hay más usuarios registrados para agregar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialogo = new Form
            {
                Text = "Agregar amigo",
                Size = new Size(320, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = UiTheme.PanelDark
            };

            var lbl = new Label { Text = "Selecciona un usuario:", ForeColor = UiTheme.TextPrimary, Location = new Point(15, 15), AutoSize = true };
            var cmb = new ComboBox
            {
                Location = new Point(15, 45),
                Width = 270,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UiTheme.PanelMid,
                ForeColor = UiTheme.TextPrimary
            };
            foreach (var u in disponibles) cmb.Items.Add(u.NombreUsuario);
            cmb.SelectedIndex = 0;

            var btnAgregar = UiTheme.CreateStyledButton("Agregar amigo", UiTheme.AccentBlue);
            btnAgregar.Location = new Point(15, 90);
            btnAgregar.Width = 270;
            btnAgregar.Click += (s, e) =>
            {
                var (exito, mensaje) = _usuarioService.AgregarAmigo(usuario.UsuarioId, (string)cmb.SelectedItem);
                MessageBox.Show(mensaje, exito ? "Amigo agregado" : "Aviso", MessageBoxButtons.OK, exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                if (exito) dialogo.DialogResult = DialogResult.OK;
            };

            dialogo.Controls.Add(lbl);
            dialogo.Controls.Add(cmb);
            dialogo.Controls.Add(btnAgregar);

            if (dialogo.ShowDialog(this) == DialogResult.OK)
                _mainForm.MostrarVista(new ProfileView(_mainForm)); // refresca contador de amigos
        }

        private void MostrarAmigos(Usuario usuario)
        {
            var amigos = _usuarioService.ObtenerAmigos(usuario.UsuarioId);
            if (amigos.Count == 0)
            {
                MessageBox.Show("Aún no tienes amigos agregados. Toca 'Add friends' para agregar a un usuario registrado.", "Friends", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialogo = new Form
            {
                Text = "Mis amigos",
                Size = new Size(340, 300),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = UiTheme.PanelDark
            };

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.PanelDark };
            int y = 8;
            foreach (var amigo in amigos)
            {
                var fila = new Panel { Location = new Point(0, y), Size = new Size(320, 38) };
                var lbl = new Label { Text = amigo.NombreUsuario, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 8), AutoSize = true, Font = UiTheme.FontSubtitle };

                var btnReportar = UiTheme.CreateStyledButton("🚩 Reportar", UiTheme.AccentRed);
                btnReportar.Size = new Size(92, 28);
                btnReportar.Location = new Point(118, 5);
                btnReportar.Click += (s, e) => { dialogo.Close(); ReportarUsuario(usuario, amigo.UsuarioId); };

                var btnEliminar = UiTheme.CreateStyledButton("Eliminar", UiTheme.PanelMid);
                btnEliminar.Size = new Size(84, 28);
                btnEliminar.Location = new Point(214, 5);
                btnEliminar.Click += (s, e) => { dialogo.Close(); EliminarAmigo(usuario, amigo.NombreUsuario); };

                fila.Controls.Add(lbl);
                fila.Controls.Add(btnReportar);
                fila.Controls.Add(btnEliminar);
                panel.Controls.Add(fila);
                y += 42;
            }

            dialogo.Controls.Add(panel);
            dialogo.ShowDialog(this);
        }

        private void EliminarAmigo(Usuario usuario, string nombreAmigo)
        {
            var confirmar = MessageBox.Show($"¿Dejar de seguir a '{nombreAmigo}'?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            var (exito, mensaje) = _usuarioService.EliminarAmigo(usuario.UsuarioId, nombreAmigo);
            MessageBox.Show(mensaje, exito ? "Amigo eliminado" : "Aviso", MessageBoxButtons.OK, exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (exito) _mainForm.MostrarVista(new ProfileView(_mainForm));
        }

        private void MostrarResumenPerfil(Usuario usuario)
        {
            int juegos = _bibliotecaService.ObtenerBibliotecaDeUsuario(usuario.UsuarioId).Count;
            MessageBox.Show(
                $"{usuario.NombreUsuario}\n{usuario.Rol}\n{juegos} juegos en biblioteca\n{usuario.Amigos.Count} amigos",
                "Perfil público", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ReportarUsuario(Usuario reportante, int objetivoIdFijo = -1)
        {
            var otros = _usuarioService.ObtenerOtrosUsuarios(reportante.UsuarioId);
            if (otros.Count == 0)
            {
                MessageBox.Show("No hay otros usuarios registrados todavía.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialogo = new Form
            {
                Text = "Reportar usuario",
                Size = new Size(340, 260),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = UiTheme.PanelDark
            };

            var lblUsuario = new Label { Text = "Usuario a reportar:", ForeColor = UiTheme.TextPrimary, Location = new Point(15, 15), AutoSize = true };
            var cmb = new ComboBox { Location = new Point(15, 40), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = UiTheme.PanelMid, ForeColor = UiTheme.TextPrimary };
            foreach (var u in otros) cmb.Items.Add(u.NombreUsuario);

            int indiceFijo = otros.FindIndex(u => u.UsuarioId == objetivoIdFijo);
            cmb.SelectedIndex = indiceFijo >= 0 ? indiceFijo : 0;

            var lblMotivo = new Label { Text = "Motivo:", ForeColor = UiTheme.TextPrimary, Location = new Point(15, 75), AutoSize = true };
            var txt = new TextBox { Location = new Point(15, 98), Width = 290, Height = 70, Multiline = true, BackColor = UiTheme.PanelMid, ForeColor = UiTheme.TextPrimary };

            var btnEnviar = UiTheme.CreateStyledButton("Enviar reporte", UiTheme.AccentRed);
            btnEnviar.Location = new Point(15, 178);
            btnEnviar.Width = 290;
            btnEnviar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    MessageBox.Show("Describe brevemente el motivo del reporte.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var objetivo = otros[cmb.SelectedIndex];
                _reporteService.ReportarUsuario(reportante, objetivo, txt.Text.Trim());
                MessageBox.Show("Gracias, tu reporte fue enviado al equipo de moderación.", "Reporte enviado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dialogo.Close();
            };

            dialogo.Controls.Add(lblUsuario);
            dialogo.Controls.Add(cmb);
            dialogo.Controls.Add(lblMotivo);
            dialogo.Controls.Add(txt);
            dialogo.Controls.Add(btnEnviar);
            dialogo.ShowDialog(this);
        }
    }
}
