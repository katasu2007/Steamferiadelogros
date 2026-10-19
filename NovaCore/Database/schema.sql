-- =========================================================
-- Esquema de Base de Datos - NovaCore
-- Compatible con SQL Server / SQLite (con ligeros ajustes de tipos)
-- =========================================================

CREATE TABLE Categorias (
    CategoriaId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre          VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Usuarios (
    UsuarioId       INTEGER PRIMARY KEY AUTOINCREMENT,
    NombreUsuario   VARCHAR(50)  NOT NULL UNIQUE,
    Correo          VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(256) NOT NULL,
    Rol             VARCHAR(20)  NOT NULL DEFAULT 'Usuario', -- 'Usuario' | 'Administrador'
    Saldo           DECIMAL(10,2) NOT NULL DEFAULT 0,
    Activo          BIT NOT NULL DEFAULT 1,                  -- 0 = baneado/desactivado
    AvatarPath      VARCHAR(260) NULL,                       -- foto de perfil elegida desde la app
    FechaRegistro   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Amigos ficticios agregados manualmente desde el Perfil (relación simple usuario-usuario)
CREATE TABLE Amigos (
    AmigoId         INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId       INTEGER NOT NULL,
    NombreAmigo     VARCHAR(50) NOT NULL,
    FechaAgregado   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId)
);

CREATE TABLE Juegos (
    JuegoId             INTEGER PRIMARY KEY AUTOINCREMENT,
    Titulo              VARCHAR(150) NOT NULL,
    Descripcion         TEXT NULL,
    Precio              DECIMAL(10,2) NOT NULL DEFAULT 0,
    CategoriaId         INTEGER NOT NULL,
    CategoriaNombre     VARCHAR(100) NULL,        -- redundante con Categorias, pero lo usa la app
    Desarrollador       VARCHAR(150) NULL,
    Publisher           VARCHAR(150) NULL,
    Tags                VARCHAR(300) NULL,        -- separados por coma, ej: "FPS, Action, Military"
    ReviewsResumen      VARCHAR(40)  NULL,         -- "Very Positive", "Mixed", etc.
    ReviewsPorcentaje   INT NOT NULL DEFAULT 0,
    ReviewsCantidad     INT NOT NULL DEFAULT 0,
    RequisitosMinimos      TEXT NULL,
    RequisitosRecomendados TEXT NULL,
    TieneContenidoMaduro   BIT NOT NULL DEFAULT 0,
    DescripcionContenidoMaduro TEXT NULL,
    ImagenPortada       VARCHAR(260) NULL,        -- ruta local o URL
    FechaPublicacion    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Activo              BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (CategoriaId) REFERENCES Categorias(CategoriaId)
);

CREATE TABLE Biblioteca (
    BibliotecaId    INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId       INTEGER NOT NULL,
    JuegoId         INTEGER NOT NULL,
    FechaCompra     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    HorasJugadas    DECIMAL(10,2) NOT NULL DEFAULT 0,
    UltimaSesion    DATETIME NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (JuegoId) REFERENCES Juegos(JuegoId),
    UNIQUE (UsuarioId, JuegoId)  -- evita comprar el mismo juego dos veces
);

CREATE TABLE Transacciones (
    TransaccionId   INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId       INTEGER NOT NULL,
    JuegoId         INTEGER NOT NULL,
    Monto           DECIMAL(10,2) NOT NULL,
    Fecha           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Estado          VARCHAR(20) NOT NULL DEFAULT 'Completada', -- Completada | Fallida | Reembolsada
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (JuegoId) REFERENCES Juegos(JuegoId)
);

-- Carrito de compras pendiente (equivalente a "Add to Cart" antes del checkout)
CREATE TABLE CarritoItems (
    CarritoItemId   INTEGER PRIMARY KEY AUTOINCREMENT,
    UsuarioId       INTEGER NOT NULL,
    JuegoId         INTEGER NOT NULL,
    FechaAgregado   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    FOREIGN KEY (JuegoId) REFERENCES Juegos(JuegoId),
    UNIQUE (UsuarioId, JuegoId)
);

-- Reportes de moderación: contra un juego o contra otro usuario
CREATE TABLE Reportes (
    ReporteId               INTEGER PRIMARY KEY AUTOINCREMENT,
    Tipo                    VARCHAR(20) NOT NULL, -- 'Juego' | 'Usuario'
    ObjetivoId              INTEGER NOT NULL,     -- JuegoId o UsuarioId reportado, según Tipo
    UsuarioReportanteId     INTEGER NOT NULL,
    Motivo                  TEXT NOT NULL,
    Fecha                   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Estado                  VARCHAR(20) NOT NULL DEFAULT 'Pendiente', -- Pendiente | Revisado
    FOREIGN KEY (UsuarioReportanteId) REFERENCES Usuarios(UsuarioId)
);

-- Datos semilla mínimos: solo se crea la cuenta Administrador.
-- Cualquier otra cuenta ("Usuario") nace cuando alguien se registra desde la app.
INSERT INTO Categorias (Nombre) VALUES ('Acción'), ('RPG'), ('Estrategia'), ('Indie'), ('Aventura');

INSERT INTO Usuarios (NombreUsuario, Correo, PasswordHash, Rol, Saldo)
VALUES ('admin', 'admin@steamclone.com', 'admin123', 'Administrador', 0);

-- =========================================================
-- Catálogo inicial: 25 videojuegos REALES (5 por categoría).
-- Los textos usan saltos de línea (X'0A' -> \n en la app) para
-- los requisitos y las sinopsis se dejan en una sola línea.
-- =========================================================

-- ---- Acción (CategoriaId = 1) ----
INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Cyberpunk 2077',
    'Cyberpunk 2077 es un RPG de acción y mundo abierto ambientado en Night City, una megalópolis obsesionada con el poder, el glamour y las modificaciones corporales. Juegas como V, un mercenario que persigue un implante único que es la clave de la inmortalidad.',
    59.99, 1, 'Acción', 'CD PROJEKT RED', 'CD PROJEKT RED',
    'RPG, Open World, Cyberpunk, Action, First-Person',
    'Muy positivas', 86, 750000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Core i7-6700 o Ryzen 5 1600 | MEMORIA: 12 GB de RAM | GRÁFICOS: GeForce GTX 1060 6GB o Radeon RX 580 8GB | ALMACENAMIENTO: 70 GB SSD | DirectX: 12',
    'OS: Windows 10 de 64 bits | PROCESADOR: Core i7-12700 o Ryzen 7 7800X3D | MEMORIA: 16 GB de RAM | GRÁFICOS: GeForce RTX 2060 SUPER o Radeon RX 5700 XT | ALMACENAMIENTO: 70 GB SSD | DirectX: 12',
    1, 'Violencia intensa, sangre, lenguaje fuerte, contenido sexual y uso de drogas (ESRB: M).',
    '2020-12-10'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Resident Evil 4',
    'Resident Evil 4 es el remake del clásico del survival horror. Leon S. Kennedy, seis años después del desastre de Raccoon City, es enviado a rescatar a la hija secuestrada del presidente, pero descubre algo siniestro en una remota aldea europea.',
    59.99, 1, 'Acción', 'CAPCOM Co., Ltd.', 'CAPCOM Co., Ltd.',
    'Survival Horror, Action, Third-Person Shooter, Singleplayer',
    'Muy positivas', 97, 210000,
    'OS: Windows 10 de 64 bits | PROCESADOR: AMD Ryzen 3 1200 o Intel Core i5-7500 | MEMORIA: 8 GB de RAM | GRÁFICOS: Radeon RX 560 (4 GB) o GeForce GTX 1050 Ti (4 GB) | ALMACENAMIENTO: 60 GB | DirectX: 12',
    'OS: Windows 10/11 de 64 bits | PROCESADOR: AMD Ryzen 5 3600 o Intel Core i7 8700 | MEMORIA: 16 GB de RAM | GRÁFICOS: Radeon RX 5700 o GeForce GTX 1070 | ALMACENAMIENTO: 60 GB SSD | DirectX: 12',
    1, 'Violencia intensa, gore, sangre y lenguaje fuerte (ESRB: M).',
    '2023-03-24'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'DOOM Eternal',
    'DOOM Eternal es la secuela de DOOM (2016). Eres el DOOM Slayer en una cruzada contra los demonios del Infierno que han invadido la Tierra, con un arsenal ampliado, movilidad extrema y combates frenéticos a 60 fps.',
    39.99, 1, 'Acción', 'id Software', 'Bethesda Softworks',
    'Action, FPS, Demons, Gore, Fast-Paced',
    'Muy positivas', 92, 195000,
    'OS: Windows 7/10 de 64 bits | PROCESADOR: Intel Core i5 de 3.3 GHz o AMD Ryzen 3 1200 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 1050 Ti o Radeon R9 280 | ALMACENAMIENTO: 80 GB | DirectX: 11',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i7-6700K o AMD Ryzen 7 1800X | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 1080 o Radeon RX Vega 56 | ALMACENAMIENTO: 80 GB | DirectX: 11',
    1, 'Violencia intensa, gore y lenguaje fuerte (ESRB: M).',
    '2020-03-20'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Grand Theft Auto V',
    'Grand Theft Auto V es un juego de acción y mundo abierto ambientado en Los Santos. Alterna entre tres criminales -Franklin, Michael y Trevor- para llevar a cabo atracos mientras exploras la ciudad en el modo historia o en GTA Online.',
    29.99, 1, 'Acción', 'Rockstar North', 'Rockstar Games',
    'Open World, Action, Multiplayer, Crime, Third-Person Shooter',
    'Muy positivas', 88, 1500000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5 3470 o AMD X8 FX-8350 | MEMORIA: 8 GB de RAM | GRÁFICOS: NVIDIA GTX 660 2GB o AMD HD 7870 2GB | ALMACENAMIENTO: 100 GB | DirectX: 11',
    'OS: Windows 10/11 de 64 bits | PROCESADOR: Intel Core i5 6600K o AMD FX-9590 | MEMORIA: 16 GB de RAM | GRÁFICOS: NVIDIA GTX 970 o AMD R9 390 | ALMACENAMIENTO: 100 GB SSD | DirectX: 11',
    1, 'Violencia intensa, sangre, lenguaje fuerte, contenido sexual y uso de drogas (ESRB: M).',
    '2015-04-14'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Devil May Cry 5',
    'Devil May Cry 5 es la nueva entrega de la saga de acción de estilo. Los demonios han tomado la ciudad y depende de Nero, Dante y V enfrentar al Rey Demonio Urizen en combates de estilo libre con combos espectaculares y una banda sonora dinámica.',
    29.99, 1, 'Acción', 'CAPCOM Co., Ltd.', 'CAPCOM Co., Ltd.',
    'Action, Hack and Slash, Demons, Stylish, Singleplayer',
    'Muy positivas', 95, 120000,
    'OS: Windows 7/8.1/10 de 64 bits | PROCESADOR: Intel Core i5-4460 o AMD FX-6300 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 760 o Radeon R7 260X | ALMACENAMIENTO: 35 GB | DirectX: 11',
    'OS: Windows 7/8.1/10 de 64 bits | PROCESADOR: Intel Core i7-3770 o AMD FX-9590 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 1060 o Radeon RX 480 | ALMACENAMIENTO: 35 GB | DirectX: 11',
    1, 'Violencia frecuente, sangre y lenguaje fuerte (ESRB: M).',
    '2019-03-08'
);

