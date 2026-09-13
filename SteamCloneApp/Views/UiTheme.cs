namespace SteamCloneApp.Views
{
    /// <summary>
    /// Paleta de colores y helpers de estilo compartidos por todas las vistas,
    /// inspirada en el modo oscuro de Steam (grises azulados + acento cian/azul).
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color BackgroundDark = Color.FromArgb(23, 26, 33);
        public static readonly Color PanelDark = Color.FromArgb(30, 34, 44);
        public static readonly Color PanelMid = Color.FromArgb(42, 47, 61);
        public static readonly Color BorderColor = Color.FromArgb(58, 64, 82);
        public static readonly Color TextPrimary = Color.FromArgb(230, 233, 240);
        public static readonly Color TextSecondary = Color.FromArgb(150, 160, 175);
        public static readonly Color AccentBlue = Color.FromArgb(46, 148, 255);
        public static readonly Color AccentGreen = Color.FromArgb(87, 203, 108);
        public static readonly Color AccentRed = Color.FromArgb(224, 82, 82);
        public static readonly Font FontTitle = new("Segoe UI", 16, FontStyle.Bold);
        public static readonly Font FontSubtitle = new("Segoe UI", 11, FontStyle.Bold);
        public static readonly Font FontBody = new("Segoe UI", 9.5f);

        /// <summary>Crea un botón con estilo plano, coloreado con el acento indicado.</summary>
        public static Button CreateStyledButton(string text, Color backColor, Color? foreColor = null)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor ?? Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = FontSubtitle,
                Cursor = Cursors.Hand,
                Height = 38
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.15f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.1f);
            return btn;
        }

        /// <summary>Aplica estilo oscuro consistente a un DataGridView.</summary>
        public static void StyleDataGridView(DataGridView grid)
        {
            grid.BackgroundColor = PanelDark;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = BorderColor;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersDefaultCellStyle.BackColor = PanelMid;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = FontSubtitle;
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.BackColor = PanelDark;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = AccentBlue;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = FontBody;
            grid.RowTemplate.Height = 32;
        }
    }
}
