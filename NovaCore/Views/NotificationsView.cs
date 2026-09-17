using NovaCore.Models;
using NovaCore.Services;

namespace NovaCore.Views
{
    /// <summary>
    /// Notificaciones del usuario: avisos de solicitudes de amistad recibidas o
    /// respondidas, compras realizadas, etc. A diferencia de antes, las
    /// notificaciones se guardan en disco y NO desaparecen solas: permanecen
    /// hasta que el usuario las elimine una por una o use "Borrar todas".
    /// Las solicitudes de amistad pendientes conservan los botones Aceptar/Rechazar.
    /// </summary>
    public class NotificationsView : UserControl
    {
        private readonly MainForm _mainForm;
        private readonly NotificacionService _notificacionService = new();
        private readonly UsuarioService _usuarioService = new();
        private readonly Panel _barraSuperior;

        public NotificationsView(MainForm mainForm)
        {
            _mainForm = mainForm;
            Dock = DockStyle.Fill;
            BackColor = UiTheme.BackgroundDark;
            AutoScroll = true;

            _barraSuperior = UiTheme.CreateBackBar("Notificaciones", () => _mainForm.MostrarVista(new StoreView(_mainForm)));

            var usuario = SessionManager.UsuarioActual!;
            _notificacionService.MarcarTodasLeidas(usuario.UsuarioId);
            _mainForm.ActualizarBadgeNotificaciones();

            Reconstruir();
        }

        private void Reconstruir()
        {
            Controls.Clear();
            Controls.Add(_barraSuperior);

            var usuario = SessionManager.UsuarioActual!;
            var notificaciones = _notificacionService.ObtenerDeUsuario(usuario.UsuarioId);

            int y = 60;

            if (notificaciones.Count == 0)
            {
                Controls.Add(new Label
                {
                    Text = "No tienes notificaciones todavía.",
                    ForeColor = UiTheme.TextSecondary,
                    Location = new Point(14, y),
                    AutoSize = true
                });
                return;
            }

            var btnBorrarTodas = UiTheme.CreateStyledButton("🗑 Borrar todas", UiTheme.PanelMid, UiTheme.TextSecondary);
            btnBorrarTodas.Size = new Size(130, 30);
            btnBorrarTodas.Location = new Point(250, y - 4);
            btnBorrarTodas.Click += (s, e) =>
            {
                if (MessageBox.Show("¿Eliminar todas tus notificaciones?", "Confirmar",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

                _notificacionService.EliminarTodas(usuario.UsuarioId);
                _mainForm.ActualizarBadgeNotificaciones();
                Reconstruir();
            };
            Controls.Add(btnBorrarTodas);
            y += 38;

            foreach (var n in notificaciones)
                Controls.Add(CrearCard(n, ref y));
        }

        private Panel CrearCard(Notificacion n, ref int y)
        {
            bool esSolicitudPendiente = n.Tipo == TipoNotificacion.SolicitudAmistad &&
                                        n.SolicitudId.HasValue &&
                                        _usuarioService.ObtenerSolicitud(n.SolicitudId.Value)?.Estado == EstadoSolicitud.Pendiente;

            int alto = esSolicitudPendiente ? 142 : 96;
            var card = new Panel { Location = new Point(14, y), Size = new Size(370, alto), BackColor = UiTheme.PanelDark };

            var lblIcono = new Label { Text = Icono(n.Tipo), Font = new Font("Segoe UI", 13), Location = new Point(10, 12), AutoSize = true };
            var lblTitulo = new Label
            {
                Text = n.Titulo,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontSubtitle,
                Location = new Point(40, 10),
                AutoSize = true,
                MaximumSize = new Size(270, 0)
            };
            var lblMensaje = new Label
            {
                Text = n.Mensaje,
                ForeColor = UiTheme.TextSecondary,
                Location = new Point(40, 38),
                AutoSize = true,
                MaximumSize = new Size(300, 0)
            };
            var lblFecha = new Label
            {
                Text = n.Fecha.ToString("dd/MM/yyyy HH:mm"),
                ForeColor = UiTheme.TextSecondary,
                Font = new Font("Segoe UI", 8),
                Location = new Point(40, 70),
                AutoSize = true
            };

            var btnEliminar = new Button
            {
                Text = "🗑",
                Size = new Size(30, 30),
                Location = new Point(332, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.PanelMid,
                ForeColor = UiTheme.AccentRed,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) =>
            {
                _notificacionService.Eliminar(n.NotificacionId);
                _mainForm.ActualizarBadgeNotificaciones();
                Reconstruir();
            };

            card.Controls.AddRange(new Control[] { lblIcono, lblTitulo, lblMensaje, lblFecha, btnEliminar });

            if (esSolicitudPendiente)
            {
                var solicitudId = n.SolicitudId!.Value;

                var btnAceptar = UiTheme.CreateStyledButton("✓ Aceptar", UiTheme.AccentGreen);
                btnAceptar.Size = new Size(154, 30);
                btnAceptar.Location = new Point(40, 100);
                btnAceptar.Click += (s, e) => ResponderSolicitud(solicitudId, aceptar: true);

                var btnRechazar = UiTheme.CreateStyledButton("✕ Rechazar", UiTheme.AccentRed);
                btnRechazar.Size = new Size(154, 30);
                btnRechazar.Location = new Point(202, 100);
                btnRechazar.Click += (s, e) => ResponderSolicitud(solicitudId, aceptar: false);

                card.Controls.Add(btnAceptar);
                card.Controls.Add(btnRechazar);
            }

            y += alto + 10;
            return card;
        }

        private void ResponderSolicitud(int solicitudId, bool aceptar)
        {
            var (exito, mensaje) = aceptar
                ? _usuarioService.AceptarSolicitud(solicitudId)
                : _usuarioService.RechazarSolicitud(solicitudId);

            MessageBox.Show(mensaje, aceptar ? "Solicitud aceptada" : "Solicitud rechazada",
                MessageBoxButtons.OK, exito ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            _mainForm.ActualizarBadgeNotificaciones();
            Reconstruir();
        }

        private static string Icono(TipoNotificacion tipo) => tipo switch
        {
            TipoNotificacion.SolicitudAmistad => "👤",
            TipoNotificacion.SolicitudAceptada => "✅",
            TipoNotificacion.SolicitudRechazada => "🚫",
            TipoNotificacion.Compra => "🛒",
            _ => "🔔"
        };
    }
}
