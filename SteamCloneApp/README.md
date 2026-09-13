# SteamCloneApp

Clon funcional de Steam en **C# / .NET 8 WinForms**, rediseñado para verse y
comportarse como la **app móvil de Steam** dentro de una ventana de escritorio
de tamaño fijo (estilo teléfono: 400x820), con navegación tipo SPA (un solo
`MainForm` que intercambia `UserControl`s, sin abrir ventanas nuevas).

## Cómo abrirlo

1. Requiere **Visual Studio 2022** (17.8+) con el workload **.NET desktop development**.
2. Abre la carpeta `SteamCloneApp` en Visual Studio o agrega `SteamCloneApp.csproj`
   como proyecto existente a una solución.
3. Compila y ejecuta (F5). No requiere base de datos: los datos viven en
   memoria (`Services/DataStore.cs`) y se recargan cada vez que abres la app.

### Credenciales de prueba
| Usuario  | Contraseña  | Rol            |
|----------|-------------|----------------|
| `admin`  | `admin123`  | Administrador  |

Cualquier otra cuenta se crea registrándose desde la propia app (botón
"Crear cuenta nueva" en el login) — no existe un usuario común pre-cargado,
tal como en Steam real.

## Estructura de la interfaz (estilo app móvil)

```
┌─────────────────────────────┐
│ 🎮 STEAMCLONE   🔍  ⋮  🛒 👤 │  ← Header: buscador, kebab, carrito, avatar
│ MENU▾   WISHLIST   WALLET    │  ← Tabs
├─────────────────────────────┤
│                              │
│      (Contenido dinámico)    │  ← StoreView / GameDetailView / etc.
│                              │
├─────────────────────────────┤
│  🏷️    📰    📚    🔔    ☰   │  ← Bottom nav: Store/News/Library/Notif/Menu
└─────────────────────────────┘
```

- El ícono central de la barra inferior (que en Steam real es "Steam Guard")
  se reemplazó por **Biblioteca**, ya que tiene más sentido funcional aquí.
- Los 5 íconos de abajo son completamente funcionales y navegan a una página
  real (no son solo decorativos).
- El botón **☰ / ⋮ / "MENU ▾"** abre `MenuPageView`, la pantalla de menú
  (equivalente al desplegable de 3 barras de Steam), que también lista los
  paneles de administración cuando el usuario activo es Administrador.
- El avatar del header navega al Perfil; se puede cambiar la foto tocándolo
  desde dentro del Perfil (usa un `OpenFileDialog`, no hace falta configurarlo
  al registrarse).

## Estructura de carpetas

```
SteamCloneApp/
├── SteamCloneApp.csproj
├── Program.cs                  → Punto de entrada
├── MainForm.cs                 → Shell tipo celular: header + tabs + bottom nav
├── Database/
│   └── schema.sql              → Esquema SQL de referencia (incluye Reportes y Carrito)
├── Models/
│   ├── Usuario.cs               → Incluye AvatarPath y lista de Amigos
│   ├── Juego.cs                 → Ampliado: Publisher, Tags, Reviews, Requisitos min/rec, Contenido maduro
│   ├── Categoria.cs
│   ├── BibliotecaItem.cs
│   ├── Transaccion.cs
│   ├── ItemCarrito.cs           → NUEVO: para el flujo Add to Cart → Checkout
│   └── Reporte.cs               → NUEVO: reportes de juegos y usuarios
├── Services/
│   ├── DataStore.cs             → En memoria; solo siembra la cuenta admin
│   ├── SessionManager.cs
│   ├── UsuarioService.cs        → + avatar, amigos, listar usuarios para reportar
│   ├── JuegoService.cs          → CRUD ampliado con los nuevos campos
│   ├── BibliotecaService.cs     → Evita comprar el mismo juego dos veces (PoseeJuego)
│   ├── TransaccionService.cs
│   ├── CarritoService.cs        → NUEVO: agregar/quitar del carrito y checkout
│   └── ReporteService.cs        → NUEVO: reportar y moderar juegos/usuarios
└── Views/
    ├── UiTheme.cs
    ├── LoginView.cs / RegisterView.cs
    ├── StoreView.cs             → Banner + "Featured & Recommended" (estilo imagen 1)
    ├── GameDetailView.cs        → Ficha completa: tags, reviews, buy box, requisitos (imágenes 2-4)
    ├── CartView.cs              → NUEVO: carrito y checkout funcional
    ├── WalletView.cs            → NUEVO: saldo + recarga de fondos
    ├── WishlistView.cs          → NUEVO: lista de deseos (placeholder visual)
    ├── NewsView.cs              → NUEVO: noticias de ejemplo
    ├── NotificationsView.cs     → NUEVO: notificaciones de compras recientes
    ├── MenuPageView.cs          → NUEVO: pantalla de menú (imagen 5) + accesos de admin
    ├── LibraryView.cs           → Biblioteca / inventario + simulación de "Jugar"
    ├── ProfileView.cs           → Perfil estilo imagen 6: avatar editable, stats, amigos, reportar usuario
    ├── AdminDashboardView.cs    → CRUD de juegos
    ├── GameFormView.cs          → Alta/edición con todos los campos nuevos
    ├── UserManagementView.cs    → Roles y baneo de usuarios
    ├── AdminReportsView.cs      → NUEVO: moderación de reportes (juegos y usuarios)
    └── ReportsView.cs           → Reportes de ventas / ingresos
```