-- ---- RPG (CategoriaId = 2) ----
INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Elden Ring',
    'Elden Ring es un RPG de acción de mundo abierto creado por FromSoftware y George R. R. Martin. El Anillo Ancestral se ha roto y las Tierras Intermedias están en ruinas; levántate como Sinluz y conviértete en Señor del Círculo.',
    59.99, 2, 'RPG', 'FromSoftware Inc.', 'FromSoftware Inc.',
    'Souls-like, Open World, Dark Fantasy, RPG, Difficult',
    'Muy positivas', 94, 750000,
    'OS: Windows 10 | PROCESADOR: INTEL CORE I5-8400 o AMD RYZEN 3 3300X | MEMORIA: 12 GB de RAM | GRÁFICOS: GEFORCE GTX 1060 3 GB o RADEON RX 580 4 GB | ALMACENAMIENTO: 60 GB | DirectX: 12',
    'OS: Windows 11 | PROCESADOR: INTEL CORE I7-8700K o AMD RYZEN 5 3600X | MEMORIA: 16 GB de RAM | GRÁFICOS: GEFORCE GTX 1070 8 GB o RADEON RX VEGA 56 8 GB | ALMACENAMIENTO: 60 GB SSD | DirectX: 12',
    1, 'Violencia fantástica y lenguaje suave (ESRB: M).',
    '2022-02-25'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'The Witcher 3: Wild Hunt',
    'The Witcher 3: Wild Hunt es un RPG de mundo abierto donde encarnas a Geralt de Rivia, un cazador de monstruos que busca a Ciri, una joven con un poder capaz de destruir el mundo, mientras la Cacería Salvaje acecha.',
    39.99, 2, 'RPG', 'CD PROJEKT RED', 'CD PROJEKT RED',
    'RPG, Open World, Fantasy, Story Rich, Mature',
    'Extremadamente positivas', 96, 780000,
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core i5-2500K o AMD Phenom II X4 940 | MEMORIA: 6 GB de RAM | GRÁFICOS: GeForce GTX 660 o Radeon HD 7870 | ALMACENAMIENTO: 50 GB | DirectX: 11',
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core i7-3770 o AMD FX-8350 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 770 o Radeon R9 290 | ALMACENAMIENTO: 50 GB | DirectX: 11',
    1, 'Violencia intensa, sangre, desnudos y contenido sexual (ESRB: M).',
    '2015-05-18'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Baldur''s Gate 3',
    'Baldur''s Gate 3 es un RPG por turnos basado en Dungeons & Dragons. Reúne a tu grupo de aventureros, domina la estrategia en combates tácticos y regresa a los Reinos Olvidados en una historia de compañerismo, traición y sacrificio.',
    59.99, 2, 'RPG', 'Larian Studios', 'Larian Studios',
    'RPG, Turn-Based, Story Rich, Character Customization, Co-op',
    'Extremadamente positivas', 96, 700000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel I5 4690 o AMD FX 8350 | MEMORIA: 8 GB de RAM | GRÁFICOS: Nvidia GTX 970 o AMD RX 480 4GB | ALMACENAMIENTO: 150 GB SSD | DirectX: 11',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel i7 8700K o AMD R5 3600 | MEMORIA: 16 GB de RAM | GRÁFICOS: Nvidia 2060 Super o AMD RX 5700 XT | ALMACENAMIENTO: 150 GB SSD | DirectX: 11',
    1, 'Violencia, sangre, desnudos, contenido sexual y lenguaje fuerte (ESRB: M).',
    '2023-08-03'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Persona 5 Royal',
    'Persona 5 Royal es un RPG por turnos donde un grupo de estudiantes forma a los Ladrones Fantasma de Corazones para reformar la sociedad robando los corazones corruptos de los adultos. Incluye nuevas historias, personajes y contenido.',
    59.99, 2, 'RPG', 'ATLUS', 'SEGA',
    'JRPG, Turn-Based, Anime, Story Rich, Singleplayer',
    'Extremadamente positivas', 96, 90000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i7-4790 o AMD Ryzen 5 1500X | MEMORIA: 8 GB de RAM | GRÁFICOS: Nvidia GTX 650 Ti o AMD Radeon HD 7870 | ALMACENAMIENTO: 40 GB | DirectX: 11',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i7-4790 o AMD Ryzen 5 1500X | MEMORIA: 8 GB de RAM | GRÁFICOS: Nvidia GTX 760 o AMD Radeon R7 260X | ALMACENAMIENTO: 40 GB | DirectX: 11',
    1, 'Violencia de fantasía, temas maduros, lenguaje fuerte y contenido sexual sugerido (ESRB: M).',
    '2022-10-21'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Monster Hunter: World',
    'Monster Hunter: World es un RPG de acción donde cazas enormes monstruos en ecosistemas vivos. Forja armaduras y armas con los materiales que obtienes, y únete con hasta tres jugadores en línea para derribar a las bestias más feroces.',
    29.99, 2, 'RPG', 'CAPCOM Co., Ltd.', 'CAPCOM Co., Ltd.',
    'Action, Co-op, Multiplayer, RPG, Hunting',
    'Muy positivas', 88, 380000,
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core i5-4460 3.2GHz o AMD FX-6300 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 760 o Radeon R7 260x | ALMACENAMIENTO: 50 GB | DirectX: 11',
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core i7 3770 3.4GHz o AMD FX-8350 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 1060 o Radeon RX 570X | ALMACENAMIENTO: 50 GB | DirectX: 11',
    1, 'Violencia de fantasía y sangre moderada (ESRB: T/PEGI 12).',
    '2018-08-09'
);

