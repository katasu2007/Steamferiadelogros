using NovaCore.Services;
using System.Text.RegularExpressions;

namespace NovaCore.Views
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

        private static readonly Regex RegexCorreo = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Color ColorInvalid = Color.FromArgb(80, 40, 46);

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
            _txtUsuario.MaxLength = 20;
            _txtUsuario.KeyPress += TxtUsuario_KeyPress;
            _txtUsuario.TextChanged += (s, e) => SanearUsuario();

            _txtCorreo.Location = new Point(30, 150); _txtCorreo.Width = 340; EstilizarTextBox(_txtCorreo);
            _txtCorreo.TextChanged += (s, e) => MarcarValidez(_txtCorreo, EsCorreoValido(_txtCorreo.Text.Trim()));

            _txtPassword.Location = new Point(30, 210); _txtPassword.Width = 340; _txtPassword.UseSystemPasswordChar = true; EstilizarTextBox(_txtPassword);
            _txtPassword.TextChanged += (s, e) => MarcarValidez(_txtPassword, ContraseniaValida(_txtPassword.Text));

            _txtPassword2.Location = new Point(30, 270); _txtPassword2.Width = 340; _txtPassword2.UseSystemPasswordChar = true; EstilizarTextBox(_txtPassword2);
            _txtPassword2.TextChanged += (s, e) => MarcarValidez(_txtPassword2, _txtPassword2.Text.Length > 0 && _txtPassword2.Text == _txtPassword.Text);

            _lblMensaje.Location = new Point(30, 310);
            _lblMensaje.AutoSize = true;
            _lblMensaje.MaximumSize = new Size(340, 0);

            var btnRegistrar = UiTheme.CreateStyledButton("Registrarme", UiTheme.AccentPrimary);
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
            // 1) Validación en la vista: campos obligatorios no vacíos.
            string usuario = _txtUsuario.Text.Trim();
            string correo = _txtCorreo.Text.Trim();
            string password = _txtPassword.Text;
            string password2 = _txtPassword2.Text;

            MarcarValidez(_txtUsuario, EsNombreUsuarioValido(usuario));
            MarcarValidez(_txtCorreo, EsCorreoValido(correo));
            MarcarValidez(_txtPassword, ContraseniaValida(password));
            MarcarValidez(_txtPassword2, password2.Length > 0 && password2 == password);

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(correo)
                || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(password2))
            {
                MostrarMensaje("Todos los campos son obligatorios.", UiTheme.AccentRed);
                return;
            }

            if (!EsNombreUsuarioValido(usuario))
            {
                MostrarMensaje("El usuario debe tener entre 3 y 20 caracteres (letras, números o guion bajo).", UiTheme.AccentRed);
                return;
            }

            // 2) El correo debe tener un formato válido.
            if (!EsCorreoValido(correo))
            {
                MostrarMensaje("Ingresa un correo electrónico válido (ej. nombre@dominio.com).", UiTheme.AccentRed);
                return;
            }

            if (!ContraseniaValida(password))
            {
                MostrarMensaje("La contraseña debe combinar letras y números (mínimo 4 caracteres).", UiTheme.AccentRed);
                return;
            }

            // 3) La contraseña y su confirmación deben coincidir exactamente.
            if (password != password2)
            {
                MostrarMensaje("Las contraseñas no coinciden.", UiTheme.AccentRed);
                return;
            }

            // 4) Registro en el backend (el servicio valida duplicados y hashea la contraseña).
            var (exito, mensaje) = _usuarioService.Registrar(usuario, correo, password);
            MostrarMensaje(mensaje, exito ? UiTheme.AccentGreen : UiTheme.AccentRed);

            // 5) Tras registrarse con éxito, se redirige automáticamente al Login.
            if (exito)
            {
                var timer = new System.Windows.Forms.Timer { Interval = 1200 };
                timer.Tick += (s, e2) => { timer.Stop(); _mainForm.MostrarLogin(); };
                timer.Start();
            }
        }

        private void TxtUsuario_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!(char.IsLetterOrDigit(e.KeyChar) || e.KeyChar == '_'))
                e.Handled = true;
        }

        private void SanearUsuario()
        {
            var s = new string(_txtUsuario.Text.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            if (s != _txtUsuario.Text)
            {
                int pos = _txtUsuario.SelectionStart;
                _txtUsuario.Text = s;
                _txtUsuario.SelectionStart = Math.Min(pos, s.Length);
            }
            MarcarValidez(_txtUsuario, EsNombreUsuarioValido(s));
        }

        private static bool EsNombreUsuarioValido(string nombre)
        {
            nombre = nombre.Trim();
            return nombre.Length >= 3
                && nombre.Length <= 20
                && nombre.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        private static bool EsCorreoValido(string correo) => RegexCorreo.IsMatch(correo);

        private static bool ContraseniaValida(string password) =>
            password.Any(char.IsLetter) && password.Any(char.IsDigit);

        private static void MarcarValidez(TextBox txt, bool valido)
        {
            if (txt.Text.Length == 0) { txt.BackColor = UiTheme.PanelMid; return; }
            txt.BackColor = valido ? UiTheme.PanelMid : ColorInvalid;
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
