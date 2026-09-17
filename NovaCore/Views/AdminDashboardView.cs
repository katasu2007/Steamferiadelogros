using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Panel de administración de juegos: Crear, Leer, Modificar y Eliminar (lógico)
    /// títulos del catálogo. Solo accesible cuando SessionManager.EsAdministrador es true
    /// (MainForm ya se encarga de no mostrar esta opción a usuarios comunes).
    /// </summary>
    public class AdminDashboardView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly JuegoService _juegoService = new();
        private readonly DataGridView _grid = new();

        public AdminDashboardView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;

            // Segunda barrera de seguridad a nivel de vista: si por algún flujo
            // alguien sin rol Administrador llega aquí, se bloquea el acceso.
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
            var dstBarra = UiTheme.CreateBackBar("Gestión de Juegos", () => _mainForm.AbrirMenuPrincipal());

            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, FlowDirection = FlowDirection.LeftToRight };
            var btnNuevo = UiTheme.CreateStyledButton("+ Nuevo juego", UiTheme.AccentPrimary);
            btnNuevo.Width = 150;
            btnNuevo.Click += (s, e) => AbrirFormulario(null);

            var btnEditar = UiTheme.CreateStyledButton("Editar seleccionado", UiTheme.PanelMid);
            btnEditar.Width = 170;
            btnEditar.Click += (s, e) => EditarSeleccionado();

            var btnEliminar = UiTheme.CreateStyledButton("Eliminar seleccionado", UiTheme.AccentRed);
            btnEliminar.Width = 180;
            btnEliminar.Click += (s, e) => EliminarSeleccionado();

            toolbar.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnEliminar });

            UiTheme.StyleDataGridView(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "JuegoId", HeaderText = "ID", Width = 50 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Titulo", HeaderText = "Título" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CategoriaNombre", HeaderText = "Categoría" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", HeaderText = "Precio" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Desarrollador", HeaderText = "Desarrollador" });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Activo", HeaderText = "Activo" });
            _grid.CellDoubleClick += (s, e) => EditarSeleccionado();

            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(dstBarra); // último control -> queda al tope (docking)
        }

        private void CargarGrid()
        {
            _grid.Rows.Clear();
            foreach (var j in _juegoService.ObtenerTodosIncluyendoInactivos().OrderBy(j => j.JuegoId))
            {
                int idx = _grid.Rows.Add(j.JuegoId, j.Titulo, j.CategoriaNombre, j.Precio.ToString("C"), j.Desarrollador, j.Activo ? "Sí" : "No");
                _grid.Rows[idx].Tag = j.JuegoId;
                if (!j.Activo) _grid.Rows[idx].DefaultCellStyle.ForeColor = UiTheme.TextSecondary;
            }
        }

        private void AbrirFormulario(Juego? juegoExistente)
        {
            var form = new GameFormView(_juegoService, juegoExistente, _mainForm);
            form.GuardadoExitoso += (s, e) => CargarGrid();
            _mainForm.MostrarVista(form);
        }

        private void EditarSeleccionado()
        {
            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecciona un juego de la lista primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var juegoId = (int)_grid.SelectedRows[0].Tag!;
            var juego = _juegoService.ObtenerPorId(juegoId);
            if (juego != null) AbrirFormulario(juego);
        }

        private void EliminarSeleccionado()
        {
            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecciona un juego de la lista primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var juegoId = (int)_grid.SelectedRows[0].Tag!;
            var confirmacion = MessageBox.Show(
                "¿Seguro que deseas eliminar este juego? Se ocultará del catálogo (baja lógica).",
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirmacion == DialogResult.Yes)
            {
                _juegoService.Eliminar(juegoId);
                CargarGrid();
            }
        }
    }
}
