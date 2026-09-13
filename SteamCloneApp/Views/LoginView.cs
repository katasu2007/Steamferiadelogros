using SteamCloneApp.Services;

namespace SteamCloneApp.Views
{
    public class LoginView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly UsuarioService _usuarioService = new();
        private readonly TextBox _txtUsuario = new();
        private readonly TextBox _txtPassword = new();
        private readonly Label _lblError = new();

        public LoginView(MainForm mainForm)
        {
            _mainForm = mainForm;
            BackColor = UiTheme.BackgroundDark;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var card = new Panel
            {
                Size = new Size(380, 420),
                BackColor = UiTheme.PanelDark,
                Anchor = AnchorStyles.None
            };
            Controls.Add(card);
            Resize += (s, e) => CentrarCard(card);
            CentrarCard(card);

            var lblTitulo = new Label
            {
                Text = "Iniciar sesión",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(30, 30)
            };

            var lblSub = new Label
            {
                Text = "Inicia sesión con tu cuenta o regístrate como usuario nuevo",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(30, 65)
            };

            var lblUsuario = CrearEtiquetaCampo("Usuario", 110);
            _txtUsuario.Location = new Point(30, 132);
            _txtUsuario.Width = 320;
            EstilizarTextBox(_txtUsuario);

            var lblPassword = CrearEtiquetaCampo("Contraseña", 170);
            _txtPassword.Location = new Point(30, 192);
            _txtPassword.Width = 320;
            _txtPassword.UseSystemPasswordChar = true;
            EstilizarTextBox(_txtPassword);

            _lblError.Location = new Point(30, 230);
            _lblError.ForeColor = UiTheme.AccentRed;
            _lblError.AutoSize = true;
            _lblError.MaximumSize = new Size(320, 0);

            var btnLogin = UiTheme.CreateStyledButton("Iniciar sesión", UiTheme.AccentBlue);
            btnLogin.Location = new Point(30, 270);
            btnLogin.Width = 320;
            btnLogin.Click += BtnLogin_Click;

            var btnIrRegistro = UiTheme.CreateStyledButton("Crear cuenta nueva", UiTheme.PanelMid);
            btnIrRegistro.Location = new Point(30, 320);
            btnIrRegistro.Width = 320;
            btnIrRegistro.Click += (s, e) => _mainForm.MostrarRegistro();

            card.Controls.AddRange(new Control[]
            {
                lblTitulo, lblSub, lblUsuario, _txtUsuario, lblPassword, _txtPassword,
                _lblError, btnLogin, btnIrRegistro
            });

            _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnLogin_Click(s!, e); };
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            _lblError.Text = string.Empty;
            try
            {
                var usuario = _usuarioService.Login(_txtUsuario.Text.Trim(), _txtPassword.Text);
                if (usuario == null)
                {
                    _lblError.Text = "Usuario o contraseña incorrectos.";
                    return;
                }

                SessionManager.IniciarSesion(usuario);
                _mainForm.AlIniciarSesionExitoso();
            }
            catch (InvalidOperationException ex)
            {
                _lblError.Text = ex.Message;
            }
        }

        private Label CrearEtiquetaCampo(string texto, int y)
        {
            return new Label
            {
                Text = texto,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(30, y)
            };
        }

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
