using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    /// <summary>
    /// Reporte simple de ingresos: total generado por todas las compras y un
    /// ranking de juegos más vendidos, con una barra dibujada a mano (Graphics)
    /// para representar el ingreso relativo de cada título sin depender de
    /// librerías externas de gráficos.
    /// </summary>
    public class ReportsView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly TransaccionService _transaccionService = new();

        public ReportsView(MainForm mainForm)
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
        }

        private void ConstruirUI()
        {
            var lblTitulo = new Label { Text = "Reportes", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, Location = new Point(0, 0), AutoSize = true };

            var ingresosTotales = _transaccionService.ObtenerIngresosTotales();
            var totalTransacciones = _transaccionService.ObtenerTodas().Count;

            var cardIngresos = CrearTarjetaResumen("Ingresos totales", ingresosTotales.ToString("C"), UiTheme.AccentGreen, new Point(0, 50));
            var cardTransacciones = CrearTarjetaResumen("Compras realizadas", totalTransacciones.ToString(), UiTheme.AccentBlue, new Point(260, 50));

            var lblRankingTitulo = new Label { Text = "Juegos más vendidos", Font = UiTheme.FontSubtitle, ForeColor = UiTheme.TextPrimary, Location = new Point(0, 190), AutoSize = true };

            var panelRanking = new Panel { Location = new Point(0, 220), Size = new Size(700, 300), AutoScroll = true };
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
                    y += 45;
                }
            }

            Controls.AddRange(new Control[] { lblTitulo, cardIngresos, cardTransacciones, lblRankingTitulo, panelRanking });
        }

        private Panel CrearTarjetaResumen(string titulo, string valor, Color acento, Point ubicacion)
        {
            var card = new Panel { Location = ubicacion, Size = new Size(240, 110), BackColor = UiTheme.PanelDark };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(acento, 2);
                e.Graphics.DrawLine(pen, 0, 0, 0, card.Height); // barra lateral de acento
            };

            var lblTitulo = new Label { Text = titulo, ForeColor = UiTheme.TextSecondary, Location = new Point(20, 15), AutoSize = true };
            var lblValor = new Label { Text = valor, ForeColor = acento, Font = new Font("Segoe UI", 22, FontStyle.Bold), Location = new Point(18, 40), AutoSize = true };
            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblValor);
            return card;
        }

        private Panel CrearFilaRanking(string titulo, int ventas, decimal ingresos, decimal maxIngreso, int y)
        {
            var fila = new Panel { Location = new Point(0, y), Size = new Size(680, 38) };

            var lblTitulo = new Label { Text = titulo, ForeColor = UiTheme.TextPrimary, Location = new Point(0, 8), Size = new Size(180, 20), AutoEllipsis = true };
            var lblVentas = new Label { Text = $"{ventas} ventas", ForeColor = UiTheme.TextSecondary, Location = new Point(185, 8), Size = new Size(80, 20) };

            var barraFondo = new Panel { Location = new Point(270, 10), Size = new Size(300, 16), BackColor = UiTheme.PanelMid };
            double proporcion = maxIngreso > 0 ? (double)(ingresos / maxIngreso) : 0;
            var barraRelleno = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size((int)(300 * proporcion), 16),
                BackColor = UiTheme.AccentBlue
            };
            barraFondo.Controls.Add(barraRelleno);

            var lblIngresos = new Label { Text = ingresos.ToString("C"), ForeColor = UiTheme.AccentGreen, Location = new Point(580, 8), AutoSize = true };

            fila.Controls.AddRange(new Control[] { lblTitulo, lblVentas, barraFondo, lblIngresos });
            return fila;
        }
    }
}
