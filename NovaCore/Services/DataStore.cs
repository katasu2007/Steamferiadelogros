using NovaCore.Models;

namespace NovaCore.Services
{
    /// <summary>
    /// Fuente de datos única en memoria (patrón Singleton). Sustituye a una
    /// base de datos real para el prototipo. Migrar a SQLite/SQL Server
    /// implica solo cambiar la implementación de los Services, no las Vistas.
    /// </summary>
    public class DataStore
    {
        private static DataStore? _instance;
        public static DataStore Instance => _instance ??= new DataStore();

        public List<Usuario> Usuarios { get; } = new();
        public List<Categoria> Categorias { get; } = new();
        public List<Juego> Juegos { get; } = new();
        public List<BibliotecaItem> Biblioteca { get; } = new();
        public List<Transaccion> Transacciones { get; } = new();
        public List<Reporte> Reportes { get; } = new();
        public List<SolicitudAmistad> SolicitudesAmistad { get; } = new();
        public List<ItemDeseo> Deseos { get; } = new();
        public List<Notificacion> Notificaciones { get; } = new();

        /// <summary>Carrito por usuario: clave = UsuarioId, valor = juegos pendientes de pagar.</summary>
        public Dictionary<int, List<ItemCarrito>> Carritos { get; } = new();

        /// <summary>Últimos términos de búsqueda por usuario (usados para las recomendaciones).</summary>
        public Dictionary<int, List<string>> BusquedasRecientes { get; } = new();

        private int _nextUsuarioId = 1;
        private int _nextJuegoId = 1;
        private int _nextBibliotecaId = 1;
        private int _nextTransaccionId = 1;
        private int _nextReporteId = 1;
        private int _nextSolicitudId = 1;
        private int _nextDeseoId = 1;
        private int _nextNotificacionId = 1;

        public int SiguienteUsuarioId() => _nextUsuarioId++;
        public int SiguienteJuegoId() => _nextJuegoId++;
        public int SiguienteBibliotecaId() => _nextBibliotecaId++;
        public int SiguienteTransaccionId() => _nextTransaccionId++;
        public int SiguienteReporteId() => _nextReporteId++;
        public int SiguienteSolicitudId() => _nextSolicitudId++;
        public int SiguienteDeseoId() => _nextDeseoId++;
        public int SiguienteNotificacionId() => _nextNotificacionId++;

        private DataStore()
        {
            SeedData();
            CargarUsuariosPersistidos();
            CargarBibliotecaPersistida();
            CargarSolicitudesPersistidas();
            CargarDeseosPersistidos();
            CargarNotificacionesPersistidas();
            CargarTransaccionesPersistidas();
        }

        /// <summary>
        /// Reemplaza los datos en memoria por los usuarios guardados en disco.
        /// Si el archivo no existe, se mantiene el seed (cuenta admin).
        /// </summary>
        private void CargarUsuariosPersistidos()
        {
            var guardados = PersistenciaService.CargarUsuarios();
            if (guardados == null || guardados.Count == 0) return;

            Usuarios.Clear();
            foreach (var u in guardados)
            {
                Usuarios.Add(u);
                if (u.UsuarioId >= _nextUsuarioId) _nextUsuarioId = u.UsuarioId + 1;
            }

            // Garantizar que siempre exista al menos un Administrador para poder entrar.
            if (!Usuarios.Any(u => u.Rol == RolUsuario.Administrador))
            {
                Usuarios.Add(new Usuario
                {
                    UsuarioId = SiguienteUsuarioId(),
                    NombreUsuario = "admin",
                    Correo = "admin@steamclone.com",
                    PasswordHash = "admin123",
                    Rol = RolUsuario.Administrador,
                    Saldo = 0,
                    Activo = true
                });
            }
        }