-- ---- Estrategia (CategoriaId = 3) ----
INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Age of Empires IV',
    'Age of Empires IV es un juego de estrategia en tiempo real que pone la historia a tu alcance. Elige entre ocho civilizaciones, construye ciudades, gestiona recursos y comanda ejércitos en campañas históricas y multijugador.',
    39.99, 3, 'Estrategia', 'Relic Entertainment', 'Xbox Game Studios',
    'Strategy, RTS, Historical, Base Building, Multiplayer',
    'Muy positivas', 88, 65000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5-6300U o AMD Ryzen 5 2400G | MEMORIA: 8 GB de RAM | GRÁFICOS: Intel HD 520 o AMD Radeon RX Vega 11 | ALMACENAMIENTO: 50 GB | DirectX: 12',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5-8400 o AMD Ryzen 5 2600 | MEMORIA: 16 GB de RAM | GRÁFICOS: GeForce GTX 970 o Radeon RX 570 | ALMACENAMIENTO: 50 GB | DirectX: 12',
    0, '', '2021-10-28'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Sid Meier''s Civilization VI',
    'Civilization VI amplía el concepto de construir un imperio que resista el paso del tiempo. Expande tu ciudad casilla por casilla, desarrolla la cultura y la tecnología, y compite por la victoria por medio de la diplomacia, la ciencia o la conquista.',
    59.99, 3, 'Estrategia', 'Firaxis Games', '2K',
    'Strategy, Turn-Based, 4X, Historical, Multiplayer',
    'Muy positivas', 82, 250000,
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core i3 2.5 GHz o AMD Phenom II 2.6 GHz | MEMORIA: 4 GB de RAM | GRÁFICOS: 1 GB DirectX 11 | ALMACENAMIENTO: 12 GB | DirectX: 11',
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core i5 2.5 GHz+ o AMD FX8350 4.0 GHz+ | MEMORIA: 8 GB de RAM | GRÁFICOS: 2 GB DirectX 11 | ALMACENAMIENTO: 12 GB | DirectX: 11',
    0, '', '2016-10-21'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Hearts of Iron IV',
    'Hearts of Iron IV es un juego de gran estrategia de la Segunda Guerra Mundial. Toma el control de cualquier nación, gestiona su economía, diplomacia y ejército, y reescribe la historia en uno de los conflictos más grandes de la humanidad.',
    39.99, 3, 'Estrategia', 'Paradox Development Studio', 'Paradox Interactive',
    'Strategy, Grand Strategy, World War II, Historical, Simulation',
    'Muy positivas', 87, 175000,
    'OS: Windows 7 de 64 bits | PROCESADOR: Intel Core 2 Quad Q9400 2.66 GHz o AMD Athlon II X4 650 3.20 GHz | MEMORIA: 4 GB de RAM | GRÁFICOS: ATI Radeon HD 5850 o NVIDIA GeForce GTX 470 | ALMACENAMIENTO: 11 GB | DirectX: 9.0c',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5 750 2.66 GHz o AMD Phenom II X4 955 3.20 GHz | MEMORIA: 4 GB de RAM | GRÁFICOS: ATI Radeon HD 6950 o NVIDIA GeForce GTX 570 | ALMACENAMIENTO: 11 GB | DirectX: 9.0c',
    0, '', '2016-06-06'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'StarCraft II',
    'StarCraft II es el RTS de ciencia ficción por excelencia. Comanda a los Terran, Protoss o Zerg en campañas épicas y en un multijugador competitivo que definió los esports, con un equilibrio de razas y estrategias siempre en evolución.',
    0.00, 3, 'Estrategia', 'Blizzard Entertainment', 'Blizzard Entertainment',
    'Strategy, RTS, Sci-fi, Competitive, Multiplayer',
    'Muy positivas', 92, 95000,
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core 2 Duo 2.6 GHz o AMD Athlon 64 X2 5600+ | MEMORIA: 2 GB de RAM | GRÁFICOS: NVIDIA GeForce 7600 GT o ATI Radeon HD 2600 XT | ALMACENAMIENTO: 30 GB | DirectX: 9.0c',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5 o AMD FX Series | MEMORIA: 4 GB de RAM | GRÁFICOS: NVIDIA GeForce GTX 650 o ATI Radeon HD 7790 | ALMACENAMIENTO: 30 GB | DirectX: 11',
    1, 'Violencia de ciencia ficción moderada y temas bélicos (ESRB: T).', '2010-07-27'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Cities: Skylines',
    'Cities: Skylines es un simulador de construcción de ciudades que reimagina la experiencia clásica. Diseña tu ciudad, administra el tráfico, los servicios públicos y las políticas, y hazla crecer desde un pequeño asentamiento hasta una gran metrópolis.',
    29.99, 3, 'Estrategia', 'Colossal Order Ltd.', 'Paradox Interactive',
    'Simulation, City Builder, Management, Sandbox, Moddable',
    'Muy positivas', 90, 170000,
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core 2 Duo 3.0 GHz o AMD Athlon 64 X2 6400+ | MEMORIA: 4 GB de RAM | GRÁFICOS: NVIDIA GeForce GTX 260 o ATI Radeon HD 5670 | ALMACENAMIENTO: 4 GB | DirectX: 9.0c',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5-3470 o AMD FX-6300 | MEMORIA: 6 GB de RAM | GRÁFICOS: NVIDIA GeForce GTX 660 o AMD Radeon HD 7870 | ALMACENAMIENTO: 4 GB | DirectX: 11',
    0, '', '2015-03-10'
);

