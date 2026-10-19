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
            CargarJuegosPersistidos();
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
                    PasswordHash = PasswordHasher.Hash("admin123"),
                    Rol = RolUsuario.Administrador,
                    Saldo = 0,
                    Activo = true
                });
            }
        }

        /// <summary>Versión del catálogo real sembrado. Súbela al reemplazar los juegos
        /// para forzar que las instalaciones existentes se actualicen al nuevo catálogo.</summary>
        private const int VersionCatalogoActual = 2;

        /// <summary>
        /// Reemplaza el catálogo en memoria por los juegos guardados en disco
        /// (altas y ediciones hechas por el administrador), conservando sus
        /// portadas. Si el archivo no existe, se mantiene el catálogo semilla.
        /// Si la versión sembrada es anterior a <see cref="VersionCatalogoActual"/>,
        /// se descarta el catálogo antiguo (juegos ficticios) y se regenera con
        /// la semilla real actual.
        /// Debe ejecutarse antes de cargar la biblioteca, que valida contra él.
        /// </summary>
        private void CargarJuegosPersistidos()
        {
            if (PersistenciaService.CargarVersionCatalogo() >= VersionCatalogoActual)
            {
                var guardados = PersistenciaService.CargarJuegos();
                if (guardados != null && guardados.Count > 0)
                {
                    Juegos.Clear();
                    foreach (var j in guardados)
                    {
                        j.CategoriaNombre = Categorias.FirstOrDefault(c => c.CategoriaId == j.CategoriaId)?.Nombre
                                            ?? j.CategoriaNombre;
                        Juegos.Add(j);
                        if (j.JuegoId >= _nextJuegoId) _nextJuegoId = j.JuegoId + 1;
                    }
                    return;
                }
            }

            // Migración: el catálogo guardado es anterior al actual (o no existía).
            // Se limpia la biblioteca de títulos obsoletos y se persiste la nueva
            // semilla para que no queden juegos ficticios de por medio.
            Biblioteca.Clear();
            PersistenciaService.GuardarBiblioteca(Biblioteca);
            PersistenciaService.GuardarJuegos(Juegos);
            PersistenciaService.GuardarVersionCatalogo(VersionCatalogoActual);
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
                PasswordHash = PasswordHasher.Hash("admin123"),
                Rol = RolUsuario.Administrador,
                Saldo = 0,
                Activo = true
            });

            // =================================================================
            // Catálogo inicial: 25 videojuegos REALES (5 por cada categoría).
            // Los datos (sinopsis, precio, reviews y requisitos de PC) son los
            // publicados oficialmente en Steam y pueden variar con el tiempo;
            // el administrador puede editarlos desde la app.
            // =================================================================
            var juegosSemilla = new[]
            {
                // ---------------------------- ACCIÓN ----------------------------
                new Juego
                {
                    Titulo = "Cyberpunk 2077",
                    Descripcion = "Cyberpunk 2077 es un RPG de acción y mundo abierto ambientado en Night City, una megalópolis obsesionada con el poder, el glamour y las modificaciones corporales. Juegas como V, un mercenario que persigue un implante único que es la clave de la inmortalidad.",
                    Precio = 59.99m,
                    CategoriaId = 1,
                    Desarrollador = "CD PROJEKT RED",
                    Publisher = "CD PROJEKT RED",
                    Tags = "RPG, Open World, Cyberpunk, Action, First-Person",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 86,
                    ReviewsCantidad = 750000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Core i7-6700 o Ryzen 5 1600\nMEMORIA: 12 GB de RAM\nGRÁFICOS: GeForce GTX 1060 6GB o Radeon RX 580 8GB\nALMACENAMIENTO: 70 GB SSD\nDirectX: Versión 12",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Core i7-12700 o Ryzen 7 7800X3D\nMEMORIA: 16 GB de RAM\nGRÁFICOS: GeForce RTX 2060 SUPER o Radeon RX 5700 XT\nALMACENAMIENTO: 70 GB SSD\nDirectX: Versión 12",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, sangre, lenguaje fuerte, contenido sexual y uso de drogas (ESRB: M).",
                    FechaPublicacion = new DateTime(2020, 12, 10)
                },
                new Juego
                {
                    Titulo = "Resident Evil 4",
                    Descripcion = "Resident Evil 4 es la remake del clásico de survival horror. Leon S. Kennedy, seis años después del desastre de Raccoon City, es enviado a rescatar a la hija secuestrada del presidente, pero descubre algo siniestro en una remota aldea europea.",
                    Precio = 59.99m,
                    CategoriaId = 1,
                    Desarrollador = "CAPCOM Co., Ltd.",
                    Publisher = "CAPCOM Co., Ltd.",
                    Tags = "Survival Horror, Action, Third-Person Shooter, Singleplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 97,
                    ReviewsCantidad = 210000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: AMD Ryzen 3 1200 o Intel Core i5-7500\nMEMORIA: 8 GB de RAM\nGRÁFICOS: AMD Radeon RX 560 (4 GB) o NVIDIA GeForce GTX 1050 Ti (4 GB)\nALMACENAMIENTO: 60 GB\nDirectX: Versión 12",
                    RequisitosRecomendados = "OS: Windows 10/11 de 64 bits\nPROCESADOR: AMD Ryzen 5 3600 o Intel Core i7 8700\nMEMORIA: 16 GB de RAM\nGRÁFICOS: AMD Radeon RX 5700 o NVIDIA GeForce GTX 1070\nALMACENAMIENTO: 60 GB SSD\nDirectX: Versión 12",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, gore, sangre y lenguaje fuerte (ESRB: M).",
                    FechaPublicacion = new DateTime(2023, 3, 24)
                },
                new Juego
                {
                    Titulo = "DOOM Eternal",
                    Descripcion = "DOOM Eternal es la secuela de DOOM (2016). Eres el DOOM Slayer en una cruzada contra los demonios del Infierno que han invadido la Tierra, con un arsenal ampliado, movilidad extrema y combates frenéticos a 60 fps.",
                    Precio = 39.99m,
                    CategoriaId = 1,
                    Desarrollador = "id Software",
                    Publisher = "Bethesda Softworks",
                    Tags = "Action, FPS, Demons, Gore, Fast-Paced",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 92,
                    ReviewsCantidad = 195000,
                    RequisitosMinimos = "OS: Windows 7/10 de 64 bits\nPROCESADOR: Intel Core i5 de 3.3 GHz o AMD Ryzen 3 1200\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 1050 Ti o AMD Radeon R9 280\nALMACENAMIENTO: 80 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i7-6700K o AMD Ryzen 7 1800X\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 1080 o AMD Radeon RX Vega 56\nALMACENAMIENTO: 80 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, gore y lenguaje fuerte (ESRB: M).",
                    FechaPublicacion = new DateTime(2020, 3, 20)
                },
                new Juego
                {
                    Titulo = "Grand Theft Auto V",
                    Descripcion = "Grand Theft Auto V es un juego de acción y mundo abierto ambientado en Los Santos. Alterna entre tres criminales -Franklin, Michael y Trevor- para llevar a cabo atracos mientras exploras la ciudad y sus alrededores en el modo historia o en GTA Online.",
                    Precio = 29.99m,
                    CategoriaId = 1,
                    Desarrollador = "Rockstar North",
                    Publisher = "Rockstar Games",
                    Tags = "Open World, Action, Multiplayer, Crime, Third-Person Shooter",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 88,
                    ReviewsCantidad = 1500000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5 3470 o AMD X8 FX-8350\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GTX 660 2GB o AMD HD 7870 2GB\nALMACENAMIENTO: 100 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 10/11 de 64 bits\nPROCESADOR: Intel Core i5 6600K o AMD FX-9590\nMEMORIA: 16 GB de RAM\nGRÁFICOS: NVIDIA GTX 970 o AMD R9 390\nALMACENAMIENTO: 100 GB SSD\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, sangre, lenguaje fuerte, contenido sexual y uso de drogas (ESRB: M).",
                    FechaPublicacion = new DateTime(2015, 4, 14)
                },
                new Juego
                {
                    Titulo = "Devil May Cry 5",
                    Descripcion = "Devil May Cry 5 es la nueva entrega de la saga de acción de estilo. Los demonios han tomado la ciudad y depende de Nero, Dante y V enfrentar al Rey Demonio Urizen en combates de estilo libre con combos espectaculares y una banda sonora dinámica.",
                    Precio = 29.99m,
                    CategoriaId = 1,
                    Desarrollador = "CAPCOM Co., Ltd.",
                    Publisher = "CAPCOM Co., Ltd.",
                    Tags = "Action, Hack and Slash, Demons, Stylish, Singleplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 95,
                    ReviewsCantidad = 120000,
                    RequisitosMinimos = "OS: Windows 7/8.1/10 de 64 bits\nPROCESADOR: Intel Core i5-4460 o AMD FX-6300\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 760 o AMD Radeon R7 260X\nALMACENAMIENTO: 35 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 7/8.1/10 de 64 bits\nPROCESADOR: Intel Core i7-3770 o AMD FX-9590\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 1060 o AMD Radeon RX 480\nALMACENAMIENTO: 35 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia frecuente, sangre y lenguaje fuerte (ESRB: M).",
                    FechaPublicacion = new DateTime(2019, 3, 8)
                },

                // ------------------------------ RPG ------------------------------
                new Juego
                {
                    Titulo = "Elden Ring",
                    Descripcion = "Elden Ring es un RPG de acción de mundo abierto creado por FromSoftware y George R. R. Martin. El Anillo Ancestral se ha roto y las Tierras Intermedias están en ruinas; levántate como Sinluz y conviértete en Señor del Círculo.",
                    Precio = 59.99m,
                    CategoriaId = 2,
                    Desarrollador = "FromSoftware Inc.",
                    Publisher = "FromSoftware Inc.",
                    Tags = "Souls-like, Open World, Dark Fantasy, RPG, Difficult",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 94,
                    ReviewsCantidad = 750000,
                    RequisitosMinimos = "OS: Windows 10\nPROCESADOR: INTEL CORE I5-8400 o AMD RYZEN 3 3300X\nMEMORIA: 12 GB de RAM\nGRÁFICOS: NVIDIA GEFORCE GTX 1060 3 GB o AMD RADEON RX 580 4 GB\nALMACENAMIENTO: 60 GB\nDirectX: Versión 12",
                    RequisitosRecomendados = "OS: Windows 11\nPROCESADOR: INTEL CORE I7-8700K o AMD RYZEN 5 3600X\nMEMORIA: 16 GB de RAM\nGRÁFICOS: NVIDIA GEFORCE GTX 1070 8 GB o AMD RADEON RX VEGA 56 8 GB\nALMACENAMIENTO: 60 GB SSD\nDirectX: Versión 12",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia fantástica y lenguaje suave (ESRB: M).",
                    FechaPublicacion = new DateTime(2022, 2, 25)
                },
                new Juego
                {
                    Titulo = "The Witcher 3: Wild Hunt",
                    Descripcion = "The Witcher 3: Wild Hunt es un RPG de mundo abierto donde encarnas a Geralt de Rivia, un cazador de monstruos que busca a Ciri, una joven con un poder capaz de destruir el mundo, mientras la Cacería Salvaje acecha.",
                    Precio = 39.99m,
                    CategoriaId = 2,
                    Desarrollador = "CD PROJEKT RED",
                    Publisher = "CD PROJEKT RED",
                    Tags = "RPG, Open World, Fantasy, Story Rich, Mature",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 96,
                    ReviewsCantidad = 780000,
                    RequisitosMinimos = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core i5-2500K o AMD Phenom II X4 940\nMEMORIA: 6 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 660 o AMD Radeon HD 7870\nALMACENAMIENTO: 50 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core i7-3770 o AMD FX-8350\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 770 o AMD Radeon R9 290\nALMACENAMIENTO: 50 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, sangre, desnudos y contenido sexual (ESRB: M).",
                    FechaPublicacion = new DateTime(2015, 5, 18)
                },
                new Juego
                {
                    Titulo = "Baldur's Gate 3",
                    Descripcion = "Baldur's Gate 3 es un RPG por turnos basado en Dungeons & Dragons. Reúne a tu grupo de aventureros, domina la estrategia en combates tácticos y regresa a los Reinos Olvidados en una historia de compañerismo, traición y sacrificio.",
                    Precio = 59.99m,
                    CategoriaId = 2,
                    Desarrollador = "Larian Studios",
                    Publisher = "Larian Studios",
                    Tags = "RPG, Turn-Based, Story Rich, Character Customization, Co-op",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 96,
                    ReviewsCantidad = 700000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel I5 4690 o AMD FX 8350\nMEMORIA: 8 GB de RAM\nGRÁFICOS: Nvidia GTX 970 o AMD RX 480 4GB\nALMACENAMIENTO: 150 GB SSD\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel i7 8700K o AMD R5 3600\nMEMORIA: 16 GB de RAM\nGRÁFICOS: Nvidia 2060 Super o AMD RX 5700 XT\nALMACENAMIENTO: 150 GB SSD\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia, sangre, desnudos, contenido sexual y lenguaje fuerte (ESRB: M).",
                    FechaPublicacion = new DateTime(2023, 8, 3)
                },
                new Juego
                {
                    Titulo = "Persona 5 Royal",
                    Descripcion = "Persona 5 Royal es un RPG por turnos donde un grupo de estudiantes forma a los Ladrones Fantasma de Corazones para reformar la sociedad robando los corazones corruptos de los adultos. Incluye nuevas historias, personajes y contenido.",
                    Precio = 59.99m,
                    CategoriaId = 2,
                    Desarrollador = "ATLUS",
                    Publisher = "SEGA",
                    Tags = "JRPG, Turn-Based, Anime, Story Rich, Singleplayer",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 96,
                    ReviewsCantidad = 90000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i7-4790 o AMD Ryzen 5 1500X\nMEMORIA: 8 GB de RAM\nGRÁFICOS: Nvidia GeForce GTX 650 Ti o AMD Radeon HD 7870\nALMACENAMIENTO: 40 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i7-4790 o AMD Ryzen 5 1500X\nMEMORIA: 8 GB de RAM\nGRÁFICOS: Nvidia GeForce GTX 760 o AMD Radeon R7 260X\nALMACENAMIENTO: 40 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia de fantasía, temas maduros, lenguaje fuerte y contenido sexual sugerido (ESRB: M).",
                    FechaPublicacion = new DateTime(2022, 10, 21)
                },
                new Juego
                {
                    Titulo = "Monster Hunter: World",
                    Descripcion = "Monster Hunter: World es un RPG de acción donde cazas enormes monstruos en ecosistemas vivos. Forja armaduras y armas con los materiales que obtienes, y únete con hasta tres jugadores en línea para derribar a las bestias más feroces.",
                    Precio = 29.99m,
                    CategoriaId = 2,
                    Desarrollador = "CAPCOM Co., Ltd.",
                    Publisher = "CAPCOM Co., Ltd.",
                    Tags = "Action, Co-op, Multiplayer, RPG, Hunting",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 88,
                    ReviewsCantidad = 380000,
                    RequisitosMinimos = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core i5-4460 3.2GHz o AMD FX-6300\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 760 o AMD Radeon R7 260x\nALMACENAMIENTO: 50 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core i7 3770 3.4GHz o AMD FX-8350\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 1060 o AMD Radeon RX 570X\nALMACENAMIENTO: 50 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia de fantasía y sangre moderada (ESRB: T/PEGI 12).",
                    FechaPublicacion = new DateTime(2018, 8, 9)
                },

                // --------------------------- ESTRATEGIA ---------------------------
                new Juego
                {
                    Titulo = "Age of Empires IV",
                    Descripcion = "Age of Empires IV es un juego de estrategia en tiempo real que pone la historia a tu alcance. Elige entre ocho civilizaciones, construye ciudades, gestiona recursos y comanda ejércitos en campañas históricas y multijugador.",
                    Precio = 39.99m,
                    CategoriaId = 3,
                    Desarrollador = "Relic Entertainment",
                    Publisher = "Xbox Game Studios",
                    Tags = "Strategy, RTS, Historical, Base Building, Multiplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 88,
                    ReviewsCantidad = 65000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5-6300U o AMD Ryzen 5 2400G\nMEMORIA: 8 GB de RAM\nGRÁFICOS: Intel HD 520 o AMD Radeon RX Vega 11\nALMACENAMIENTO: 50 GB\nDirectX: Versión 12",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5-8400 o AMD Ryzen 5 2600\nMEMORIA: 16 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 970 o AMD Radeon RX 570\nALMACENAMIENTO: 50 GB\nDirectX: Versión 12",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "",
                    FechaPublicacion = new DateTime(2021, 10, 28)
                },
                new Juego
                {
                    Titulo = "Sid Meier's Civilization VI",
                    Descripcion = "Civilization VI amplía el concepto de construir un imperio que resista el paso del tiempo. Expande tu ciudad casilla por casilla, desarrolla la cultura y la tecnología, y compite por la victoria por medio de la diplomacia, la ciencia o la conquista.",
                    Precio = 59.99m,
                    CategoriaId = 3,
                    Desarrollador = "Firaxis Games",
                    Publisher = "2K",
                    Tags = "Strategy, Turn-Based, 4X, Historical, Multiplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 82,
                    ReviewsCantidad = 250000,
                    RequisitosMinimos = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core i3 2.5 GHz o AMD Phenom II 2.6 GHz\nMEMORIA: 4 GB de RAM\nGRÁFICOS: 1 GB DirectX 11\nALMACENAMIENTO: 12 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: 4ta generación Intel Core i5 2.5GHz+ o AMD FX8350 4.0 GHz+\nMEMORIA: 8 GB de RAM\nGRÁFICOS: 2 GB DirectX 11\nALMACENAMIENTO: 12 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "",
                    FechaPublicacion = new DateTime(2016, 10, 21)
                },
                new Juego
                {
                    Titulo = "Hearts of Iron IV",
                    Descripcion = "Hearts of Iron IV es un juego de gran estrategia de la Segunda Guerra Mundial. Toma el control de cualquier nación, gestiona su economía, diplomacia y ejército, y reescribe la historia en uno de los conflictos más grandes de la humanidad.",
                    Precio = 39.99m,
                    CategoriaId = 3,
                    Desarrollador = "Paradox Development Studio",
                    Publisher = "Paradox Interactive",
                    Tags = "Strategy, Grand Strategy, World War II, Historical, Simulation",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 87,
                    ReviewsCantidad = 175000,
                    RequisitosMinimos = "OS: Windows 7 de 64 bits\nPROCESADOR: Intel Core 2 Quad Q9400 2.66 GHz o AMD Athlon II X4 650 3.20 GHz\nMEMORIA: 4 GB de RAM\nGRÁFICOS: ATI Radeon HD 5850 o NVIDIA GeForce GTX 470\nALMACENAMIENTO: 11 GB\nDirectX: Versión 9.0c",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5 750 2.66 GHz o AMD Phenom II X4 955 3.20 GHz\nMEMORIA: 4 GB de RAM\nGRÁFICOS: ATI Radeon HD 6950 o NVIDIA GeForce GTX 570\nALMACENAMIENTO: 11 GB\nDirectX: Versión 9.0c",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "",
                    FechaPublicacion = new DateTime(2016, 6, 6)
                },
                new Juego
                {
                    Titulo = "StarCraft II",
                    Descripcion = "StarCraft II es el RTS de ciencia ficción por excelencia. Comanda a los Terran, Protoss o Zerg en campañas épicas y en un multijugador competitivo que definió los esports, con un equilibrio de razas y estrategias siempre en evolución.",
                    Precio = 0m,
                    CategoriaId = 3,
                    Desarrollador = "Blizzard Entertainment",
                    Publisher = "Blizzard Entertainment",
                    Tags = "Strategy, RTS, Sci-fi, Competitive, Multiplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 92,
                    ReviewsCantidad = 95000,
                    RequisitosMinimos = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core 2 Duo 2.6 GHz o AMD Athlon 64 X2 5600+\nMEMORIA: 2 GB de RAM\nGRÁFICOS: NVIDIA GeForce 7600 GT o ATI Radeon HD 2600 XT\nALMACENAMIENTO: 30 GB\nDirectX: Versión 9.0c",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5 o AMD FX Series\nMEMORIA: 4 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 650 o ATI Radeon HD 7790\nALMACENAMIENTO: 30 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia de ciencia ficción moderada y temas bélicos (ESRB: T).",
                    FechaPublicacion = new DateTime(2010, 7, 27)
                },
                new Juego
                {
                    Titulo = "Cities: Skylines",
                    Descripcion = "Cities: Skylines es un simulador de construcción de ciudades que reimagina la experiencia clásica. Diseña tu ciudad, administra el tráfico, los servicios públicos y las políticas, y hazla crecer desde un pequeño asentamiento hasta una gran metrópolis.",
                    Precio = 29.99m,
                    CategoriaId = 3,
                    Desarrollador = "Colossal Order Ltd.",
                    Publisher = "Paradox Interactive",
                    Tags = "Simulation, City Builder, Management, Sandbox, Moddable",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 90,
                    ReviewsCantidad = 170000,
                    RequisitosMinimos = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core 2 Duo 3.0 GHz o AMD Athlon 64 X2 6400+\nMEMORIA: 4 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 260 o ATI Radeon HD 5670\nALMACENAMIENTO: 4 GB\nDirectX: Versión 9.0c",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5-3470 o AMD FX-6300\nMEMORIA: 6 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 660 o AMD Radeon HD 7870\nALMACENAMIENTO: 4 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "",
                    FechaPublicacion = new DateTime(2015, 3, 10)
                },

                // ------------------------------ INDIE ------------------------------
                new Juego
                {
                    Titulo = "Hollow Knight",
                    Descripcion = "Hollow Knight es un metroidvania de acción y aventura ambientado en el reino subterráneo de Hallownest. Explora cavernas interconectadas, enfrenta criaturas corrompidas y descubre los misterios de un mundo dibujado a mano con un estilo inconfundible.",
                    Precio = 14.99m,
                    CategoriaId = 4,
                    Desarrollador = "Team Cherry",
                    Publisher = "Team Cherry",
                    Tags = "Metroidvania, Platformer, Difficult, Atmospheric, Indie",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 98,
                    ReviewsCantidad = 145000,
                    RequisitosMinimos = "OS: Windows 7\nPROCESADOR: Intel Core 2 Duo E5200\nMEMORIA: 4 GB de RAM\nGRÁFICOS: GeForce 9800GTX+ (1GB)\nALMACENAMIENTO: 9 GB\nDirectX: Versión 10",
                    RequisitosRecomendados = "OS: Windows 10\nPROCESADOR: Intel Core i5\nMEMORIA: 8 GB de RAM\nGRÁFICOS: GeForce GTX 560\nALMACENAMIENTO: 9 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "Violencia de fantasía leve (ESRB: E10+).",
                    FechaPublicacion = new DateTime(2017, 2, 24)
                },
                new Juego
                {
                    Titulo = "Celeste",
                    Descripcion = "Celeste es un juego de plataformas de precisión sobre superar obstáculos y dificultades. Ayuda a Madeline a escalar la montaña Celeste, descubre su verdadero yo y enfrenta cientos de desafíos diseñados a mano con controles precisos y una banda sonora memorable.",
                    Precio = 19.99m,
                    CategoriaId = 4,
                    Desarrollador = "Maddy Makes Games",
                    Publisher = "Maddy Makes Games",
                    Tags = "Platformer, Difficult, Pixel Graphics, Indie, Story Rich",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 98,
                    ReviewsCantidad = 90000,
                    RequisitosMinimos = "OS: Windows 7/8/10\nPROCESADOR: Intel Core i3 o superior\nMEMORIA: 2 GB de RAM\nGRÁFICOS: OpenGL compatible con 512 MB\nALMACENAMIENTO: 1 GB\nDirectX: Versión 9.0c",
                    RequisitosRecomendados = "OS: Windows 10\nPROCESADOR: Intel Core i5\nMEMORIA: 4 GB de RAM\nGRÁFICOS: OpenGL compatible con 1 GB\nALMACENAMIENTO: 1 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "Temas de ansiedad y depresión; lenguaje leve (ESRB: E10+).",
                    FechaPublicacion = new DateTime(2018, 1, 25)
                },
                new Juego
                {
                    Titulo = "Hades",
                    Descripcion = "Hades es un roguelike de acción donde encarnas a Zagreus, príncipe del Inframundo, mientras lucha por escapar del reino de su padre Hades. Cada huida exitosa y cada muerte te acerca a la verdad y desbloquea nuevas habilidades narrativas.",
                    Precio = 24.99m,
                    CategoriaId = 4,
                    Desarrollador = "Supergiant Games",
                    Publisher = "Supergiant Games",
                    Tags = "Roguelike, Action, Hack and Slash, Greek Mythology, Indie",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 98,
                    ReviewsCantidad = 175000,
                    RequisitosMinimos = "OS: Windows 7/8/10\nPROCESADOR: 1.4GHz de doble núcleo\nMEMORIA: 4 GB de RAM\nGRÁFICOS: DirectX 10+\nALMACENAMIENTO: 15 GB\nDirectX: Versión 10",
                    RequisitosRecomendados = "OS: Windows 10\nPROCESADOR: 3.0GHz de cuatro núcleos\nMEMORIA: 8 GB de RAM\nGRÁFICOS: DirectX 11 compatible con 2GB\nALMACENAMIENTO: 15 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia de fantasía, temas mitológicos y lenguaje leve (ESRB: T).",
                    FechaPublicacion = new DateTime(2020, 9, 17)
                },
                new Juego
                {
                    Titulo = "Stardew Valley",
                    Descripcion = "Stardew Valley es un RPG de simulación agrícola. Hereda la vieja granja de tu abuelo y emprende una nueva vida: cultiva, cría animales, explora minas, pesca, conoce a los habitantes del pueblo y forma una familia.",
                    Precio = 14.99m,
                    CategoriaId = 4,
                    Desarrollador = "ConcernedApe",
                    Publisher = "ConcernedApe",
                    Tags = "Farming Sim, Life Sim, Pixel Graphics, RPG, Relaxing",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 98,
                    ReviewsCantidad = 520000,
                    RequisitosMinimos = "OS: Windows Vista o superior\nPROCESADOR: 2 GHz\nMEMORIA: 2 GB de RAM\nGRÁFICOS: Tarjeta con 256 MB de memoria de video\nALMACENAMIENTO: 1 GB\nDirectX: Versión 10",
                    RequisitosRecomendados = "OS: Windows 10\nPROCESADOR: 2 GHz o superior\nMEMORIA: 4 GB de RAM\nGRÁFICOS: Tarjeta con 512 MB de memoria de video\nALMACENAMIENTO: 1 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "Referencias al alcohol y temas emocionales leves (ESRB: E10+).",
                    FechaPublicacion = new DateTime(2016, 2, 26)
                },
                new Juego
                {
                    Titulo = "Undertale",
                    Descripcion = "Undertale es un RPG por turnos donde controlas a un niño humano que cae en un mundo subterráneo habitado por monstruos. Puedes elegir matar o perdonar a cada enemigo, y cada decisión altera profundamente la historia y su final.",
                    Precio = 9.99m,
                    CategoriaId = 4,
                    Desarrollador = "tobyfox",
                    Publisher = "tobyfox",
                    Tags = "RPG, Story Rich, Pixel Graphics, Multiple Endings, Indie",
                    ReviewsResumen = "Extremadamente positivas",
                    ReviewsPorcentaje = 97,
                    ReviewsCantidad = 210000,
                    RequisitosMinimos = "OS: Windows Vista o superior\nPROCESADOR: 2 GHz de doble núcleo\nMEMORIA: 2 GB de RAM\nGRÁFICOS: Tarjeta de video con 128 MB\nALMACENAMIENTO: 400 MB\nDirectX: Versión 9.0",
                    RequisitosRecomendados = "OS: Windows 10\nPROCESADOR: 2 GHz de doble núcleo o superior\nMEMORIA: 4 GB de RAM\nGRÁFICOS: Tarjeta de video con 256 MB\nALMACENAMIENTO: 400 MB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia de fantasía, temas maduros y lenguaje leve (ESRB: T).",
                    FechaPublicacion = new DateTime(2015, 9, 15)
                },

                // ---------------------------- AVENTURA ----------------------------
                new Juego
                {
                    Titulo = "Red Dead Redemption 2",
                    Descripcion = "Red Dead Redemption 2 es una épica del Lejano Oeste. Tras un atraco fallido, Arthur Morgan y la banda de Van der Linde deben huir a través de Estados Unidos, en una historia sobre la lealtad, la redención y el fin de la era del salvaje oeste.",
                    Precio = 59.99m,
                    CategoriaId = 5,
                    Desarrollador = "Rockstar Games",
                    Publisher = "Rockstar Games",
                    Tags = "Open World, Story Rich, Action, Western, Adventure",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 90,
                    ReviewsCantidad = 650000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5-2500K o AMD FX-6300\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 770 2GB o AMD Radeon R9 280 3GB\nALMACENAMIENTO: 150 GB\nDirectX: Versión 12",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i7-4770K o AMD Ryzen 5 1500X\nMEMORIA: 12 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 1060 6GB o AMD Radeon RX 480 4GB\nALMACENAMIENTO: 150 GB SSD\nDirectX: Versión 12",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, sangre, lenguaje fuerte, desnudos y contenido sexual (ESRB: M).",
                    FechaPublicacion = new DateTime(2019, 12, 5)
                },
                new Juego
                {
                    Titulo = "God of War",
                    Descripcion = "God of War acompaña a Kratos, ahora padre, y a su hijo Atreus en un viaje por los reinos nórdicos. Deberán cumplir el último deseo de la madre de Atreus, enfrentando dioses y criaturas en un relato íntimo sobre paternidad y redención.",
                    Precio = 49.99m,
                    CategoriaId = 5,
                    Desarrollador = "Santa Monica Studio",
                    Publisher = "PlayStation Publishing LLC",
                    Tags = "Action, Adventure, Mythology, Story Rich, Singleplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 95,
                    ReviewsCantidad = 130000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel i5-2500k o AMD Ryzen 3 1200\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GTX 960 4GB o AMD RX 470 4GB\nALMACENAMIENTO: 70 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel i5-6600k o AMD Ryzen 5 2400G\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GTX 1060 6GB o AMD RX 570 8GB\nALMACENAMIENTO: 70 GB SSD\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, sangre y lenguaje fuerte (ESRB: M).",
                    FechaPublicacion = new DateTime(2022, 1, 14)
                },
                new Juego
                {
                    Titulo = "Rise of the Tomb Raider",
                    Descripcion = "Rise of the Tomb Raider continúa la historia de Lara Croft en su búsqueda del secreto de la inmortalidad. Viaja a Siberia, explora tumbas ancestrales y sobrevive a una organización enemiga en una aventura de acción y exploración.",
                    Precio = 29.99m,
                    CategoriaId = 5,
                    Desarrollador = "Crystal Dynamics",
                    Publisher = "Square Enix",
                    Tags = "Action, Adventure, Exploration, Female Protagonist, Singleplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 92,
                    ReviewsCantidad = 78000,
                    RequisitosMinimos = "OS: Windows 7/8/10 de 64 bits\nPROCESADOR: Intel Core i3-2100 o AMD Athlon II X2 260\nMEMORIA: 6 GB de RAM\nGRÁFICOS: NVIDIA GTX 650 o AMD Radeon HD 7770\nALMACENAMIENTO: 25 GB\nDirectX: Versión 11",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i7-3770 o AMD FX-8350\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GTX 970 o AMD Radeon R9 290X\nALMACENAMIENTO: 25 GB\nDirectX: Versión 11",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia, sangre y temas maduros (ESRB: M).",
                    FechaPublicacion = new DateTime(2016, 1, 28)
                },
                new Juego
                {
                    Titulo = "Stray",
                    Descripcion = "Stray es una aventura en tercera persona donde eres un gato callejero que debe escapar de una ciudad ciberpunk abandonada hace tiempo. Resuelve acertijos, escóndete de los drones y descubre los misterios de sus habitantes robots.",
                    Precio = 29.99m,
                    CategoriaId = 5,
                    Desarrollador = "BlueTwelve Studio",
                    Publisher = "Annapurna Interactive",
                    Tags = "Adventure, Cyberpunk, Atmospheric, Cats, Exploration",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 97,
                    ReviewsCantidad = 70000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5-2550K o AMD FX-8350\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 650 Ti o AMD Radeon R7 360\nALMACENAMIENTO: 10 GB\nDirectX: Versión 12",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5-8400 o AMD Ryzen 5 2600\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 1080 o AMD Radeon RX 5700\nALMACENAMIENTO: 10 GB SSD\nDirectX: Versión 12",
                    TieneContenidoMaduro = false,
                    DescripcionContenidoMaduro = "Temas infantiles y violencia de ciencia ficción leve (ESRB: E10+).",
                    FechaPublicacion = new DateTime(2022, 7, 19)
                },
                new Juego
                {
                    Titulo = "Uncharted: Legacy of Thieves Collection",
                    Descripcion = "Uncharted: Legacy of Thieves Collection reúne Uncharted 4: El Desenlace del Ladrón y Uncharted: El Legado Perdido. Encarna a Nathan Drake y Chloe Frazer en aventuras de caza de tesoros por el mundo, con escenas de acción espectaculares.",
                    Precio = 49.99m,
                    CategoriaId = 5,
                    Desarrollador = "Naughty Dog",
                    Publisher = "PlayStation Publishing LLC",
                    Tags = "Adventure, Action, Story Rich, Cinematic, Singleplayer",
                    ReviewsResumen = "Muy positivas",
                    ReviewsPorcentaje = 91,
                    ReviewsCantidad = 42000,
                    RequisitosMinimos = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i5-4430 o AMD Ryzen 3 1200\nMEMORIA: 8 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 960 o AMD Radeon RX 470\nALMACENAMIENTO: 125 GB\nDirectX: Versión 12",
                    RequisitosRecomendados = "OS: Windows 10 de 64 bits\nPROCESADOR: Intel Core i7-4770 o AMD Ryzen 5 1500X\nMEMORIA: 16 GB de RAM\nGRÁFICOS: NVIDIA GeForce GTX 1060 o AMD Radeon RX 570\nALMACENAMIENTO: 125 GB SSD\nDirectX: Versión 12",
                    TieneContenidoMaduro = true,
                    DescripcionContenidoMaduro = "Violencia intensa, sangre y lenguaje fuerte (ESRB: M).",
                    FechaPublicacion = new DateTime(2022, 10, 19)
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
