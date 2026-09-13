using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>Billetera virtual: muestra el saldo actual y permite recargar montos predefinidos.</summary>
    public class WalletView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly UsuarioService _usuarioService = new();
        private Label _lblSaldo = new();

        public WalletView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var lblTitulo = new Label { Text = "Wallet", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 14), AutoSize = true };

            var tarjeta = new Panel { Location = new Point(14, 60), Size = new Size(370, 100), BackColor = UiTheme.PanelDark };
            var lblLabel = new Label { Text = "Saldo disponible", ForeColor = UiTheme.TextSecondary, Location = new Point(15, 15), AutoSize = true };
            _lblSaldo = new Label { Text = SessionManager.UsuarioActual!.Saldo.ToString("C"), ForeColor = UiTheme.AccentGreen, Font = new Font("Segoe UI", 26, FontStyle.Bold), Location = new Point(15, 38), AutoSize = true };
            tarjeta.Controls.Add(lblLabel);
            tarjeta.Controls.Add(_lblSaldo);

            var lblAgregar = new Label { Text = "Agregar fondos", Font = UiTheme.FontSubtitle, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 180), AutoSize = true };

            var flow = new FlowLayoutPanel { Location = new Point(14, 215), Width = 370, Height = 60 };
            foreach (var monto in new decimal[] { 10, 25, 50, 100 })
            {
                var btn = UiTheme.CreateStyledButton(monto.ToString("C0"), UiTheme.AccentBlue);
                btn.Width = 80;
                btn.Click += (s, e) => AgregarFondos(monto);
                flow.Controls.Add(btn);
            }

            Controls.AddRange(new Control[] { lblTitulo, tarjeta, lblAgregar, flow });
        }

        private void AgregarFondos(decimal monto)
        {
            _usuarioService.RecargarSaldo(SessionManager.UsuarioActual!.UsuarioId, monto);
            _lblSaldo.Text = SessionManager.UsuarioActual!.Saldo.ToString("C");
            MessageBox.Show($"Se agregaron {monto:C} a tu billetera.", "Fondos agregados", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
