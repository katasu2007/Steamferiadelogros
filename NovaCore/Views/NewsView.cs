namespace NovaCore.Views
{
    /// <summary>Sección de noticias (contenido estático de ejemplo, cumple con el
    /// ícono "Noticias" de la barra inferior siendo una página real y navegable).</summary>
    public class NewsView : UserControl
    {
        public NewsView(MainForm mainForm)
        {
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;

            var lblTitulo = new Label { Text = "Noticias", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 14), AutoSize = true };
            Controls.Add(lblTitulo);

            var noticias = new (string Titulo, string Resumen)[]
            {
                ("WARDOGS llega a Early Access", "Cien jugadores, tres equipos, un solo control zone. Ya disponible en la tienda."),
                ("Actualización de la billetera virtual", "Ahora puedes recargar tu saldo directamente desde la pestaña Billetera."),
                ("Nuevo sistema de reportes", "Ayúdanos a mantener la comunidad sana reportando juegos o usuarios desde su ficha."),
            };

            int y = 60;
            foreach (var (titulo, resumen) in noticias)
            {
                var card = new Panel { Location = new Point(14, y), Size = new Size(370, 90), BackColor = UiTheme.PanelDark };
                var lblT = new Label { Text = titulo, ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontSubtitle, Location = new Point(10, 10), AutoSize = true };
                var lblR = new Label { Text = resumen, ForeColor = UiTheme.TextSecondary, Location = new Point(10, 36), MaximumSize = new Size(350, 0), AutoSize = true };
                card.Controls.Add(lblT);
                card.Controls.Add(lblR);
                Controls.Add(card);
                y += 100;
            }
        }
    }
}