## Funcionalidades nuevas de esta versión

- **Registro real de usuarios**: ya no hay un "Usuario común" de ejemplo;
  cada persona que se registra se convierte en una cuenta `Usuario` normal.
  El rol `Administrador` solo existe en la cuenta `admin` sembrada (aunque un
  admin puede ascender a otros usuarios desde "Gestión de Usuarios").
- **Foto de perfil**: se elige desde dentro de la app (Perfil → tocar avatar),
  no en el formulario de registro, usando `OpenFileDialog`.
- **Carrito de compras real**: "Add to Cart" en la ficha del juego agrega el
  ítem al carrito (`CarritoService`); el ícono 🛒 del header muestra la
  cantidad y lleva a `CartView`, donde se confirma la compra (se descuenta el
  saldo y el juego pasa a la Biblioteca).
- **No se puede comprar el mismo juego dos veces**: `BibliotecaService.PoseeJuego`
  se valida tanto al agregar al carrito como al hacer checkout.
- **Los juegos comprados quedan en tu inventario**: la Biblioteca (`LibraryView`)
  es el inventario del usuario; también accesible desde Perfil → "Inventory".
- **Reportes de juegos y usuarios**: botón "🚩 Reportar este juego" en la
  ficha del juego, y "Reportar un usuario" en el Perfil. Los administradores
  los revisan en el nuevo panel **Moderación** (`AdminReportsView`), pudiendo
  marcarlos como revisados o banear directamente al usuario reportado.
- **Wallet funcional**: la tab "WALLET" del header abre `WalletView`, donde se
  puede ver el saldo y recargar montos predefinidos.

## Arquitectura y flujo de navegación (SPA)

- `MainForm.MostrarVista(UserControl vista)` sigue siendo el único punto de
  navegación: limpia el panel de contenido y agrega la vista nueva. Ninguna
  vista abre `Form`s nuevos (salvo pequeños diálogos modales para reportar,
  que son intencionalmente ventanas emergentes de confirmación).
- El menú de administración ya no vive en un sidebar fijo (se quitó al pasar
  al diseño de teléfono); ahora los accesos de Administrador aparecen dentro
  de `MenuPageView`, condicionados a `SessionManager.EsAdministrador`.
- Como segunda capa de seguridad, `AdminDashboardView`, `UserManagementView`,
  `AdminReportsView` y `ReportsView` también verifican el rol en su propio
  constructor.

## Migrar de memoria a una base de datos real

`Database/schema.sql` ya incluye las tablas nuevas (`CarritoItems`, `Reportes`,
`Amigos`) además del esquema original. Para conectar una base real, instala
`Microsoft.Data.Sqlite` (la línea ya está preparada, comentada, en el
`.csproj`) y reemplaza la implementación interna de `DataStore` y de cada
`*Service` — las Vistas no cambian, porque solo dependen de los métodos
públicos de los Services.

## Pendientes / ideas para seguir iterando

- Hashing real de contraseñas (BCrypt) en vez de texto plano.
- Persistencia real (SQLite) usando el esquema incluido.
- Descarga real de juegos (actualmente "Ir a mi biblioteca" simula la
  posesión del juego; "Jugar" simula horas jugadas con un timer).
- Completar los ítems marcados como "próximamente" en el menú (Community,
  Chat, Settings) con funcionalidad real si el proyecto lo requiere.
