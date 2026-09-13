using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>
    /// Carrito de compras: lista los juegos agregados, permite quitarlos y
    /// confirma la compra (descuenta saldo y entrega los juegos a la biblioteca).
    /// Cada vez que cambia el contenido, la vista se reconstruye por completo
    /// desde cero (más simple y robusto que ir agregando/quitando controles sueltos).
    /// </summary>
    public class CartView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly CarritoService _carritoService = new();

        public CartView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            Reconstruir(null);
        }

        /// <summary>Reconstruye toda la página desde cero. <paramref name="mensaje"/> se
        /// muestra abajo (ej. resultado del checkout o de quitar un ítem).</summary>
        private void Reconstruir(string? mensaje, bool esError = false)
        {
            Controls.Clear();

            var usuario = SessionManager.UsuarioActual!;
            var items = _carritoService.ObtenerCarrito(usuario.UsuarioId);

            var lblTitulo = new Label { Text = "Tu carrito", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 14), AutoSize = true };
            Controls.Add(lblTitulo);

            int y = 60;

            if (items.Count == 0)
            {
                Controls.Add(new Label
                {
                    Text = "Tu carrito está vacío.\nVe a la Tienda y toca \"Add to Cart\" en algún juego.",
                    ForeColor = UiTheme.TextSecondary,
                    Location = new Point(14, y),
                    AutoSize = true,
                    MaximumSize = new Size(370, 0)
                });
                y += 60;

                var btnIrTienda = UiTheme.CreateStyledButton("Ir a la Tienda", UiTheme.AccentBlue);
                btnIrTienda.Location = new Point(14, y);
                btnIrTienda.Width = 370;
                btnIrTienda.Click += (s, e) => _mainForm.MostrarVista(new StoreView(_mainForm));
                Controls.Add(btnIrTienda);
                return;
            }

            foreach (var item in items)
            {
                var fila = new Panel { Location = new Point(14, y), Size = new Size(370, 54), BackColor = UiTheme.PanelDark };
                var lblNombre = new Label { Text = item.Titulo, ForeColor = UiTheme.TextPrimary, Location = new Point(10, 17), AutoSize = true };
                var lblPrecio = new Label { Text = item.Precio.ToString("C"), ForeColor = UiTheme.AccentGreen, Location = new Point(220, 17), AutoSize = true };
                var btnQuitar = new Button { Text = "✕", Size = new Size(32, 32), Location = new Point(326, 11), FlatStyle = FlatStyle.Flat, BackColor = UiTheme.PanelMid, ForeColor = UiTheme.AccentRed };
                btnQuitar.FlatAppearance.BorderSize = 0;

                var juegoId = item.JuegoId; // captura local para el closure
                btnQuitar.Click += (s, e) =>
                {
                    _carritoService.QuitarDelCarrito(usuario.UsuarioId, juegoId);
                    _mainForm.ActualizarBadgeCarrito();
                    Reconstruir("Juego quitado del carrito.");
                };

                fila.Controls.AddRange(new Control[] { lblNombre, lblPrecio, btnQuitar });
                Controls.Add(fila);
                y += 62;
            }

            y += 10;
            var total = _carritoService.ObtenerTotal(usuario.UsuarioId);
            var lblTotal = new Label
            {
                Text = $"Total: {total:C}",
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontSubtitle,
                Location = new Point(14, y),
                AutoSize = true
            };
            Controls.Add(lblTotal);
            y += 28;

            var lblSaldo = new Label
            {
                Text = $"Saldo disponible: {usuario.Saldo:C}",
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(14, y),
                AutoSize = true
            };
            Controls.Add(lblSaldo);
            y += 36;

            var btnCheckout = UiTheme.CreateStyledButton("Confirmar compra", UiTheme.AccentGreen);
            btnCheckout.Location = new Point(14, y);
            btnCheckout.Width = 370;
            btnCheckout.Click += (s, e) =>
            {
                var (exito, msg) = _carritoService.Checkout(usuario);
                _mainForm.ActualizarBadgeCarrito();
                Reconstruir(msg, !exito);
            };
            Controls.Add(btnCheckout);
            y += 50;

            if (!string.IsNullOrEmpty(mensaje))
            {
                var lblMensaje = new Label
                {
                    Text = mensaje,
                    ForeColor = esError ? UiTheme.AccentRed : UiTheme.AccentGreen,
                    Location = new Point(14, y),
                    AutoSize = true,
                    MaximumSize = new Size(370, 0)
                };
                Controls.Add(lblMensaje);
            }
        }
    }
}