        /// <summary>
        /// Reemplaza la biblioteca en memoria por los juegos adquiridos guardados
        /// en disco. Si el archivo no existe, se parte de una biblioteca vacía.
        /// </summary>
        private void CargarBibliotecaPersistida()
        {
            var guardados = PersistenciaService.CargarBiblioteca();
            if (guardados == null || guardados.Count == 0) return;

            Biblioteca.Clear();
            foreach (var item in guardados)
            {
                if (!Juegos.Any(j => j.JuegoId == item.JuegoId)) continue;

                item.EnEjecucion = false;
                Biblioteca.Add(item);
                if (item.BibliotecaId >= _nextBibliotecaId) _nextBibliotecaId = item.BibliotecaId + 1;
            }
        }

        /// <summary>
        /// Reemplaza las solicitudes de amistad en memoria por las guardadas en
        /// disco. Se descartan las que referencian usuarios que ya no existen.
        /// </summary>
        private void CargarSolicitudesPersistidas()
        {
            var guardadas = PersistenciaService.CargarSolicitudes();
            if (guardadas == null || guardadas.Count == 0) return;

            SolicitudesAmistad.Clear();
            foreach (var s in guardadas)
            {
                if (!Usuarios.Any(u => u.UsuarioId == s.RemitenteId) ||
                    !Usuarios.Any(u => u.UsuarioId == s.DestinatarioId)) continue;

                SolicitudesAmistad.Add(s);
                if (s.SolicitudId >= _nextSolicitudId) _nextSolicitudId = s.SolicitudId + 1;
            }
        }

        /// <summary>Carga desde disco las listas de deseos, descartando entradas de
        /// usuarios o juegos que ya no existen.</summary>
        private void CargarDeseosPersistidos()
        {
            var guardados = PersistenciaService.CargarDeseos();
            if (guardados == null || guardados.Count == 0) return;

            Deseos.Clear();
            foreach (var d in guardados)
            {
                if (!Usuarios.Any(u => u.UsuarioId == d.UsuarioId)) continue;
                if (!Juegos.Any(j => j.JuegoId == d.JuegoId)) continue;

                Deseos.Add(d);
                if (d.ItemDeseoId >= _nextDeseoId) _nextDeseoId = d.ItemDeseoId + 1;
            }
        }

        /// <summary>Carga las notificaciones persistidas (solo de usuarios existentes).</summary>
        private void CargarNotificacionesPersistidas()
        {
            var guardadas = PersistenciaService.CargarNotificaciones();
            if (guardadas == null || guardadas.Count == 0) return;

            Notificaciones.Clear();
            foreach (var n in guardadas)
            {
                if (!Usuarios.Any(u => u.UsuarioId == n.UsuarioId)) continue;

                Notificaciones.Add(n);
                if (n.NotificacionId >= _nextNotificacionId) _nextNotificacionId = n.NotificacionId + 1;
            }
        }

        /// <summary>Carga las transacciones (ventas) persistidas.</summary>
        private void CargarTransaccionesPersistidas()
        {
            var guardadas = PersistenciaService.CargarTransacciones();
            if (guardadas == null || guardadas.Count == 0) return;

            Transacciones.Clear();
            foreach (var t in guardadas)
            {
                Transacciones.Add(t);
                if (t.TransaccionId >= _nextTransaccionId) _nextTransaccionId = t.TransaccionId + 1;
            }
        }

