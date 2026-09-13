namespace SteamCloneApp.Views
{
    /// <summary>Lista de deseos. Versión simplificada (no persiste selección de juegos
    /// todavía) pero cumple el flujo visual de la tab "WISHLIST" de Steam.</summary>
    public class WishlistView : UserControl
    {
        public WishlistView(MainForm mainForm)
        {
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;

            var lblTitulo = new Label { Text = "Wishlist", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 14), AutoSize = true };
            var lblVacio = new Label
            {
                Text = "Tu lista de deseos está vacía.\nToca el corazón en un juego (próximamente) para guardarlo aquí.",
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(14, 60),
                AutoSize = true,
                MaximumSize = new Size(370, 0)
            };

            Controls.Add(lblTitulo);
            Controls.Add(lblVacio);
        }
    }
}