-- ---- Indie (CategoriaId = 4) ----
INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Hollow Knight',
    'Hollow Knight es un metroidvania de acción y aventura ambientado en el reino subterráneo de Hallownest. Explora cavernas interconectadas, enfrenta criaturas corrompidas y descubre los misterios de un mundo dibujado a mano con un estilo inconfundible.',
    14.99, 4, 'Indie', 'Team Cherry', 'Team Cherry',
    'Metroidvania, Platformer, Difficult, Atmospheric, Indie',
    'Extremadamente positivas', 98, 145000,
    'OS: Windows 7 | PROCESADOR: Intel Core 2 Duo E5200 | MEMORIA: 4 GB de RAM | GRÁFICOS: GeForce 9800GTX+ (1GB) | ALMACENAMIENTO: 9 GB | DirectX: 10',
    'OS: Windows 10 | PROCESADOR: Intel Core i5 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 560 | ALMACENAMIENTO: 9 GB | DirectX: 11',
    0, 'Violencia de fantasía leve (ESRB: E10+).', '2017-02-24'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Celeste',
    'Celeste es un juego de plataformas de precisión sobre superar obstáculos y dificultades. Ayuda a Madeline a escalar la montaña Celeste, descubre su verdadero yo y enfrenta cientos de desafíos diseñados a mano con controles precisos y una banda sonora memorable.',
    19.99, 4, 'Indie', 'Maddy Makes Games', 'Maddy Makes Games',
    'Platformer, Difficult, Pixel Graphics, Indie, Story Rich',
    'Extremadamente positivas', 98, 90000,
    'OS: Windows 7/8/10 | PROCESADOR: Intel Core i3 o superior | MEMORIA: 2 GB de RAM | GRÁFICOS: OpenGL compatible con 512 MB | ALMACENAMIENTO: 1 GB | DirectX: 9.0c',
    'OS: Windows 10 | PROCESADOR: Intel Core i5 | MEMORIA: 4 GB de RAM | GRÁFICOS: OpenGL compatible con 1 GB | ALMACENAMIENTO: 1 GB | DirectX: 11',
    0, 'Temas de ansiedad y depresión; lenguaje leve (ESRB: E10+).', '2018-01-25'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Hades',
    'Hades es un roguelike de acción donde encarnas a Zagreus, príncipe del Inframundo, mientras lucha por escapar del reino de su padre Hades. Cada huida exitosa y cada muerte te acerca a la verdad y desbloquea nuevas habilidades narrativas.',
    24.99, 4, 'Indie', 'Supergiant Games', 'Supergiant Games',
    'Roguelike, Action, Hack and Slash, Greek Mythology, Indie',
    'Extremadamente positivas', 98, 175000,
    'OS: Windows 7/8/10 | PROCESADOR: 1.4GHz de doble núcleo | MEMORIA: 4 GB de RAM | GRÁFICOS: DirectX 10+ | ALMACENAMIENTO: 15 GB | DirectX: 10',
    'OS: Windows 10 | PROCESADOR: 3.0GHz de cuatro núcleos | MEMORIA: 8 GB de RAM | GRÁFICOS: DirectX 11 compatible con 2GB | ALMACENAMIENTO: 15 GB | DirectX: 11',
    1, 'Violencia de fantasía, temas mitológicos y lenguaje leve (ESRB: T).', '2020-09-17'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Stardew Valley',
    'Stardew Valley es un RPG de simulación agrícola. Hereda la vieja granja de tu abuelo y emprende una nueva vida: cultiva, cría animales, explora minas, pesca, conoce a los habitantes del pueblo y forma una familia.',
    14.99, 4, 'Indie', 'ConcernedApe', 'ConcernedApe',
    'Farming Sim, Life Sim, Pixel Graphics, RPG, Relaxing',
    'Extremadamente positivas', 98, 520000,
    'OS: Windows Vista o superior | PROCESADOR: 2 GHz | MEMORIA: 2 GB de RAM | GRÁFICOS: Tarjeta con 256 MB de memoria de video | ALMACENAMIENTO: 1 GB | DirectX: 10',
    'OS: Windows 10 | PROCESADOR: 2 GHz o superior | MEMORIA: 4 GB de RAM | GRÁFICOS: Tarjeta con 512 MB de memoria de video | ALMACENAMIENTO: 1 GB | DirectX: 11',
    0, 'Referencias al alcohol y temas emocionales leves (ESRB: E10+).', '2016-02-26'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Undertale',
    'Undertale es un RPG por turnos donde controlas a un niño humano que cae en un mundo subterráneo habitado por monstruos. Puedes elegir matar o perdonar a cada enemigo, y cada decisión altera profundamente la historia y su final.',
    9.99, 4, 'Indie', 'tobyfox', 'tobyfox',
    'RPG, Story Rich, Pixel Graphics, Multiple Endings, Indie',
    'Extremadamente positivas', 97, 210000,
    'OS: Windows Vista o superior | PROCESADOR: 2 GHz de doble núcleo | MEMORIA: 2 GB de RAM | GRÁFICOS: Tarjeta de video con 128 MB | ALMACENAMIENTO: 400 MB | DirectX: 9.0',
    'OS: Windows 10 | PROCESADOR: 2 GHz de doble núcleo o superior | MEMORIA: 4 GB de RAM | GRÁFICOS: Tarjeta de video con 256 MB | ALMACENAMIENTO: 400 MB | DirectX: 11',
    1, 'Violencia de fantasía, temas maduros y lenguaje leve (ESRB: T).', '2015-09-15'
);

