using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Panel de administración de usuarios: lista todos los usuarios registrados,
    /// permite cambiar su rol (Usuario/Administrador) y activar/desactivar (banear) cuentas.
    /// </summary>
    public class UserManagementView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly UsuarioService _usuarioService = new();
        private readonly DataGridView _grid = new();

        public UserManagementView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;

            if (!SessionManager.EsAdministrador)
            {
                Controls.Add(new Label { Text = "Acceso restringido a administradores.", ForeColor = UiTheme.AccentRed, AutoSize = true });
                return;
            }

            ConstruirUI();
            CargarGrid();
        }

        private void ConstruirUI()
        {
            var barraTitulo = UiTheme.CreateBackBar("Gestión de Usuarios", () => _mainForm.AbrirMenuPrincipal());

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
            var btnCambiarRol = UiTheme.CreateStyledButton("Cambiar rol", UiTheme.AccentPrimary);
            btnCambiarRol.Width = 150;
            btnCambiarRol.Click += (s, e) => CambiarRolSeleccionado();

            var btnToggleActivo = UiTheme.CreateStyledButton("Banear / Reactivar", UiTheme.AccentRed);
            btnToggleActivo.Width = 170;
            btnToggleActivo.Click += (s, e) => ToggleActivoSeleccionado();

            toolbar.Controls.AddRange(new Control[] { btnCambiarRol, btnToggleActivo });

            UiTheme.StyleDataGridView(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UsuarioId", HeaderText = "ID", Width = 50 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NombreUsuario", HeaderText = "Usuario" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Correo", HeaderText = "Correo" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rol", HeaderText = "Rol" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Saldo", HeaderText = "Saldo" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado" });

            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(barraTitulo); // último control -> queda al tope (docking)
        }

        private void CargarGrid()
        {
            _grid.Rows.Clear();
            foreach (var u in _usuarioService.ObtenerTodos().OrderBy(u => u.UsuarioId))
            {
                int idx = _grid.Rows.Add(u.UsuarioId, u.NombreUsuario, u.Correo, u.Rol, u.Saldo.ToString("C"), u.Activo ? "Activo" : "Baneado");
                _grid.Rows[idx].Tag = u.UsuarioId;
                _grid.Rows[idx].DefaultCellStyle.ForeColor = u.Activo ? UiTheme.TextPrimary : UiTheme.AccentRed;
            }
        }

        private void CambiarRolSeleccionado()
        {
            if (!ObtenerUsuarioSeleccionado(out var usuarioId, out var usuario)) return;

            if (usuario!.UsuarioId == SessionManager.UsuarioActual!.UsuarioId)
            {
                MessageBox.Show("No puedes cambiar tu propio rol mientras tienes la sesión activa.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var nuevoRol = usuario.Rol == RolUsuario.Administrador ? RolUsuario.Usuario : RolUsuario.Administrador;
            var confirm = MessageBox.Show(
                $"¿Cambiar el rol de '{usuario.NombreUsuario}' a {nuevoRol}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _usuarioService.CambiarRol(usuarioId, nuevoRol);
                CargarGrid();
            }
        }

        private void ToggleActivoSeleccionado()
        {
            if (!ObtenerUsuarioSeleccionado(out var usuarioId, out var usuario)) return;

            if (usuario!.UsuarioId == SessionManager.UsuarioActual!.UsuarioId)
            {
                MessageBox.Show("No puedes banear tu propia cuenta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _usuarioService.ToggleActivo(usuarioId);
            CargarGrid();
        }

        private bool ObtenerUsuarioSeleccionado(out int usuarioId, out Usuario? usuario)
        {
            usuarioId = 0;
            usuario = null;

            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecciona un usuario de la lista primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            usuarioId = (int)_grid.SelectedRows[0].Tag!;
            int idSeleccionado = usuarioId;
            usuario = _usuarioService.ObtenerTodos().FirstOrDefault(u => u.UsuarioId == idSeleccionado);
            return usuario != null;
        }
    }
}
