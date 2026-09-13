using SteamCloneApp.Models;
using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>
    /// Panel de moderación: lista los reportes enviados por los usuarios contra
    /// juegos o contra otros usuarios, y permite marcarlos como revisados.
    /// Solo accesible para Administradores.
    /// </summary>
    public class AdminReportsView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly ReporteService _reporteService = new();
        private readonly UsuarioService _usuarioService = new();
        private readonly DataGridView _grid = new();

        public AdminReportsView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;

            if (!SessionManager.EsAdministrador)
            {
                Controls.Add(new Label { Text = "Acceso restringido a administradores.", ForeColor = UiTheme.AccentRed, AutoSize = true, Location = new Point(14, 14) });
                return;
            }

            ConstruirUI();
            CargarGrid();
        }

        private void ConstruirUI()
        {
            var lblTitulo = new Label { Text = "Moderación", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Dock = DockStyle.Top, Height = 40, Padding = new Padding(14, 8, 0, 0) };

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(10, 0, 0, 0) };
            var btnRevisado = UiTheme.CreateStyledButton("Marcar como revisado", UiTheme.AccentBlue);
            btnRevisado.Width = 190;
            btnRevisado.Click += (s, e) => MarcarRevisado();

            var btnBanear = UiTheme.CreateStyledButton("Banear usuario reportado", UiTheme.AccentRed);
            btnBanear.Width = 190;
            btnBanear.Click += (s, e) => BanearUsuarioSeleccionado();

            toolbar.Controls.AddRange(new Control[] { btnRevisado, btnBanear });

            UiTheme.StyleDataGridView(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo", HeaderText = "Tipo", Width = 70 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Objetivo", HeaderText = "Reportado" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reportante", HeaderText = "Reportado por" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Motivo", HeaderText = "Motivo" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Estado", HeaderText = "Estado", Width = 90 });

            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(lblTitulo);
        }

        private void CargarGrid()
        {
            _grid.Rows.Clear();
            foreach (var r in _reporteService.ObtenerTodos())
            {
                int idx = _grid.Rows.Add(r.Tipo, r.ObjetivoNombre, r.UsuarioReportanteNombre, r.Motivo, r.Estado);
                _grid.Rows[idx].Tag = r;
                if (r.Estado == Models.EstadoReporte.Revisado)
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = UiTheme.TextSecondary;
            }

            if (_grid.Rows.Count == 0)
                _grid.Rows.Add("—", "Sin reportes todavía", "—", "—", "—");
        }

        private void MarcarRevisado()
        {
            if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Reporte reporte)
            {
                MessageBox.Show("Selecciona un reporte de la lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _reporteService.MarcarRevisado(reporte.ReporteId);
            CargarGrid();
        }

        private void BanearUsuarioSeleccionado()
        {
            if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not Reporte reporte)
            {
                MessageBox.Show("Selecciona un reporte de la lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (reporte.Tipo != Models.TipoReporte.Usuario)
            {
                MessageBox.Show("Esta acción solo aplica a reportes de tipo Usuario.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirmar = MessageBox.Show($"¿Banear a '{reporte.ObjetivoNombre}'?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmar == DialogResult.Yes)
            {
                _usuarioService.ToggleActivo(reporte.ObjetivoId);
                _reporteService.MarcarRevisado(reporte.ReporteId);
                CargarGrid();
                MessageBox.Show("Usuario baneado.", "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
