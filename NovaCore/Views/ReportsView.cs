using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Reporte de ventas (solo administradores): total de ingresos, cantidad de
    /// compras completadas y ranking de juegos más vendidos, con una barra
    /// dibujada a mano (Graphics) para representar el ingreso relativo de cada
    /// título. El contenido se ajusta al ancho de la pantalla del programa (400px
    /// de diseño) en lugar del ancho de escritorio que usaba la versión anterior.
    /// </summary>
    public class ReportsView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly TransaccionService _transaccionService = new();
        private readonly Panel _barraSuperior;

        public ReportsView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;

            _barraSuperior = UiTheme.CreateBackBar("Reportes de Ventas", () => _mainForm.AbrirMenuPrincipal());

            if (!SessionManager.EsAdministrador)
            {
                Controls.Add(_barraSuperior);
                Controls.Add(new Label { Text = "Acceso restringido a administradores.", ForeColor = UiTheme.AccentRed, Location = new Point(14, 60), AutoSize = true });
                return;
            }

            ConstruirUI();
        }

        private void ConstruirUI()
        {
            Controls.Add(_barraSuperior);

            var ingresosTotales = _transaccionService.ObtenerIngresosTotales();
            var totalVentas = _transaccionService.ContarVentas();

            var cardIngresos = CrearTarjetaResumen("Ingresos totales", ingresosTotales.ToString("C"), UiTheme.AccentGreen, new Point(14, 60));
            var cardVentas = CrearTarjetaResumen("Compras realizadas", totalVentas.ToString(), UiTheme.AccentPrimary, new Point(200, 60));

            var lblRankingTitulo = new Label { Text = "Juegos más vendidos", Font = UiTheme.FontSubtitle, ForeColor = UiTheme.TextPrimary, Location = new Point(14, 176), AutoSize = true };

            var panelRanking = new Panel { Location = new Point(14, 206), Size = new Size(372, 320), AutoScroll = true };
            var ranking = _transaccionService.ObtenerRankingJuegos();

            if (ranking.Count == 0)
            {
                panelRanking.Controls.Add(new Label { Text = "Aún no se han registrado ventas.", ForeColor = UiTheme.TextSecondary, AutoSize = true });
            }
            else
            {
                decimal maxIngreso = ranking.Max(r => r.Ingresos);
                int y = 0;
                foreach (var (titulo, ventas, ingresos) in ranking)
                {
                    panelRanking.Controls.Add(CrearFilaRanking(titulo, ventas, ingresos, maxIngreso, y));
                    y += 46;
                }
            }

            Controls.AddRange(new Control[] { cardIngresos, cardVentas, lblRankingTitulo, panelRanking });
        }

        private Panel CrearTarjetaResumen(string titulo, string valor, Color acento, Point ubicacion)
        {
            var card = new Panel { Location = ubicacion, Size = new Size(180, 100), BackColor = UiTheme.PanelDark };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(acento, 2);
                e.Graphics.DrawLine(pen, 0, 0, 0, card.Height); // barra lateral de acento
            };

            var lblTitulo = new Label { Text = titulo, ForeColor = UiTheme.TextSecondary, Location = new Point(14, 14), AutoSize = true };
            var lblValor = new Label { Text = valor, ForeColor = acento, Font = new Font("Segoe UI", 18, FontStyle.Bold), Location = new Point(12, 42), AutoSize = true };
            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblValor);
            return card;
        }

        private Panel CrearFilaRanking(string titulo, int ventas, decimal ingresos, decimal maxIngreso, int y)
        {
            var fila = new Panel { Location = new Point(0, y), Size = new Size(356, 42) };

            var lblTitulo = new Label { Text = titulo, ForeColor = UiTheme.TextPrimary, Location = new Point(0, 4), Size = new Size(220, 18), AutoEllipsis = true };
            var lblVentas = new Label { Text = $"{ventas} ventas", ForeColor = UiTheme.TextSecondary, Font = new Font("Segoe UI", 8), Location = new Point(0, 24), Size = new Size(75, 15) };

            var barraFondo = new Panel { Location = new Point(80, 26), Size = new Size(190, 12), BackColor = UiTheme.PanelMid };
            double proporcion = maxIngreso > 0 ? (double)(ingresos / maxIngreso) : 0;
            var barraRelleno = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size((int)(190 * proporcion), 12),
                BackColor = UiTheme.AccentPrimary
            };
            barraFondo.Controls.Add(barraRelleno);

            var lblIngresos = new Label { Text = ingresos.ToString("C"), ForeColor = UiTheme.AccentGreen, Location = new Point(278, 4), Size = new Size(78, 18), TextAlign = ContentAlignment.TopRight };

            fila.Controls.AddRange(new Control[] { lblTitulo, lblVentas, barraFondo, lblIngresos });
            return fila;
        }
    }
}
