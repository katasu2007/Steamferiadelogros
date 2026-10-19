using System.Globalization;
using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Billetera virtual estilo Steam. Muestra el saldo, permite elegir un monto
    /// (predefinido o personalizado) y abrir una pasarela de pago simulada en un
    /// overlay a pantalla completa dentro de la app (tarjeta validada en tiempo
    /// real, código de tarjeta de regalo/PayPal), con pantalla de confirmación,
    /// indicador de "Procesando pago...", persistencia del saldo y de la
    /// transacción + notificación, y un historial de movimientos recientes.
    /// </summary>
    public class WalletView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly UsuarioService _usuarioService = new();
        private readonly TransaccionService _transaccionService = new();

        private Label _lblSaldo = new();
        private Panel _panelHistorial = new();
        private Panel _panelContenido = new();
        private Button _btnContinuar = new();

        // Monto elegido: 0 = personalizado (se lee del TextBox).
        private decimal _montoSeleccionado = 25m;
        private TextBox _txtMontoPersonalizado = new();
        private Label _lblErrorMonto = new();

        private readonly Dictionary<decimal, Panel> _tarjetasMonto = new();

        // --- Layout responsivo de la pasarela de pago ---
        // Margen lateral interno (en unidades de diseño) a cada lado del formulario.
        private const int MargenLateral = 15;

        /// <summary>Ancho de diseño de la app (400 px) escalado por <see cref="UiTheme.EscalaUI"/>.</summary>
        private static int AnchoEscalado(int valor) =>
            (int)Math.Round(valor * UiTheme.EscalaUI);

        public WalletView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            var barraSuperior = UiTheme.CreateBackBar("Billetera", () => _mainForm.MostrarVista(new StoreView(_mainForm)));
            Controls.Add(barraSuperior);

            _panelContenido = new Panel { Location = new Point(0, 48), AutoScroll = false };
            Controls.Add(_panelContenido);
            AjustarPanelContenido();

            var contenedor = _panelContenido;
            int y = 12;

            // ----- Tarjeta de saldo -----
            var tarjeta = new Panel { Location = new Point(14, y), Size = new Size(372, 104), BackColor = UiTheme.PanelDark };
            tarjeta.Controls.Add(new Label { Text = "Saldo disponible", ForeColor = UiTheme.TextSecondary, Location = new Point(16, 14), AutoSize = true });
            _lblSaldo = new Label
            {
                Text = SaldoActual().ToString("C"),
                ForeColor = UiTheme.AccentGreen,
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                Location = new Point(16, 38),
                AutoSize = true
            };
            tarjeta.Controls.Add(_lblSaldo);
            contenedor.Controls.Add(tarjeta);
            y += 120;

            // ----- Selección de monto -----
            contenedor.Controls.Add(Seccion("1. Elige el monto", y));
            y += 26;

            var flowMontos = new FlowLayoutPanel { Location = new Point(14, y), Width = 372, Height = 66, WrapContents = false };
            foreach (var monto in new decimal[] { 10, 25, 50, 100 })
                flowMontos.Controls.Add(CrearTarjetaMonto(monto));
            contenedor.Controls.Add(flowMontos);
            y += 76;

            // Monto personalizado
            contenedor.Controls.Add(new Label
            {
                Text = "Monto personalizado ($5.00 – $500.00)",
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(14, y),
                AutoSize = true
            });
            y += 20;

            _txtMontoPersonalizado = new TextBox
            {
                Location = new Point(14, y),
                Width = 200,
                BackColor = UiTheme.PanelMid,
                ForeColor = UiTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = UiTheme.FontBody
            };
            _txtMontoPersonalizado.TextChanged += (s, e) =>
            {
                // Al escribir un monto válido se deseleccionan las tarjetas fijas.
                if (decimal.TryParse(_txtMontoPersonalizado.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    SeleccionarMonto(0m);
                ActualizarBotonContinuar();
            };
            contenedor.Controls.Add(_txtMontoPersonalizado);
            y += 24;

            _lblErrorMonto = new Label { Location = new Point(14, y), AutoSize = true, ForeColor = UiTheme.AccentRed };
            contenedor.Controls.Add(_lblErrorMonto);
            y += 22;

            // ----- Botón continuar con el pago -----
            _btnContinuar = UiTheme.CreateStyledButton("Continuar con el pago", UiTheme.AccentPrimary);
            _btnContinuar.Location = new Point(14, y);
            _btnContinuar.Width = 372;
            _btnContinuar.Height = 44;
            _btnContinuar.Click += (s, e) => IniciarPasarela();
            contenedor.Controls.Add(_btnContinuar);
            y += 58;

            // ----- Historial de movimientos -----
            contenedor.Controls.Add(Seccion("Historial de Movimientos", y));
            y += 26;

            _panelHistorial = new Panel { Location = new Point(14, y), Width = 372, AutoScroll = true };
            _panelHistorial.Height = 160;
            _yHistorial = y;
            contenedor.Controls.Add(_panelHistorial);

            CargarHistorial();
            ActualizarAlturaHistorial();
            AjustarAltoContenido();

            SeleccionarMonto(25m);
        }

        // ============================ PANEL DE CONTENIDO ============================

        /// <summary>El panel de contenido ocupa el ancho/alto disponible bajo la barra superior.</summary>
        private void AjustarPanelContenido()
        {
            int alto = Math.Max(this.ClientSize.Height - 48, 0);
            _panelContenido.Size = new Size(Math.Max(this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth, 0), alto);
        }

        /// <summary>Da al panel de contenido la altura justa para alojar todo (el scroll lo
        /// gestiona el propio UserControl, que tiene AutoScroll activado).</summary>
        private int _yHistorial;

        private void AjustarAltoContenido()
        {
            int necesario = Math.Max(_yHistorial + _panelHistorial.Height + 20, this.ClientSize.Height - 48);
            _panelContenido.Height = necesario;
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            if (_panelContenido == null || _panelContenido.IsDisposed) return;
            AjustarPanelContenido();
            AjustarAltoContenido();
        }

        // ============================ TARJETAS DE MONTO ============================

        private Panel CrearTarjetaMonto(decimal monto)
        {
            var card = new Panel
            {
                Size = new Size(86, 60),
                Margin = new Padding(3),
                BackColor = UiTheme.PanelMid,
                Cursor = Cursors.Hand,
                Tag = monto
            };
            card.Controls.Add(new Label
            {
                Text = monto.ToString("C0"),
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });

            card.Click += (s, e) => SeleccionarMonto(monto);
            foreach (Control hijo in card.Controls) hijo.Click += (s, e) => SeleccionarMonto(monto);

            card.Paint += (s, e) =>
            {
                bool sel = (decimal)card.Tag == _montoSeleccionado;
                using var pen = new Pen(sel ? UiTheme.AccentPrimary : UiTheme.BorderColor, sel ? 2f : 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            _tarjetasMonto[monto] = card;
            return card;
        }

        private void SeleccionarMonto(decimal monto)
        {
            _montoSeleccionado = monto;
            foreach (var card in _tarjetasMonto.Values)
            {
                card.BackColor = (decimal)card.Tag == monto ? UiTheme.PanelDark : UiTheme.PanelMid;
                card.Invalidate();
            }

            if (monto != 0m) _txtMontoPersonalizado.Text = string.Empty;
            ActualizarBotonContinuar();
        }

        /// <summary>Devuelve el monto vigente (personalizado si el usuario escribió uno válido).</summary>
        private decimal MontoEfectivo()
        {
            if (_montoSeleccionado != 0m) return _montoSeleccionado;

            var texto = _txtMontoPersonalizado.Text.Replace(',', '.').Trim();
            if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal m))
                return m;
            return 0m;
        }

        private void ActualizarBotonContinuar()
        {
            var monto = MontoEfectivo();
            bool valido = monto >= UsuarioService.MontoMinimoRecarga && monto <= UsuarioService.MontoMaximoRecarga;
            _btnContinuar.Enabled = valido;

            if (_montoSeleccionado == 0m && _txtMontoPersonalizado.TextLength > 0 && !valido)
            {
                _lblErrorMonto.Text = $"El monto debe estar entre {UsuarioService.MontoMinimoRecarga:C} y {UsuarioService.MontoMaximoRecarga:C}.";
            }
            else
            {
                _lblErrorMonto.Text = string.Empty;
            }
        }

        // ============================ PASARELA DE PAGO (OVERLAY) ============================

        private void IniciarPasarela()
        {
            var monto = MontoEfectivo();
            if (monto < UsuarioService.MontoMinimoRecarga || monto > UsuarioService.MontoMaximoRecarga)
            {
                _lblErrorMonto.Text = $"El monto debe estar entre {UsuarioService.MontoMinimoRecarga:C} y {UsuarioService.MontoMaximoRecarga:C}.";
                return;
            }

            var overlay = CrearOverlay();
            var (paso, mostrar) = ConstruirPasoMetodo(overlay, monto);
            overlay.Controls.Add(paso);
            Controls.Add(overlay);
            overlay.BringToFront();
            mostrar();
        }

        /// <summary>Crea un overlay a pantalla completa que bloquea la vista de fondo.</summary>
        private Panel CrearOverlay()
        {
            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.BackgroundDark,
                AutoScroll = true
            };
            return overlay;
        }

        /// <summary>Paso 1: elegir y rellenar el método de pago. Devuelve el panel y un
        /// lambda que da el foco al primer campo.</summary>
        private (Panel, Action) ConstruirPasoMetodo(Panel overlay, decimal monto)
        {
            // El panel raíz hace scroll y aloja una barra superior fija + contenido.
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            var barra = UiTheme.CreateBackBar($"Pagar {monto:C}", () => CancelarOverlay(overlay));
            panel.Controls.Add(barra);

            // Contenido con margen lateral; el ancho se recalcula al redimensionar.
            var contenido = new Panel { Location = new Point(0, barra.Height), AutoScroll = false };
            panel.Controls.Add(contenido);

            int margen = AnchoEscalado(MargenLateral);
            int anchoContenido = AnchoFormulario(panel, margen);
            contenido.Size = new Size(anchoContenido + margen * 2, 0);

            int y = AnchoEscalado(12);

            var lblTotal = new Label
            {
                Text = $"Total a pagar: {monto:C} USD",
                ForeColor = UiTheme.AccentPrimary,
                Font = UiTheme.FontSubtitle,
                Location = new Point(margen, y),
                AutoSize = true
            };
            contenido.Controls.Add(lblTotal);
            y += AnchoEscalado(30);

            // Tipos de método
            var tipoTarjeta = new RadioButton { Text = "Tarjeta de Crédito / Débito", ForeColor = UiTheme.TextPrimary, Location = new Point(margen, y), AutoSize = true, Checked = true };
            var tipoGift = new RadioButton { Text = "Tarjeta de Regalo / Tarjeta Steam", ForeColor = UiTheme.TextPrimary, Location = new Point(margen, y + AnchoEscalado(24)), AutoSize = true };
            var tipoPayPal = new RadioButton { Text = "PayPal / Cuenta Digital", ForeColor = UiTheme.TextPrimary, Location = new Point(margen, y + AnchoEscalado(48)), AutoSize = true };
            contenido.Controls.AddRange(new Control[] { tipoTarjeta, tipoGift, tipoPayPal });
            y += AnchoEscalado(80);

            // --- Sub-paneles de método (mismo ancho que el contenido) ---
            int anchoPanel = anchoContenido;

            var panelTarjeta = ConstruirPanelTarjeta(overlay, monto, anchoPanel);
            panelTarjeta.Location = new Point(margen, y);
            contenido.Controls.Add(panelTarjeta);

            var panelGift = ConstruirPanelGiftCard(overlay, monto, anchoPanel);
            panelGift.Location = new Point(margen, y);
            panelGift.Visible = false;
            contenido.Controls.Add(panelGift);

            var panelPayPal = ConstruirPanelPayPal(overlay, monto, anchoPanel);
            panelPayPal.Location = new Point(margen, y);
            panelPayPal.Visible = false;
            contenido.Controls.Add(panelPayPal);

            int altoSubPanel = Math.Max(panelTarjeta.Height, Math.Max(panelGift.Height, panelPayPal.Height));
            contenido.Height = y + altoSubPanel + AnchoEscalado(16);

            tipoTarjeta.CheckedChanged += (s, e) => Alternar(panelTarjeta, panelGift, panelPayPal, tipoTarjeta.Checked);
            tipoGift.CheckedChanged += (s, e) => Alternar(panelGift, panelTarjeta, panelPayPal, tipoGift.Checked);
            tipoPayPal.CheckedChanged += (s, e) => Alternar(panelPayPal, panelTarjeta, panelGift, tipoPayPal.Checked);

            // Reajusta el ancho de los sub-paneles y sus campos al redimensionar.
            panel.Resize += (s, e) => AjustarPasoMetodo(panel, contenido, margen, panelTarjeta, panelGift, panelPayPal);

            return (panel, () => { });
        }

        /// <summary>Ancho disponible para el formulario: ancho útil del panel menos los
        /// márgenes laterales y la barra de scroll vertical, con un mínimo razonable.</summary>
        private static int AnchoFormulario(Control contenedor, int margen)
        {
            int util = contenedor.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            return Math.Max(util - margen * 2, AnchoEscalado(200));
        }

        /// <summary>Reposiciona el contenido del paso de método cuando cambia el tamaño
        /// del panel (adaptación responsiva sin desbordar el ancho).</summary>
        private static void AjustarPasoMetodo(Panel panel, Panel contenido, int margen,
            Panel panelTarjeta, Panel panelGift, Panel panelPayPal)
        {
            int ancho = AnchoFormulario(panel, margen);
            contenido.Width = ancho + margen * 2;

            RedimensionarSubPanel(panelTarjeta, ancho, margen);
            RedimensionarSubPanel(panelGift, ancho, margen);
            RedimensionarSubPanel(panelPayPal, ancho, margen);
        }

        /// <summary>Ajusta el ancho del sub-panel y re-ancla sus campos al nuevo ancho.</summary>
        private static void RedimensionarSubPanel(Panel subPanel, int ancho, int margen)
        {
            subPanel.Width = ancho;
            foreach (Control c in subPanel.Controls)
            {
                if (c is TextBox txt && txt.Tag is string tag && tag == "full")
                    txt.Width = Math.Max(ancho - margen * 2, AnchoEscalado(80));
                else if (c is Button btn && btn.Tag is string tagBtn && tagBtn == "full")
                    btn.Width = Math.Max(ancho - margen * 2, AnchoEscalado(80));
                else if (c is Label lbl && lbl.Tag is string tagLbl && tagLbl == "full")
                    lbl.Width = Math.Max(ancho - margen * 2, AnchoEscalado(80));
            }
        }

        private static void Alternar(Panel mostrar, Panel ocultar1, Panel ocultar2, bool activo)
        {
            if (!activo) return;
            mostrar.Visible = true;
            ocultar1.Visible = false;
            ocultar2.Visible = false;
        }

        // ----- Sub-panel Tarjeta (con validación en tiempo real) -----
        private Panel ConstruirPanelTarjeta(Panel overlay, decimal monto, int ancho)
        {
            int margen = AnchoEscalado(MargenLateral);
            int anchoCampo = Math.Max(ancho - margen * 2, AnchoEscalado(80));
            int alto = AnchoEscalado(330);

            var panel = new Panel { Size = new Size(ancho, alto), BackColor = UiTheme.PanelDark };

            var txtNumero = CampoTexto(panel, "Número de tarjeta (16 dígitos)", margen, AnchoEscalado(14), anchoCampo, "full");
            txtNumero.MaxLength = 19; // 16 dígitos + 3 espacios de agrupación
            var txtNombre = CampoTexto(panel, "Nombre en la tarjeta", margen, AnchoEscalado(76), anchoCampo, "full");

            // Vencimiento y CVC en dos columnas (~45% cada una) con separación central.
            int espacio = AnchoEscalado(10);
            int anchoColumna = (anchoCampo - espacio) / 2;
            int xColumna2 = margen + anchoColumna + espacio;
            int yVenc = AnchoEscalado(138);

            var txtVenc = CampoTexto(panel, "Vencimiento (MM/AA)", margen, yVenc, anchoColumna, "col1");
            txtVenc.MaxLength = 5;
            var txtCvc = CampoTexto(panel, "CVC (3 dígitos)", xColumna2, yVenc, anchoColumna, "col2");
            txtCvc.MaxLength = 3;

            var lblError = new Label
            {
                Location = new Point(margen, AnchoEscalado(200)),
                Size = new Size(anchoCampo, AnchoEscalado(34)),
                ForeColor = UiTheme.AccentRed,
                Font = new Font("Segoe UI", 8 * UiTheme.EscalaUI)
            };
            panel.Controls.Add(lblError);

            // Validación en tiempo real
            void Validar()
            {
                string numero = SoloDigitos(txtNumero.Text);
                if (numero.Length > 0 && numero.Length < 16) { lblError.Text = "El número de tarjeta debe tener 16 dígitos."; return; }
                if (txtNombre.TextLength > 0 && string.IsNullOrWhiteSpace(txtNombre.Text)) { lblError.Text = "Ingresa el nombre del titular."; return; }
                if (txtVenc.TextLength == 5 && !VencimientoValido(txtVenc.Text)) { lblError.Text = "Vencimiento inválido (usa MM/AA)."; return; }
                if (txtCvc.TextLength > 0 && SoloDigitos(txtCvc.Text).Length != txtCvc.TextLength) { lblError.Text = "El CVC solo admite dígitos."; return; }
                lblError.Text = string.Empty;
            }

            txtNumero.TextChanged += (s, e) => { FormatearNumeroTarjeta(txtNumero); Validar(); };
            txtVenc.TextChanged += (s, e) => { FormatearVencimiento(txtVenc); Validar(); };
            txtNombre.TextChanged += (s, e) => Validar();
            txtCvc.TextChanged += (s, e) => Validar();

            var btnContinuar = UiTheme.CreateStyledButton("Continuar", UiTheme.AccentPrimary);
            btnContinuar.Location = new Point(margen, AnchoEscalado(244));
            btnContinuar.Width = anchoCampo;
            btnContinuar.Height = AnchoEscalado(42);
            btnContinuar.Tag = "full";
            btnContinuar.Click += (s, e) =>
            {
                string numero = SoloDigitos(txtNumero.Text);
                if (numero.Length != 16) { lblError.Text = "El número de tarjeta debe tener 16 dígitos."; return; }
                if (string.IsNullOrWhiteSpace(txtNombre.Text)) { lblError.Text = "Ingresa el nombre del titular."; return; }
                if (!VencimientoValido(txtVenc.Text)) { lblError.Text = "Ingresa un vencimiento válido (MM/AA)."; return; }
                if (SoloDigitos(txtCvc.Text).Length != 3) { lblError.Text = "El CVC debe tener 3 dígitos."; return; }

                string mascara = $"**** **** **** {numero.Substring(12)}";
                ConfirmarRecarga(overlay, monto, $"Tarjeta {mascara}", mascara);
            };
            panel.Controls.Add(btnContinuar);

            // Mantiene las dos columnas al 45% cuando cambia el ancho.
            panel.Resize += (s, e) =>
            {
                int w = Math.Max(panel.Width - margen * 2, AnchoEscalado(80));
                int col = (w - espacio) / 2;
                txtNumero.Width = w;
                txtNombre.Width = w;
                txtVenc.Width = col;
                txtCvc.Location = new Point(margen + col + espacio, txtCvc.Location.Y);
                txtCvc.Width = col;
                btnContinuar.Width = w;
                lblError.Width = w;
            };

            return panel;
        }

        // ----- Sub-panel Gift card -----
        private Panel ConstruirPanelGiftCard(Panel overlay, decimal monto, int ancho)
        {
            int margen = AnchoEscalado(MargenLateral);
            int anchoCampo = Math.Max(ancho - margen * 2, AnchoEscalado(80));
            var panel = new Panel { Size = new Size(ancho, AnchoEscalado(200)), BackColor = UiTheme.PanelDark };

            panel.Controls.Add(new Label { Text = "Código de Tarjeta Regalo / Steam", ForeColor = UiTheme.TextSecondary, Location = new Point(margen, AnchoEscalado(14)), AutoSize = true });
            var txtCodigo = CampoTexto(panel, "", margen, AnchoEscalado(36), anchoCampo, "full");
            txtCodigo.MaxLength = 24;
            txtCodigo.CharacterCasing = CharacterCasing.Upper;
            txtCodigo.PlaceholderText = "STEAM-100USD-2026";
            panel.Controls.Add(new Label
            {
                Text = "Ej. STEAM-100USD-2026, NOVA-025USD-2026, PUPU-010USD-2026.",
                ForeColor = UiTheme.TextSecondary,
                Font = new Font("Segoe UI", 7.5f * UiTheme.EscalaUI, FontStyle.Italic),
                Location = new Point(margen, AnchoEscalado(68)),
                Size = new Size(anchoCampo, AnchoEscalado(30)),
                Tag = "full"
            });

            var lblError = new Label { Location = new Point(margen, AnchoEscalado(104)), Size = new Size(anchoCampo, AnchoEscalado(30)), ForeColor = UiTheme.AccentRed, Font = new Font("Segoe UI", 8 * UiTheme.EscalaUI), Tag = "full" };
            panel.Controls.Add(lblError);

            var btnContinuar = UiTheme.CreateStyledButton("Continuar", UiTheme.AccentPrimary);
            btnContinuar.Location = new Point(margen, AnchoEscalado(140));
            btnContinuar.Width = anchoCampo;
            btnContinuar.Height = AnchoEscalado(42);
            btnContinuar.Tag = "full";
            btnContinuar.Click += (s, e) =>
            {
                string codigo = NormalizarCodigo(txtCodigo.Text);
                if (codigo.Length < 12) { lblError.Text = "El código debe tener al menos 12 caracteres."; return; }
                if (!GiftCards.TryGetValue(codigo, out decimal valorTarjeta)) { lblError.Text = "Código inválido o ya utilizado."; return; }
                ConfirmarRecarga(overlay, valorTarjeta, $"Tarjeta de Regalo {codigo}", codigo);
            };
            panel.Controls.Add(btnContinuar);

            panel.Resize += (s, e) =>
            {
                int w = Math.Max(panel.Width - margen * 2, AnchoEscalado(80));
                txtCodigo.Width = w;
                lblError.Width = w;
                btnContinuar.Width = w;
            };

            return panel;
        }

        // ----- Sub-panel PayPal (login rápido simulado) -----
        private Panel ConstruirPanelPayPal(Panel overlay, decimal monto, int ancho)
        {
            int margen = AnchoEscalado(MargenLateral);
            int anchoCampo = Math.Max(ancho - margen * 2, AnchoEscalado(80));
            var panel = new Panel { Size = new Size(ancho, AnchoEscalado(210)), BackColor = UiTheme.PanelDark };

            panel.Controls.Add(new Label { Text = "🅿️  Inicio de sesión rápido", ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontSubtitle, Location = new Point(margen, AnchoEscalado(14)), AutoSize = true });
            var txtCorreo = CampoTexto(panel, "Correo de PayPal", margen, AnchoEscalado(50), anchoCampo, "full");
            txtCorreo.Text = SessionManager.UsuarioActual?.Correo ?? "usuario@paypal.com";

            var lblEstado = new Label { Location = new Point(margen, AnchoEscalado(116)), Size = new Size(anchoCampo, AnchoEscalado(30)), ForeColor = UiTheme.TextSecondary, Font = new Font("Segoe UI", 8 * UiTheme.EscalaUI), Tag = "full" };
            panel.Controls.Add(lblEstado);

            var btnContinuar = UiTheme.CreateStyledButton("Continuar con PayPal", UiTheme.AccentPrimary);
            btnContinuar.Location = new Point(margen, AnchoEscalado(150));
            btnContinuar.Width = anchoCampo;
            btnContinuar.Height = AnchoEscalado(42);
            btnContinuar.Tag = "full";
            btnContinuar.Click += (s, e) =>
            {
                if (!txtCorreo.Text.Contains('@')) { lblEstado.ForeColor = UiTheme.AccentRed; lblEstado.Text = "Ingresa un correo válido."; return; }
                ConfirmarRecarga(overlay, monto, "PayPal", txtCorreo.Text.Trim());
            };
            panel.Controls.Add(btnContinuar);

            panel.Resize += (s, e) =>
            {
                int w = Math.Max(panel.Width - margen * 2, AnchoEscalado(80));
                txtCorreo.Width = w;
                lblEstado.Width = w;
                btnContinuar.Width = w;
            };

            return panel;
        }

        // ============================ CONFIRMACIÓN / PROCESO / ÉXITO ============================

        /// <summary>Paso 2: pantalla de confirmación con el resumen de la recarga.</summary>
        private void ConfirmarRecarga(Panel overlay, decimal monto, string metodo, string detalleMetodo)
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var barra = UiTheme.CreateBackBar("Confirmar recarga", () => CancelarOverlay(overlay));
            panel.Controls.Add(barra);

            int margen = AnchoEscalado(MargenLateral);
            int anchoCampo = AnchoFormulario(panel, margen);

            int y = barra.Height + AnchoEscalado(12);
            panel.Controls.Add(new Label { Text = "Resumen de la recarga", ForeColor = UiTheme.TextPrimary, Font = UiTheme.FontTitle, Location = new Point(margen, y), AutoSize = true });
            y += AnchoEscalado(44);

            var resumen = new Panel { Location = new Point(margen, y), Size = new Size(anchoCampo, AnchoEscalado(150)), BackColor = UiTheme.PanelDark, Tag = "full" };
            resumen.Controls.Add(Renglon("Monto a recargar", monto.ToString("C"), AnchoEscalado(16), UiTheme.TextPrimary, anchoCampo));
            resumen.Controls.Add(Renglon("Método de pago", metodo, AnchoEscalado(56), UiTheme.TextSecondary, anchoCampo));
            resumen.Controls.Add(Renglon("Total a pagar", monto.ToString("C"), AnchoEscalado(96), UiTheme.AccentPrimary, anchoCampo));
            resumen.Paint += (s, e) =>
            {
                using var pen = new Pen(UiTheme.BorderColor, 1f);
                e.Graphics.DrawLine(pen, AnchoEscalado(16), AnchoEscalado(88), resumen.Width - AnchoEscalado(16), AnchoEscalado(88));
            };
            panel.Controls.Add(resumen);
            y += AnchoEscalado(170);

            var btnConfirmar = UiTheme.CreateStyledButton("Confirmar y Recargar", UiTheme.AccentGreen);
            btnConfirmar.Location = new Point(margen, y);
            btnConfirmar.Width = anchoCampo;
            btnConfirmar.Height = AnchoEscalado(46);
            btnConfirmar.Tag = "full";
            btnConfirmar.Click += (s, e) => ProcesarRecarga(overlay, monto, metodo, detalleMetodo);
            panel.Controls.Add(btnConfirmar);
            y += AnchoEscalado(58);

            var btnCancelar = UiTheme.CreateStyledButton("Cancelar", UiTheme.PanelMid);
            btnCancelar.Location = new Point(margen, y);
            btnCancelar.Width = anchoCampo;
            btnCancelar.Height = AnchoEscalado(40);
            btnCancelar.Tag = "full";
            btnCancelar.Click += (s, e) => CancelarOverlay(overlay);
            panel.Controls.Add(btnCancelar);

            // Reajuste responsivo del resumen y sus botones.
            panel.Resize += (s, e) =>
            {
                int w = AnchoFormulario(panel, margen);
                resumen.Width = w;
                btnConfirmar.Width = w;
                btnCancelar.Width = w;
                foreach (Control c in resumen.Controls)
                    if (c is Panel fila) fila.Width = w;
            };

            MostrarPaso(overlay, panel);
        }

        /// <summary>Paso 3: indicador de "Procesando pago..." y acreditación tras un breve retardo.</summary>
        private void ProcesarRecarga(Panel overlay, decimal monto, string metodo, string detalleMetodo)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.BackgroundDark, AutoScroll = true };

            // Contenedor centrado vertical y horizontalmente mediante anclaje.
            var centro = new Panel { Width = panel.Width, Height = AnchoEscalado(110) };
            centro.Location = new Point(0, Math.Max((panel.Height - centro.Height) / 2, AnchoEscalado(120)));

            var lblProc = new Label
            {
                Text = "Procesando pago...",
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontTitle,
                Dock = DockStyle.Top,
                Height = AnchoEscalado(40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblDetalle = new Label
            {
                Text = $"{metodo}  ·  {monto:C}",
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontBody,
                Dock = DockStyle.Top,
                Height = AnchoEscalado(24),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var barra = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Width = Math.Max(centro.Width - AnchoEscalado(100), AnchoEscalado(120)),
                Height = AnchoEscalado(8)
            };
            barra.Location = new Point((centro.Width - barra.Width) / 2, AnchoEscalado(72));

            centro.Controls.Add(barra);
            centro.Controls.Add(lblDetalle);
            centro.Controls.Add(lblProc);
            panel.Controls.Add(centro);
            panel.Resize += (s, e) =>
            {
                centro.Width = panel.Width;
                barra.Width = Math.Max(centro.Width - AnchoEscalado(100), AnchoEscalado(120));
                barra.Location = new Point((centro.Width - barra.Width) / 2, barra.Location.Y);
            };

            MostrarPaso(overlay, panel);

            var timer = new System.Windows.Forms.Timer { Interval = 1400 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();

                var usuario = SessionManager.UsuarioActual;
                bool ok = usuario != null && _usuarioService.RecargarSaldo(usuario.UsuarioId, monto, metodo);
                if (!ok)
                {
                    MessageBox.Show("No se pudo procesar la recarga. Intenta de nuevo.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CancelarOverlay(overlay);
                    return;
                }

                _lblSaldo.Text = SaldoActual().ToString("C");
                CargarHistorial();
                ActualizarAlturaHistorial();

                MostrarExito(overlay, monto, metodo);
            };
            timer.Start();
        }

        /// <summary>Paso 4: pantalla de éxito; al cerrar refresca la billetera (nuevo saldo + historial).</summary>
        private void MostrarExito(Panel overlay, decimal monto, string metodo)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.BackgroundDark, AutoScroll = true };

            var centro = new Panel { Width = panel.Width, Height = AnchoEscalado(300) };
            centro.Location = new Point(0, AnchoEscalado(150));

            var lblCheck = new Label
            {
                Text = "✔",
                ForeColor = UiTheme.AccentGreen,
                Font = new Font("Segoe UI", 40 * UiTheme.EscalaUI, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = AnchoEscalado(70),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblTitulo = new Label
            {
                Text = "¡Recarga exitosa!",
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontTitle,
                Dock = DockStyle.Top,
                Height = AnchoEscalado(36),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblMensaje = new Label
            {
                Text = $"Se han añadido {monto:C} USD a tu billetera exitosamente.",
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontBody,
                Dock = DockStyle.Top,
                Height = AnchoEscalado(48),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblDetalle = new Label
            {
                Text = $"Método: {metodo}\nNuevo saldo: {SaldoActual():C}",
                ForeColor = UiTheme.TextSecondary,
                Font = new Font("Segoe UI", 8 * UiTheme.EscalaUI),
                Dock = DockStyle.Top,
                Height = AnchoEscalado(40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Se añaden en orden inverso porque Dock=Top apila de abajo hacia arriba.
            centro.Controls.Add(lblDetalle);
            centro.Controls.Add(lblMensaje);
            centro.Controls.Add(lblTitulo);
            centro.Controls.Add(lblCheck);

            int margen = AnchoEscalado(MargenLateral);
            var btnCerrar = UiTheme.CreateStyledButton("Volver a la billetera", UiTheme.AccentGreen);
            btnCerrar.Location = new Point(margen, AnchoEscalado(270));
            btnCerrar.Width = Math.Max(panel.ClientSize.Width - margen * 2 - SystemInformation.VerticalScrollBarWidth, AnchoEscalado(120));
            btnCerrar.Height = AnchoEscalado(46);
            btnCerrar.Tag = "full";
            btnCerrar.Click += (s, e) => CancelarOverlay(overlay);
            centro.Controls.Add(btnCerrar);

            panel.Controls.Add(centro);
            panel.Resize += (s, e) =>
            {
                centro.Width = panel.Width;
                btnCerrar.Width = Math.Max(panel.ClientSize.Width - margen * 2 - SystemInformation.VerticalScrollBarWidth, AnchoEscalado(120));
            };

            MostrarPaso(overlay, panel);
        }

        private void MostrarPaso(Panel overlay, Panel paso)
        {
            overlay.Controls.Clear();
            overlay.Controls.Add(paso);
            paso.Dock = DockStyle.Fill;
            overlay.AutoScrollPosition = new Point(0, 0);
        }

        private void CancelarOverlay(Panel overlay)
        {
            Controls.Remove(overlay);
            overlay.Dispose();
            _lblSaldo.Text = SaldoActual().ToString("C");
            CargarHistorial();
            ActualizarAlturaHistorial();
        }

        // ============================ HISTORIAL ============================

        private void CargarHistorial()
        {
            _panelHistorial.Controls.Clear();

            var usuario = SessionManager.UsuarioActual;
            if (usuario == null) return;

            var movimientos = _transaccionService.ObtenerMovimientosUsuario(usuario.UsuarioId);
            if (movimientos.Count == 0)
            {
                _panelHistorial.Controls.Add(new Label
                {
                    Text = "Aún no tienes movimientos en tu billetera.",
                    ForeColor = UiTheme.TextSecondary,
                    Location = new Point(0, 6),
                    AutoSize = true
                });
                return;
            }

            int y = 0;
            foreach (var mov in movimientos)
            {
                _panelHistorial.Controls.Add(CrearFilaHistorial(mov, y));
                y += 50;
            }
        }

        private Panel CrearFilaHistorial(Transaccion mov, int y)
        {
            bool esRecarga = mov.Tipo == TipoTransaccion.Recarga;

            var fila = new Panel { Location = new Point(0, y), Size = new Size(356, 46) };
            fila.Paint += (s, e) =>
            {
                using var pen = new Pen(UiTheme.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, fila.Height - 1, fila.Width, fila.Height - 1);
            };

            string descripcion = esRecarga ? $"Recarga · {mov.MetodoPago}" : $"Compra: {mov.JuegoTitulo}";
            var lblDesc = new Label
            {
                Text = descripcion,
                ForeColor = UiTheme.TextPrimary,
                Location = new Point(0, 6),
                Size = new Size(250, 18),
                AutoEllipsis = true
            };
            var lblFecha = new Label
            {
                Text = mov.Fecha.ToString("d MMM, yyyy  HH:mm"),
                ForeColor = UiTheme.TextSecondary,
                Font = new Font("Segoe UI", 8),
                Location = new Point(0, 26),
                Size = new Size(250, 15)
            };

            var lblMonto = new Label
            {
                Text = (esRecarga ? "+ " : "- ") + mov.Monto.ToString("C"),
                ForeColor = esRecarga ? UiTheme.AccentGreen : UiTheme.AccentPrimary,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(252, 10),
                Size = new Size(104, 24),
                TextAlign = ContentAlignment.TopRight
            };

            fila.Controls.AddRange(new Control[] { lblDesc, lblFecha, lblMonto });
            return fila;
        }

        private void ActualizarAlturaHistorial()
        {
            int filas = _panelHistorial.Controls.Count;
            int altoContenido = filas * 50;
            int altoDeseado = Math.Clamp(altoContenido, 60, 240);
            _panelHistorial.Height = altoDeseado;
            AjustarAltoContenido();
        }

        // ============================ VALIDACIONES ============================

        private static string SoloDigitos(string s) => new((s ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>Agrupa el número de tarjeta en bloques de 4 dígitos mientras se escribe.</summary>
        private static void FormatearNumeroTarjeta(TextBox txt)
        {
            string digitos = SoloDigitos(txt.Text);
            if (digitos.Length > 16) digitos = digitos.Substring(0, 16);

            var bloques = new List<string>();
            for (int i = 0; i < digitos.Length; i += 4)
                bloques.Add(digitos.Substring(i, Math.Min(4, digitos.Length - i)));

            string formateado = string.Join(" ", bloques);
            if (txt.Text != formateado)
            {
                txt.Text = formateado;
                txt.SelectionStart = txt.TextLength;
            }
        }

        /// <summary>Da formato MM/AA mientras se escribe en el vencimiento.</summary>
        private static void FormatearVencimiento(TextBox txt)
        {
            string digitos = SoloDigitos(txt.Text);
            if (digitos.Length > 4) digitos = digitos.Substring(0, 4);

            string formateado = digitos.Length > 2
                ? digitos.Substring(0, 2) + "/" + digitos.Substring(2)
                : digitos;

            if (txt.Text != formateado)
            {
                txt.Text = formateado;
                txt.SelectionStart = txt.TextLength;
            }
        }

        private static bool VencimientoValido(string venc)
        {
            venc = (venc ?? string.Empty).Trim();
            if (venc.Length != 5 || venc[2] != '/') return false;
            if (!int.TryParse(venc.Substring(0, 2), out int mes)) return false;
            if (!int.TryParse(venc.Substring(3, 2), out int anio)) return false;
            if (mes < 1 || mes > 12) return false;

            int anioCompleto = 2000 + anio;
            var finDeMes = new DateTime(anioCompleto, mes, 1).AddMonths(1).AddDays(-1);
            return finDeMes >= DateTime.Today;
        }

        /// <summary>Normaliza un código quitando guiones/espacios para compararlo.</summary>
        private static string NormalizarCodigo(string codigo) =>
            new((codigo ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());

        // ============================ HELPERS ============================

        private static decimal SaldoActual() => SessionManager.UsuarioActual?.Saldo ?? 0m;

        /// <summary>Etiqueta de campo + TextBox con estilo del tema. El ancho es
        /// relativo al contenedor (<paramref name="ancho"/>) y se marca con
        /// <paramref name="tag"/> para poder reajustarlo al redimensionar.</summary>
        private static TextBox CampoTexto(Panel padre, string etiqueta, int x, int y, int ancho, string tag)
        {
            if (!string.IsNullOrEmpty(etiqueta))
                padre.Controls.Add(new Label { Text = etiqueta, ForeColor = UiTheme.TextSecondary, Location = new Point(x, y), AutoSize = true });

            var txt = new TextBox
            {
                Location = new Point(x, y + AnchoEscalado(20)),
                Width = Math.Max(ancho, AnchoEscalado(40)),
                BackColor = UiTheme.PanelMid,
                ForeColor = UiTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = UiTheme.FontBody,
                Tag = tag
            };
            padre.Controls.Add(txt);
            return txt;
        }

        private static Panel Renglon(string etiqueta, string valor, int y, Color colorValor, int ancho)
        {
            int margen = AnchoEscalado(16);
            int anchoValor = Math.Max(ancho / 2, AnchoEscalado(80));
            var panel = new Panel { Location = new Point(0, y), Size = new Size(ancho, AnchoEscalado(32)), Tag = "fila" };
            panel.Controls.Add(new Label { Text = etiqueta, ForeColor = UiTheme.TextSecondary, Location = new Point(margen, AnchoEscalado(8)), AutoSize = true });
            panel.Controls.Add(new Label
            {
                Text = valor,
                ForeColor = colorValor,
                Font = UiTheme.FontBody,
                Location = new Point(ancho - margen - anchoValor, AnchoEscalado(8)),
                Size = new Size(anchoValor, AnchoEscalado(18)),
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true
            });
            return panel;
        }

        private static Label Seccion(string texto, int y) => new()
        {
            Text = texto,
            Font = UiTheme.FontSubtitle,
            ForeColor = UiTheme.TextPrimary,
            Location = new Point(14, y),
            AutoSize = true
        };

        // ============================ GIFT CARDS ============================

        // Códigos de tarjeta regalo (normalizados sin guiones) -> monto acreditado.
        private static readonly Dictionary<string, decimal> GiftCards = new(StringComparer.OrdinalIgnoreCase)
        {
            ["STEAM100USD2026"] = 100m,
            ["STEAM050USD2026"] = 50m,
            ["NOVA025USD2026"] = 25m,
            ["PUPU010USD2026"] = 10m
        };
    }
}