        /// <summary>
        /// Carga datos de ejemplo. Solo se crea la cuenta de Administrador;
        /// cualquier otra cuenta ("Usuario") se crea cuando alguien se registra
        /// desde la app, tal como en Steam.
        /// </summary>
        private void SeedData()
        {
            Categorias.AddRange(new[]
            {
                new Categoria { CategoriaId = 1, Nombre = "Acción" },
                new Categoria { CategoriaId = 2, Nombre = "RPG" },
                new Categoria { CategoriaId = 3, Nombre = "Estrategia" },
                new Categoria { CategoriaId = 4, Nombre = "Indie" },
                new Categoria { CategoriaId = 5, Nombre = "Aventura" },
            });

            Usuarios.Add(new Usuario
            {
                UsuarioId = SiguienteUsuarioId(),
                NombreUsuario = "admin",
                Correo = "admin@steamclone.com",
                PasswordHash = "admin123",
                Rol = RolUsuario.Administrador,
                Saldo = 0,
                Activo = true
            });

            var juegosSemilla = new[]
            {
                new Juego
                {
                    Titulo = "WARDOGS",
                    Descripcion = "WARDOGS is a TACTICAL ALL OUT WARFARE FPS where one hundred players split across three teams fight over the control zone. Set in the derelict industrial mountains of Eastern Europe on a destructible battlefield where teamwork pays and cash is king.",
                    Precio = 24.99m,
                    CategoriaId = 1,
                    Desarrollador = "BULKHEAD",
                    Publisher = "Team17",
                    Tags = "Early Access, FPS, Action, Military",
                    ReviewsResumen = "Very Positive",
                    ReviewsPorcentaje = 81,
                    ReviewsCantidad = 21415,
                    RequisitosMinimos = "Requiere procesador y sistema operativo de 64 bits\nOS: Windows 10\nPROCESADOR: Intel Core i5 8600, AMD Ryzen 5 3500\nMEMORIA: 16 GB RAM\nGRÁFICOS: Nvidia GTX 1660, Radeon RX 590\nRED: Conexión de banda ancha\nALMACENAMIENTO: 50 GB\nNOTAS: 1080p Low @ 60fps (Upscaled)",
                    RequisitosRecomendados = "Requiere procesador y sistema operativo de 64 bits\nOS: Windows 11\nPROCESADOR: Intel Core i7 12700k, AMD Ryzen 7 5700x\nMEMORIA: 16 GB RAM\nGRÁFICOS: Nvidia RTX 3070, AMD Radeon RX 6700 XT\nRED: Conexión de banda ancha\nALMACENAMIENTO: 50 GB\nNOTAS: 1440p Medium @ 70fps+ (Native) o 4K Medium @ 60fps (Upscaled)",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Wardogs contains killing with weapons and melee combat."
                },
                new Juego
                {
                    Titulo = "Voidwalker Horizons",
                    Descripcion = "Explora un universo generado proceduralmente, construye tu base, pilota tu nave y sobrevive junto a tu escuadrón en un aniversario especial lleno de nuevas expansiones.",
                    Precio = 39.99m,
                    CategoriaId = 5,
                    Desarrollador = "Hollow Sky Studios",
                    Publisher = "Hollow Sky Studios",
                    Tags = "Space, Exploration, Survival, Open World, Multiplayer",
                    ReviewsResumen = "Overwhelmingly Positive",
                    ReviewsPorcentaje = 92,
                    ReviewsCantidad = 158230,
                    RequisitosMinimos = "OS: Windows 10\nPROCESADOR: Intel i5 3570, AMD FX 6350\nMEMORIA: 8 GB RAM\nGRÁFICOS: GTX 960, Radeon R9 280\nALMACENAMIENTO: 20 GB",
                    RequisitosRecomendados = "OS: Windows 11\nPROCESADOR: Intel i7 8700, AMD Ryzen 5 3600\nMEMORIA: 16 GB RAM\nGRÁFICOS: GTX 1660, RX 590\nALMACENAMIENTO: 20 GB SSD",
                    TieneContenidoMaduro = false
                },
                new Juego
                {
                    Titulo = "Elden Shadows",
                    Descripcion = "RPG de mundo abierto ambientado en un reino sumido en la oscuridad, con combate exigente y un mundo interconectado lleno de secretos.",
                    Precio = 39.99m,
                    CategoriaId = 2,
                    Desarrollador = "Grimoire Interactive",
                    Publisher = "Grimoire Interactive",
                    Tags = "RPG, Souls-like, Dark Fantasy, Open World",
                    ReviewsResumen = "Very Positive",
                    ReviewsPorcentaje = 88,
                    ReviewsCantidad = 64210,
                    RequisitosMinimos = "OS: Windows 10\nPROCESADOR: i5 8400, Ryzen 5 1500X\nMEMORIA: 12 GB RAM\nGRÁFICOS: GTX 1060 3GB\nALMACENAMIENTO: 60 GB",
                    RequisitosRecomendados = "OS: Windows 10/11\nPROCESADOR: i7 8700K, Ryzen 5 3600X\nMEMORIA: 16 GB RAM\nGRÁFICOS: GTX 1070 8GB\nALMACENAMIENTO: 60 GB SSD",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia fantástica intensa y sangre."
                },
                new Juego
                {
                    Titulo = "Legion Tactics",
                    Descripcion = "Estrategia por turnos con batallas masivas, gestión de recursos y un árbol de tecnologías profundo.",
                    Precio = 24.99m,
                    CategoriaId = 3,
                    Desarrollador = "Ironclad Games",
                    Publisher = "Ironclad Games",
                    Tags = "Strategy, Turn-Based, War",
                    ReviewsResumen = "Positive",
                    ReviewsPorcentaje = 78,
                    ReviewsCantidad = 5321,
                    RequisitosMinimos = "OS: Windows 10\nPROCESADOR: i3 8100\nMEMORIA: 6 GB RAM\nGRÁFICOS: GTX 960\nALMACENAMIENTO: 15 GB",
                    RequisitosRecomendados = "OS: Windows 10/11\nPROCESADOR: i5 9400\nMEMORIA: 8 GB RAM\nGRÁFICOS: GTX 1050 Ti\nALMACENAMIENTO: 15 GB",
                    TieneContenidoMaduro = false
                },
                new Juego
                {
                    Titulo = "Pixel Drifters",
                    Descripcion = "Aventura indie de carreras con estética retro, pixel art hecho a mano y banda sonora synthwave original.",
                    Precio = 9.99m,
                    CategoriaId = 4,
                    Desarrollador = "Tiny Rocket",
                    Publisher = "Tiny Rocket",
                    Tags = "Indie, Retro, Racing, Pixel Graphics",
                    ReviewsResumen = "Very Positive",
                    ReviewsPorcentaje = 94,
                    ReviewsCantidad = 3012,
                    RequisitosMinimos = "OS: Windows 7+\nPROCESADOR: Cualquier dual-core\nMEMORIA: 4 GB RAM\nGRÁFICOS: Integrados\nALMACENAMIENTO: 2 GB",
                    RequisitosRecomendados = "OS: Windows 10\nPROCESADOR: i3\nMEMORIA: 4 GB RAM\nGRÁFICOS: GT 730\nALMACENAMIENTO: 2 GB",
                    TieneContenidoMaduro = false
                },
                new Juego
                {
                    Titulo = "Cyber Rift",
                    Descripcion = "Shooter táctico con hackeo en tiempo real, sigilo y misiones cooperativas en una metrópoli cyberpunk.",
                    Precio = 29.99m,
                    CategoriaId = 1,
                    Desarrollador = "Nullpoint Studios",
                    Publisher = "Nullpoint Studios",
                    Tags = "Cyberpunk, Tactical, Stealth, Co-op",
                    ReviewsResumen = "Mixed",
                    ReviewsPorcentaje = 62,
                    ReviewsCantidad = 1899,
                    RequisitosMinimos = "OS: Windows 10\nPROCESADOR: i5 7500\nMEMORIA: 8 GB RAM\nGRÁFICOS: GTX 1050\nALMACENAMIENTO: 35 GB",
                    RequisitosRecomendados = "OS: Windows 11\nPROCESADOR: i7 9700K\nMEMORIA: 16 GB RAM\nGRÁFICOS: RTX 2060\nALMACENAMIENTO: 35 GB SSD",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia con armas de fuego y lenguaje fuerte."
                },
            };

            foreach (var j in juegosSemilla)
            {
                j.JuegoId = SiguienteJuegoId();
                j.CategoriaNombre = Categorias.First(c => c.CategoriaId == j.CategoriaId).Nombre;
                Juegos.Add(j);
            }
        }
    }
}