-- ---- Aventura (CategoriaId = 5) ----
INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Red Dead Redemption 2',
    'Red Dead Redemption 2 es una épica del Lejano Oeste. Tras un atraco fallido, Arthur Morgan y la banda de Van der Linde deben huir a través de Estados Unidos, en una historia sobre la lealtad, la redención y el fin de la era del salvaje oeste.',
    59.99, 5, 'Aventura', 'Rockstar Games', 'Rockstar Games',
    'Open World, Story Rich, Action, Western, Adventure',
    'Muy positivas', 90, 650000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5-2500K o AMD FX-6300 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 770 2GB o Radeon R9 280 3GB | ALMACENAMIENTO: 150 GB | DirectX: 12',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i7-4770K o AMD Ryzen 5 1500X | MEMORIA: 12 GB de RAM | GRÁFICOS: GeForce GTX 1060 6GB o Radeon RX 480 4GB | ALMACENAMIENTO: 150 GB SSD | DirectX: 12',
    1, 'Violencia intensa, sangre, lenguaje fuerte, desnudos y contenido sexual (ESRB: M).',
    '2019-12-05'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'God of War',
    'God of War acompaña a Kratos, ahora padre, y a su hijo Atreus en un viaje por los reinos nórdicos. Deberán cumplir el último deseo de la madre de Atreus, enfrentando dioses y criaturas en un relato íntimo sobre paternidad y redención.',
    49.99, 5, 'Aventura', 'Santa Monica Studio', 'PlayStation Publishing LLC',
    'Action, Adventure, Mythology, Story Rich, Singleplayer',
    'Muy positivas', 95, 130000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel i5-2500k o AMD Ryzen 3 1200 | MEMORIA: 8 GB de RAM | GRÁFICOS: NVIDIA GTX 960 4GB o AMD RX 470 4GB | ALMACENAMIENTO: 70 GB | DirectX: 11',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel i5-6600k o AMD Ryzen 5 2400G | MEMORIA: 8 GB de RAM | GRÁFICOS: NVIDIA GTX 1060 6GB o AMD RX 570 8GB | ALMACENAMIENTO: 70 GB SSD | DirectX: 11',
    1, 'Violencia intensa, sangre y lenguaje fuerte (ESRB: M).', '2022-01-14'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Rise of the Tomb Raider',
    'Rise of the Tomb Raider continúa la historia de Lara Croft en su búsqueda del secreto de la inmortalidad. Viaja a Siberia, explora tumbas ancestrales y sobrevive a una organización enemiga en una aventura de acción y exploración.',
    29.99, 5, 'Aventura', 'Crystal Dynamics', 'Square Enix',
    'Action, Adventure, Exploration, Female Protagonist, Singleplayer',
    'Muy positivas', 92, 78000,
    'OS: Windows 7/8/10 de 64 bits | PROCESADOR: Intel Core i3-2100 o AMD Athlon II X2 260 | MEMORIA: 6 GB de RAM | GRÁFICOS: GeForce GTX 650 o Radeon HD 7770 | ALMACENAMIENTO: 25 GB | DirectX: 11',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i7-3770 o AMD FX-8350 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 970 o Radeon R9 290X | ALMACENAMIENTO: 25 GB | DirectX: 11',
    1, 'Violencia, sangre y temas maduros (ESRB: M).', '2016-01-28'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Stray',
    'Stray es una aventura en tercera persona donde eres un gato callejero que debe escapar de una ciudad ciberpunk abandonada hace tiempo. Resuelve acertijos, escóndete de los drones y descubre los misterios de sus habitantes robots.',
    29.99, 5, 'Aventura', 'BlueTwelve Studio', 'Annapurna Interactive',
    'Adventure, Cyberpunk, Atmospheric, Cats, Exploration',
    'Muy positivas', 97, 70000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5-2550K o AMD FX-8350 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 650 Ti o Radeon R7 360 | ALMACENAMIENTO: 10 GB | DirectX: 12',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5-8400 o AMD Ryzen 5 2600 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 1080 o Radeon RX 5700 | ALMACENAMIENTO: 10 GB SSD | DirectX: 12',
    0, 'Temas infantiles y violencia de ciencia ficción leve (ESRB: E10+).', '2022-07-19'
);

