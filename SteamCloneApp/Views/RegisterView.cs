using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    public class RegisterView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly UsuarioService _usuarioService = new();
        private readonly TextBox _txtUsuario = new();
        private readonly TextBox _txtCorreo = new();
        private readonly TextBox _txtPassword = new();
        private readonly TextBox _txtPassword2 = new();
        private readonly Label _lblMensaje = new();

        public RegisterView(MainForm mainForm)
        {
            _mainForm = mainForm;
            BackColor = UiTheme.BackgroundDark;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var card = new Panel { Size = new Size(400, 480), BackColor = UiTheme.PanelDark, Anchor = AnchorStyles.None };
            Controls.Add(card);
            Resize += (s, e) => CentrarCard(card);
            CentrarCard(card);

            var lblTitulo = new Label { Text = "Crear cuenta", Font = UiTheme.FontTitle, ForeColor = UiTheme.TextPrimary, AutoSize = true, Location = new Point(30, 25) };

            _txtUsuario.Location = new Point(30, 90); _txtUsuario.Width = 340; EstilizarTextBox(_txtUsuario);
            _txtCorreo.Location = new Point(30, 150); _txtCorreo.Width = 340; EstilizarTextBox(_txtCorreo);
            _txtPassword.Location = new Point(30, 210); _txtPassword.Width = 340; _txtPassword.UseSystemPasswordChar = true; EstilizarTextBox(_txtPassword);
            _txtPassword2.Location = new Point(30, 270); _txtPassword2.Width = 340; _txtPassword2.UseSystemPasswordChar = true; EstilizarTextBox(_txtPassword2);

            _lblMensaje.Location = new Point(30, 310);
            _lblMensaje.AutoSize = true;
            _lblMensaje.MaximumSize = new Size(340, 0);

            var btnRegistrar = UiTheme.CreateStyledButton("Registrarme", UiTheme.AccentBlue);
            btnRegistrar.Location = new Point(30, 360); btnRegistrar.Width = 340;
            btnRegistrar.Click += BtnRegistrar_Click;

            var btnVolver = UiTheme.CreateStyledButton("Volver a inicio de sesión", UiTheme.PanelMid);
            btnVolver.Location = new Point(30, 410); btnVolver.Width = 340;
            btnVolver.Click += (s, e) => _mainForm.MostrarLogin();

            card.Controls.AddRange(new Control[]
            {
                lblTitulo,
                CrearEtiqueta("Nombre de usuario", 70), _txtUsuario,
                CrearEtiqueta("Correo electrónico", 130), _txtCorreo,
                CrearEtiqueta("Contraseña", 190), _txtPassword,
                CrearEtiqueta("Confirmar contraseña", 250), _txtPassword2,
                _lblMensaje, btnRegistrar, btnVolver
            });
        }

        private void BtnRegistrar_Click(object? sender, EventArgs e)
        {
            if (_txtPassword.Text != _txtPassword2.Text)
            {
                MostrarMensaje("Las contraseñas no coinciden.", UiTheme.AccentRed);
                return;
            }

            var (exito, mensaje) = _usuarioService.Registrar(_txtUsuario.Text, _txtCorreo.Text, _txtPassword.Text);
            MostrarMensaje(mensaje, exito ? UiTheme.AccentGreen : UiTheme.AccentRed);

            if (exito)
            {
                var timer = new System.Windows.Forms.Timer { Interval = 1200 };
                timer.Tick += (s, e2) => { timer.Stop(); _mainForm.MostrarLogin(); };
                timer.Start();
            }
        }

        private void MostrarMensaje(string texto, Color color)
        {
            _lblMensaje.Text = texto;
            _lblMensaje.ForeColor = color;
        }

        private Label CrearEtiqueta(string texto, int y) => new()
        {
            Text = texto, ForeColor = UiTheme.TextSecondary, AutoSize = true, Location = new Point(30, y)
        };

        private static void EstilizarTextBox(TextBox txt)
        {
            txt.BackColor = UiTheme.PanelMid;
            txt.ForeColor = UiTheme.TextPrimary;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = UiTheme.FontBody;
            txt.Height = 30;
        }

        private void CentrarCard(Panel card)
        {
            card.Location = new Point((Width - card.Width) / 2, Math.Max(20, (Height - card.Height) / 2));
        }
    }
}
