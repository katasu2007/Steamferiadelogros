using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>Notificaciones simuladas: confirma compras recientes y avisos generales.</summary>
    public class NotificationsView : UserControl
    {
        public NotificationsView(MainForm mainForm)
        {
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;

            var lblTitulo = new Label { Text = "Notifications", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 14), AutoSize = true };
            Controls.Add(lblTitulo);

            var transaccionService = new TransaccionService();
            var usuario = SessionManager.UsuarioActual!;
            var compras = transaccionService.ObtenerTodas().Where(t => t.UsuarioId == usuario.UsuarioId).Take(5).ToList();

            int y = 60;

            if (compras.Count == 0)
            {
                Controls.Add(new Label { Text = "No tienes notificaciones todavía.", ForeColor = UiTheme.TextSecondary, Location = new Point(14, y), AutoSize = true });
                return;
            }

            foreach (var compra in compras)
            {
                var card = new Panel { Location = new Point(14, y), Size = new Size(370, 60), BackColor = UiTheme.PanelDark };
                var lbl = new Label
                {
                    Text = $"✅ Compraste '{compra.JuegoTitulo}' por {compra.Monto:C}",
                    ForeColor = UiTheme.TextPrimary,
                    Location = new Point(10, 10),
                    AutoSize = true
                };
                var lblFecha = new Label { Text = compra.Fecha.ToString("dd/MM/yyyy HH:mm"), ForeColor = UiTheme.TextSecondary, Location = new Point(10, 32), AutoSize = true };
                card.Controls.Add(lbl);
                card.Controls.Add(lblFecha);
                Controls.Add(card);
                y += 70;
            }
        }
    }
}
