# NovaCore

> **Tienda digital de videojuegos** — un clon educativo de la experiencia de Steam,
> construido como aplicación de escritorio con **WinForms** y **.NET 8**.
> Proyecto de la materia *Desarrollo de Aplicaciones* (Versión 3).

---

## Índice

1. [Introducción y ficha técnica](#1-introducción-y-ficha-técnica)
2. [Evolución del proyecto](#2-evolución-del-proyecto)
3. [Arquitectura](#3-arquitectura)
4. [Árbol del repositorio](#4-árbol-del-repositorio)
5. [Módulos funcionales (manual de usuario)](#5-módulos-funcionales-manual-de-usuario)
6. [Persistencia de datos](#6-persistencia-de-datos)
7. [Instalación, compilación y uso](#7-instalación-compilación-y-uso)
8. [Lecciones aprendidas y trabajo futuro](#8-lecciones-aprendidas-y-trabajo-futuro)

---

## 1. Introducción y ficha técnica

**NovaCore** es una aplicación de escritorio que recrea las funciones esenciales de una
tienda de videojuegos tipo Steam: catálogo con categorías, ficha de detalle, carrito,
biblioteca personal, lista de deseos, billetera con pasarela de pago simulada,
notificaciones, sistema de amigos, reseñas/reportes y un panel de administración.

La ventana está **dimensionada como un teléfono móvil (400 × 820 px)** y funciona como
una **SPA (Single Page Application)**: una sola ventana que intercambia vistas
(`UserControl`) en lugar de abrir muchas ventanas. Toda la interfaz está en **español**
y usa un **tema oscuro con acento naranja** ("tema nova").

| Dato | Valor |
|---|---|
| **Nombre** | NovaCore |
| **Tipo** | Clon educativo de tienda de videojuegos (estilo Steam) |
| **Versión actual** | 3 |
| **Tecnología** | C# · WinForms · .NET 8 (`net8.0-windows`) |
| **Arquitectura** | 3 capas (Models / Services / Views) + SPA |
| **Persistencia** | Archivos JSON locales en `%LOCALAPPDATA%\NovaCore` |
| **Interfaz** | Español, tema oscuro, ventana tipo móvil 400×820 |
| **Repositorio** | https://github.com/katasu2007/Steamferiadelogros.git |
| **Rama principal** | `main` |

---

## 2. Evolución del proyecto

### Fases de desarrollo (P1 → P4)

| Fase | Entregable principal |
|---|---|
| **P1** | Estructura base: modelos, `DataStore` en memoria y primera maqueta de Login/Registro. |
| **P2** | Catálogo, ficha de detalle, carrito y biblioteca con compra que descuenta saldo. |
| **P3** | Persistencia JSON (usuarios, juegos, transacciones, etc.), notificaciones, amigos, reportes y panel de administración. |
| **P4** | Rebranding a **NovaCore**, tema nova, portadas reales, escalado responsivo, traducción total al español y **rediseño completo de la Billetera con pasarela de pago**. |

### Versiones publicadas

| Versión | Commit | Descripción |
|---|---|---|
| **V1** | `089dbbd` | Inicial: clon de Steam (WinForms .NET 8) con persistencia local de usuarios. |
| **V1.1** | `54d4c8a` | Eliminación de cuenta para usuarios y filtros de validación en el registro. |
| **V2** | `30b0902` | Rebranding a NovaCore, tema nova, portadas de juegos, escalado y traducción de la UI al español. |
| **V2.1** | `91d5902` | Documentación: manual completo, bitácora y cuentas de prueba. |
| **V3** | `206ee40` | Rediseño de la Billetera con pasarela de pago y persistencia de saldo (movimientos, recargas, notificación). |

Historial completo con:

```bash
git log --oneline
```

---

## 3. Arquitectura

El proyecto sigue una **arquitectura de 3 capas** clásica, más una capa de presentación
organizada como SPA.

### Capa de Modelos — `NovaCore/Models/`
Clases POCO que representan las entidades y se serializan a JSON. Incluyen enums de
dominio (`RolUsuario`, `EstadoTransaccion`, `TipoTransaccion`, `TipoNotificacion`,
`EstadoSolicitud`, `EstadoReporte`, etc.).

`Usuario`, `Juego`, `Transaccion`, `BibliotecaItem`, `ItemCarrito`, `ItemDeseo`,
`Notificacion`, `Amigo`/`SolicitudAmistad`, `Reporte`, `Categoria`.

### Capa de Servicios — `NovaCore/Services/`
Toda la lógica de negocio y el acceso a datos. Ninguna vista escribe archivos
directamente: siempre pasa por un servicio.

| Servicio | Responsabilidad |
|---|---|
| `DataStore` | Singleton en memoria; carga los JSON y expone listas + `Siguiente*Id`. |
| `PersistenciaService` | Rutas de archivos, carpeta base y guardado/carga JSON (soporta `NOVACORE_DATADIR`). |
| `SessionManager` | Usuario autenticado en la sesión (`UsuarioActual`, saldo en memoria). |
| `UsuarioService` | Registro, login, hashing (PBKDF2), roles, saldo y recargas. |
| `PasswordHasher` | Hash/verificación PBKDF2-SHA256 con sal por usuario; migra contraseñas viejas en texto plano. |
| `JuegoService` | CRUD del catálogo, filtros por categoría y búsqueda. |
| `CarritoService` / `DeseoService` | Carrito de compras y lista de deseos. |
| `BibliotecaService` | Juegos comprados, horas jugadas y última sesión. |
| `TransaccionService` | Compras y recargas, historial y reportes de ventas. |
| `NotificacionService` | Creación, lectura, marcado y borrado de notificaciones. |
| `ReporteService` | Reportes de moderación (contra juegos o usuarios). |
| `RecomendacionService` | Sugerencias de juegos según categorías compradas. |

### Capa de Presentación — `NovaCore/Views/`
Cada pantalla es un `UserControl` que `MainForm` intercambia dentro del contenedor
central. `MainForm` gestiona además el **escalado responsivo** (`AplicarEscala`,
`EscalarArbol`, `EscalarFuentes`) para que la UI se adapte a distintos DPI/tamaños.

### Distribución por roles
- **Usuario**: tienda, búsqueda, detalle, carrito, biblioteca, deseos, billetera,
  perfil, amigos, notificaciones y recomendaciones.
- **Administrador**: todo lo anterior más **Dashboard**, **Gestión de usuarios**,
  **Gestión de juegos** y **Reportes de moderación**.

---

## 4. Árbol del repositorio

```
SteamCloneApp/
├── README.md                     ← este documento (README raíz)
├── clone.txt                     ← credenciales de prueba
├── .gitignore
├── Imagenes/                     ← imágenes de referencia (portadas, logo)
│   ├── ELDEN RING.jpg
│   ├── WARDOGS.jpg
│   └── logo.png
│
└── NovaCore/                     ← proyecto principal (.NET 8 WinForms)
    ├── NovaCore.csproj
    ├── Program.cs                ← punto de entrada (Application.Run MainForm)
    ├── MainForm.cs               ← ventana contenedora SPA + escalado
    ├── README.md                 ← documentación interna del proyecto
    ├── Assets/
    │   └── logo.png
    ├── Database/
    │   └── schema.sql            ← esquema SQL de referencia (8 tablas + seeds)
    ├── Models/
    │   ├── BibliotecaItem.cs
    │   ├── Categoria.cs
    │   ├── ItemCarrito.cs
    │   ├── ItemDeseo.cs
    │   ├── Juego.cs
    │   ├── Notificacion.cs
    │   ├── Reporte.cs
    │   ├── SolicitudAmistad.cs
    │   ├── Transaccion.cs
    │   └── Usuario.cs
    ├── Services/
    │   ├── BibliotecaService.cs
    │   ├── CarritoService.cs
    │   ├── DataStore.cs
    │   ├── DeseoService.cs
    │   ├── JuegoService.cs
    │   ├── NotificacionService.cs
    │   ├── PasswordHasher.cs
    │   ├── PersistenciaService.cs
    │   ├── RecomendacionService.cs
    │   ├── ReporteService.cs
    │   ├── SessionManager.cs
    │   ├── TransaccionService.cs
    │   └── UsuarioService.cs
    └── Views/
        ├── UiTheme.cs            ← paleta, fuentes y factor de escala
        ├── LoginView.cs
        ├── RegisterView.cs
        ├── MenuPageView.cs
        ├── StoreView.cs
        ├── SearchView.cs
        ├── GameDetailView.cs
        ├── CartView.cs
        ├── LibraryView.cs
        ├── WishlistView.cs
        ├── WalletView.cs
        ├── ProfileView.cs
        ├── NotificationsView.cs
        ├── NewsView.cs
        ├── RecomendacionesView.cs
        ├── ReportsView.cs
        ├── AdminDashboardView.cs
        ├── AdminReportsView.cs
        ├── GameFormView.cs
        └── UserManagementView.cs
```

---

## 5. Módulos funcionales (manual de usuario)

### 5.1 Autenticación y registro
- **Login** con nombre de usuario y contraseña. Valida contra los hashes PBKDF2.
- **Registro** con validaciones: usuario y correo únicos, formato de correo, longitud
  de contraseña y confirmación.
- **Eliminación de cuenta**: un usuario puede borrar su propia cuenta desde el perfil.

### 5.2 Tienda y catálogo
- Catálogo con **5 categorías** (Acción, RPG, Estrategia, Indie, Aventura) y **25 juegos
  reales** sembrados.
- **Ficha de detalle** al estilo Steam: portada, desarrollador/publisher, tags,
  resumen de reseñas (%, cantidad), requisitos mínimos/recomendados y aviso de
  **contenido maduro**.
- **Búsqueda** por título y **descubrimiento** por categorías.

### 5.3 Carrito y compra
- Agrega/quita juegos al **carrito** y visualiza el total.
- Al **pagar**, se descuenta el saldo de la billetera y el juego pasa a la
  **biblioteca** (no se puede comprar dos veces el mismo título).

### 5.4 Biblioteca, deseos y recomendaciones
- **Biblioteca**: juegos comprados, con horas jugadas y última sesión.
- **Lista de deseos**: guarda títulos para después.
- **Recomendaciones**: sugerencias basadas en las categorías ya compradas.

### 5.5 Billetera (rediseñada en la V3)
Flujo completo de **pasarela de pago simulada**:

1. Elegir monto **fijo** ($10 / $25 / $50 / $100) o **personalizado**.
2. Monto personalizado validado: **mínimo $5.00 y máximo $500.00**.
3. Elegir **método de pago**:
   - **Tarjeta**: número en bloques de 4 dígitos, vencimiento `MM/AA` y CVC de 3 dígitos
     (validación en vivo).
   - **Gift Card**: código normalizado (sin guiones).
   - **PayPal**: confirmación simple.
4. **Confirmación** con la tarjeta enmascarada (`**** **** **** 4321`).
5. Pantalla "**Procesando pago…**" (~1.4 s).
6. **Éxito** con el nuevo saldo y **notificación de sistema**.

**Gift cards válidas:**

| Código (formato de entrada) | Monto |
|---|---|
| `STEAM-100USD-2026` | $100.00 |
| `STEAM-050USD-2026` | $50.00 |
| `NOVA-025USD-2026` | $25.00 |
| `PUPU-010USD-2026` | $10.00 |

La billetera muestra un **historial de movimientos**: verde (`+`) para recargas y
naranja (`−`) para compras.

### 5.6 Notificaciones y amigos
- **Notificaciones** de compras, recargas (tipo `Sistema`), solicitudes de amistad, etc.
- **Amigos**: agregar amigos y gestionar solicitudes de amistad.

### 5.7 Administración
- **Dashboard** con métricas generales.
- **Gestión de usuarios** (activar/desactivar, roles).
- **Gestión de juegos** (alta/edición del catálogo) mediante `GameFormView`.
- **Reportes de moderación** (`AdminReportsView`).

---

## 6. Persistencia de datos

No se requiere un servidor de base de datos: la app persiste todo en **archivos JSON
locales**, en la carpeta `%LOCALAPPDATA%\NovaCore`.

```
%LOCALAPPDATA%\NovaCore\
├── usuarios.json          ← cuentas, roles, saldo, avatares
├── juegos.json            ← catálogo
├── transacciones.json     ← compras y recargas (Tipo: Compra | Recarga)
├── notificaciones.json    ← notificaciones por usuario
├── biblioteca.json        ← juegos comprados por usuario
├── solicitudes.json       ← solicitudes de amistad
├── catalogo.version       ← marca de versión del catálogo
├── Avatares\              ← fotos de perfil subidas
└── Portadas\              ← portadas de juegos subidas
```

**Carpeta de datos personalizada:** define la variable de entorno `NOVACORE_DATADIR`
para que la app use otra ubicación (ideal para pruebas o entornos portables).

### Esquema SQL de referencia (`NovaCore/Database/schema.sql`)
El archivo `schema.sql` documenta cómo se modelarían los mismos datos en una base de
datos relacional (compatible con **SQL Server** y, con ajustes de tipos, **SQLite**).
Contiene **8 tablas**:

| # | Tabla | Propósito |
|---|---|---|
| 1 | `Categorias` | Categorías del catálogo. |
| 2 | `Usuarios` | Cuentas, rol, saldo, avatar, estado activo. |
| 3 | `Amigos` | Relación simple usuario–amigo agregado desde el perfil. |
| 4 | `Juegos` | Ficha completa (precio, tags, reseñas, requisitos, contenido maduro). |
| 5 | `Biblioteca` | Juegos comprados (con UNIQUE usuario+juego). |
| 6 | `Transacciones` | Compras y movimientos de la billetera. |
| 7 | `CarritoItems` | Carrito pendiente antes del checkout. |
| 8 | `Reportes` | Reportes de moderación (juego o usuario). |

Además incluye **datos semilla**: 5 categorías, la cuenta `Administrador` y el
**catálogo inicial de 25 juegos** (5 por categoría).

---

## 7. Instalación, compilación y uso

### Requisitos
- **Windows 10/11**
- **[.NET SDK 8.0](https://dotnet.microsoft.com/download)** (incluye el runtime de
  escritorio `net8.0-windows`)

### Compilar y ejecutar

```bash
# 1. Clonar
git clone https://github.com/katasu2007/Steamferiadelogros.git

# 2. Compilar (desde la carpeta del proyecto)
cd SteamCloneApp/NovaCore
dotnet build NovaCore.csproj -v q --nologo

# 3. Ejecutar
dotnet run --project NovaCore.csproj
```

Compilación limpia alternativa (genera el `.exe` en `bin\Debug\net8.0-windows\`):

```bash
dotnet build NovaCore.csproj -c Release
```

### Cuentas de prueba (`clone.txt`)

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `admin123` | Administrador |
| `rokoko` | `12345` | Usuario |
| `sushi` | `xd123` | Usuario |
| `Kat` | `ola123` | Usuario |
| `admin` (cuenta semilla) | `admin@steamclone.com` | Administrador |

> Las contraseñas se guardan **hasheadas** con PBKDF2-SHA256. Los registros antiguos en
> texto plano se migran automáticamente al iniciar sesión.

### Carpeta de datos personalizada

```powershell
$env:NOVACORE_DATADIR = "C:\mi\carpeta\datos"
dotnet run --project NovaCore.csproj
```

---

## 8. Lecciones aprendidas y trabajo futuro

### Lecciones aprendidas
- **Separar lógica de UI** (patrón Models/Services/Views) facilita muchísimo extender el
  proyecto: añadir la Billetera no obligó a tocar el resto de módulos.
- **Persistir en JSON** fue una solución ágil y portable, sin depender de un motor de
  base de datos, adecuada para el alcance académico.
- **El escalado responsivo** (un factor global en `UiTheme.EscalaUI`) evita duplicar
  medidas y mantiene la UI coherente en distintos tamaños/DPI.
- **La validación debe vivir en los servicios**, no solo en la UI: validar el monto de
  recarga ($5–$500) en `UsuarioService` evita estados inválidos aunque cambie la vista.
- **Hashear contraseñas** con PBKDF2 desde el inicio sería lo ideal; migrar registros en
  texto plano fue una buena red de seguridad.

### Trabajo futuro
- Migrar la persistencia de JSON a **SQLite** usando el `Microsoft.Data.Sqlite` ya
  previsto en el `.csproj` y el esquema de `schema.sql`.
- **Async/await** real en operaciones de pago y carga de imágenes.
- Notificaciones "en vivo" y actualización de vistas sin recargar toda la pantalla.
- Tests automatizados de los servicios (rangos de recarga, compras, roles).
- Empaquetado como instalador (MSIX/ClickOnce).

---

<p align="center"><sub>NovaCore · Versión 3 · Proyecto académico — clon educativo de Steam (WinForms / .NET 8)</sub></p>
