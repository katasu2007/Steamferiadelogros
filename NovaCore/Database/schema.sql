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