INSERT INTO Juegos (Titulo, Descripcion, Precio, CategoriaId, CategoriaNombre, Desarrollador, Publisher, Tags, ReviewsResumen, ReviewsPorcentaje, ReviewsCantidad, RequisitosMinimos, RequisitosRecomendados, TieneContenidoMaduro, DescripcionContenidoMaduro, FechaPublicacion)
VALUES (
    'Uncharted: Legacy of Thieves Collection',
    'Uncharted: Legacy of Thieves Collection reúne Uncharted 4: El Desenlace del Ladrón y Uncharted: El Legado Perdido. Encarna a Nathan Drake y Chloe Frazer en aventuras de caza de tesoros por el mundo, con escenas de acción espectaculares.',
    49.99, 5, 'Aventura', 'Naughty Dog', 'PlayStation Publishing LLC',
    'Adventure, Action, Story Rich, Cinematic, Singleplayer',
    'Muy positivas', 91, 42000,
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i5-4430 o AMD Ryzen 3 1200 | MEMORIA: 8 GB de RAM | GRÁFICOS: GeForce GTX 960 o Radeon RX 470 | ALMACENAMIENTO: 125 GB | DirectX: 12',
    'OS: Windows 10 de 64 bits | PROCESADOR: Intel Core i7-4770 o AMD Ryzen 5 1500X | MEMORIA: 16 GB de RAM | GRÁFICOS: GeForce GTX 1060 o Radeon RX 570 | ALMACENAMIENTO: 125 GB SSD | DirectX: 12',
    1, 'Violencia intensa, sangre y lenguaje fuerte (ESRB: M).', '2022-10-19'
);
