# NovaCore — Clon de Steam (WinForms .NET 8)

> Manual completo del sistema y bitácora de creación.

NovaCore (antes **SteamCloneApp**) es un clon funcional de **Steam** escrito en
**C# con Windows Forms sobre .NET 8**. Rediseñado progresivamente hasta
parecerse a la **app móvil de Steam** dentro de una ventana de escritorio de
tamaño fijo (proporción teléfono 400&times;820), con navegación tipo SPA (un
solo `MainForm` que intercambia `UserControl`s sin abrir ventanas nuevas),
tema visual oscuro "nova" y la interfaz completa traducida al español.

No requiere base de datos externa: los datos viven en memoria
(`Services/DataStore.cs`) y **se persisten en archivos JSON** en
`%LOCALAPPDATA%\NovaCore`.

---

## Tabla de contenidos

1. [Requisitos y cómo ejecutarlo](#requisitos-y-cómo-ejecutarlo)
2. [Créditos / cuentas de prueba](#credenciales-de-prueba)
3. [Manual del usuario](#manual-del-usuario)
   - [Pantallas principales](#pantallas-principales)
   - [Flujo de compra](#flujo-de-compra)
   - [Perfil, amigos y notificaciones](#perfil-amigos-y-notificaciones)
   - [Panel de administración](#panel-de-administración)
4. [Arquitectura del sistema](#arquitectura-del-sistema)
5. [Estructura de carpetas](#estructura-de-carpetas)
6. [Modelos de datos](#modelos-de-datos)
7. [Servicios de negocio](#servicios-de-negocio)
8. [Persistencia en disco](#persistencia-en-disco)
9. [Tema visual y escalado responsivo](#tema-visual-y-escalado-responsivo)
10. [Esquema SQL de referencia](#esquema-sql-de-referencia)
11. [Historial de creación (bitácora)](#historial-de-creación-bitácora)
12. [Manual de creación paso a paso](#manual-de-creación-paso-a-paso)
13. [Pendientes / mejoras futuras](#pendientes--mejoras-futuras)

---

## Requisitos y cómo ejecutarlo

1. [.NET SDK 8.0+](https://dotnet.microsoft.com/download) (probado también con
   .NET 10 SDK) o **Visual Studio 2022 (17.8+)** con el workload
   **.NET desktop development**.
2. Desde la carpeta del proyecto:

   ```bash
   dotnet run --project NovaCore/NovaCore.csproj
   ```

   O abre la carpeta `NovaCore` en Visual Studio y presiona **F5**.

3. La aplicación no necesita instalación ni base de datos. Al cerrarla, los
   datos se guardan automáticamente en disco (ver
   [Persistencia](#persistencia-en-disco)).

### Variables de entorno

| Variable           | Efecto                                              |
|--------------------|-----------------------------------------------------|
| `NOVACORE_DATADIR` | Redirige la carpeta de datos (por defecto `%LOCALAPPDATA%\NovaCore`). Útil para pruebas sin tocar datos reales. |

---

## Credenciales de prueba

La app **solo siembra la cuenta de Administrador**. El resto de las cuentas se
crean registrándose desde la propia app (botón **"Crear cuenta nueva"** en el
login), igual que en Steam real. En la raíz del repositorio existe `clone.txt`
con cuentas de prueba usadas durante el desarrollo:

| Usuario  | Contraseña      | Rol           | Nota                                   |
|----------|-----------------|---------------|----------------------------------------|
| `admin`  | `admin123`      | Administrador | Siempre existe (sembrada por `DataStore`). |
| `rokoko` | `12345`         | Usuario       | Solo existe si se registró localmente. |
| `sushi`  | `spiderman123`  | Usuario       | Solo existe si se registró localmente. |

> Al registrarse se otorga un **saldo de bienvenida de $50 USD** (Wallet) para
> poder probar el flujo de compra.

---

## Manual del usuario

### Estructura de la interfaz (estilo app móvil)

```
┌────────────────────────────────────────┐
│ ✦ NOVACORE        🔍 ⋮  🛒  👤         │  ← Header: logo, búsqueda, categorías, carrito, avatar
│ MENÚ ▾   DESEOS   BILLETERA            │  ← Tabs
├────────────────────────────────────────┤
│                                        │
│     (Contenido dinámico — UserControl) │  ← Store / GameDetail / Cart / Perfil / etc.
│                                        │
├────────────────────────────────────────┤
│  🏷️    📰    📚    🔔    ☰            │  ← Bottom nav: Tienda / Noticias / Biblioteca / Notif / Menú
└────────────────────────────────────────┘
```

- **Header:** logo NOVACORE, botón 🔍 (búsqueda), **MENÚ ▾** (menú desplegable
  de categorías y recomendaciones), 🛒 (carrito, con badge de cantidad) y 👤
  (avatar → perfil). Fila 2: pestañas **MENÚ ▾ / DESEOS / BILLETERA**.
- **Bottom nav:** los 5 botones navegan a páginas reales. El ☰ abre el menú
  principal y actúa como *toggle*: si se presiona de nuevo, regresa
  exactamente a la vista anterior.
- La barra inferior y el header **se ocultan** en pantallas secundarias
  (búsqueda, carrito, perfil, detalle de juego, paneles de administración).

### Pantallas principales

| Pantalla          | Vista                          | Qué permite                                       |
|-------------------|--------------------------------|---------------------------------------------------|
| Login / Registro  | `LoginView` / `RegisterView`   | Iniciar sesión y crear cuenta con validación en vivo; el registro hashea la contraseña (PBKDF2) y, al completarse, redirige al login. |
| Tienda            | `StoreView`                    | Banner destacado + cards "Destacados y recomendados". |
| Detalle de juego  | `GameDetailView`               | Ficha completa: descripción, desarrollador/distribuidora, etiquetas, reseñas simuladas, caja de compra, wishlist, requisitos min/rec, contenido maduro y botón "Reportar este juego". |
| Búsqueda          | `SearchView`                   | Filtra el catálogo en tiempo real; registra términos para las recomendaciones. |
| Recomendaciones   | `RecomendacionesView`          | Sugerencias personalizadas por categorías poseídas + búsquedas recientes (o los más populares si aún no hay datos). |
| Carrito           | `CartView`                     | Lista de juegos a comprar, quitar ítems, ver total/saldo y confirmar compra (checkout). |
| Billetera         | `WalletView`                   | Saldo actual, recarga por montos ($10/$25/$50/$100) o **monto personalizado** ($5.00–$500.00), y una **pasarela de pago simulada** (overlay) con Tarjeta validada en tiempo real / código de Tarjeta Regalo / PayPal, pantalla de **confirmación**, indicador **"Procesando pago..."** y **notificación** al acreditar. Incluye **historial de movimientos** (recargas en verde, compras en naranja). |
| Lista de deseos   | `WishlistView`                 | Juegos guardados; permite pasarlos al carrito o quitarlos. |
| Noticias          | `NewsView`                     | Noticias de ejemplo (página navegable). |
| Notificaciones    | `NotificationsView`            | Avisos de amistad y compras; aceptar/rechazar solicitudes; borrar individual o todas. |
| Biblioteca        | `LibraryView`                  | Inventario del usuario, detalle del juego y simulación de "Jugar" con horas acumuladas. |
| Perfil            | `ProfileView`                  | Avatar editable, stats (Juegos/Amigos/Billetera), amigos, "MI CONTENIDO", reportar usuario, detalles de cuenta y eliminación de cuenta. |
| Menú              | `MenuPageView`                 | Accesos a todas las secciones + paneles de administración (solo admin). |

### Flujo de compra

1. En la Tienda (o búsqueda) toca un juego → `GameDetailView`.
2. Pulsa **"Añadir al carrito"** (se valida que no lo tengas ya en la
   biblioteca y que no esté repetido).
3. El 🛒 del header muestra la cantidad. Entra a él (o a la pestaña DESEOS y
   desde ahí "Al carrito").
4. Pulsa **"Confirmar compra"**: se descuenta el saldo de la billetera, se
   registra la transacción, se genera una notificación de compra y el juego se
   agrega a tu Biblioteca.
5. En la Biblioteca puedes **▶ Jugar** (simulación): un timer acumula horas
   jugadas y las persiste (se ve el indicador verde "● En ejecución").

> No se puede comprar el mismo juego dos veces: `BibliotecaService.PoseeJuego`
> se valida tanto al agregar al carrito como en el checkout.

### Perfil, amigos y notificaciones

- **Avatar:** tócalo dentro del Perfil y elige una imagen con `OpenFileDialog`.
  Se copia a `%LOCALAPPDATA%\NovaCore\Avatares` y se muestra también en el
  header.
- **Amigos:** botón **"👤➕ Agregar amigos"** → busca por nombre de usuario →
  envía una solicitud. Llega como notificación al otro usuario, que puede
  **✓ Aceptar** o **✕ Rechazar**. Al aceptar se agregan mutuamente. En
  "MI CONTENIDO → Amigos" puedes verlos, reportarlos o eliminarlos.
- **Notificaciones:** se persisten y **no desaparecen solas**; solo se borran
  si el usuario las elimina (individual o "🗑 Borrar todas"). El 🔔 de la barra
  inferior muestra el número de no leídas.

### Panel de administración

Accesible con la cuenta `admin` (o cualquier usuario que un admin haya
ascendido): **Menú → sección "— ADMINISTRACIÓN —"**.

| Panel               | Vista                | Qué permite                                            |
|---------------------|----------------------|--------------------------------------------------------|
| Gestión de Juegos   | `AdminDashboardView` | CRUD del catálogo (+ `GameFormView` con portada, requisitos, reviews, contenido maduro, etc.). Eliminación lógica (se oculta del catálogo). |
| Gestión de Usuarios | `UserManagementView` | Cambiar rol y banear/reactivar cuentas (no a sí mismo). |
| Reportes de Ventas  | `ReportsView`        | Ingresos totales, compras realizadas y ranking de juegos más vendidos con barras. |
| Moderación          | `AdminReportsView`   | Reportes de juegos/usuarios pendientes: marcar como revisado o banear al reportado. |

> Motivos de reporte: cualquier usuario puede reportar un **juego** (desde su
> ficha) o a otro **usuario** (desde el Perfil o desde la lista de amigos).
> Los administradores los gestionan en Moderación (con contador de pendientes
> en el menú).

---

## Arquitectura del sistema

- **Patrón:** WinForms, una sola ventana (`MainForm`) con paneles. Cada
  pantalla es un `UserControl` que se inyecta en el `ContentPanel`.
- **Navegación tipo SPA:** `MainForm.MostrarVista(UserControl)` es el único
  punto de navegación. Hace una **transición animada** (slide-up con ease-out
  cúbico, ~240 ms) y captura la vista saliente como imagen para un efecto de
  desplazamiento sin parpadeos (el panel de contenido es *double-buffered*).
- **Roles por sesión:** `SessionManager` guarda el usuario activo en memoria
  y expone `EsAdministrador`. El menú muestra los accesos de admin según el
  rol y, como **segunda barrera**, cada vista de administración verifica el
  rol en su propio constructor.
- **Desacople vista–datos:** las vistas solo dependen de los métodos públicos
  de los `*Service`; `DataStore` (fuente en memoria) y `PersistenciaService`
  (capa de disco) pueden reemplazarse por una base de datos real **sin tocar
  las vistas**.
- **Escalado responsivo:** la UI se diseña a 400&times;820 y se re-escala de
  forma uniforme (factor 0.55 &ndash; 2.5) según el tamaño de la ventana/pantalla,
  ajustando tamaños, posiciones y fuentes (`UiTheme.EscalaUI` permite que los
  textos dibujados a mano también se adapten).

---

## Estructura de carpetas

```
SteamCloneApp/
├── clone.txt                      → Cuentas de prueba usadas durante el desarrollo
├── Imagenes/                      → Portadas de ejemplo (ELDEN RING.jpg, WARDOGS.jpg, logo.png)
└── NovaCore/
    ├── NovaCore.csproj            → .NET 8 WinForms (WinExe, net8.0-windows)
    ├── Program.cs                 → Punto de entrada ([STAThread] Main)
    ├── MainForm.cs                → Shell tipo celular: header + tabs + bottom nav + animaciones + escala
    ├── Assets/logo.png            → Logo del header e ícono de ventana (si falta, se dibuja una nova vectorial)
    ├── Database/schema.sql        → Esquema SQL de referencia (SQL Server / SQLite)
    ├── Models/
    │   ├── Usuario.cs             → + RolUsuario, AvatarPath, Saldo, Activo, Amigos, FechaRegistro
    │   ├── Juego.cs               → Publisher, Tags, Reviews, Requisitos min/rec, Contenido maduro, ImagenPortada, Activo
    │   ├── Categoria.cs
    │   ├── BibliotecaItem.cs      → HorasJugadas, UltimaSesion (EnEjecucion no se persiste)
    │   ├── Transaccion.cs         → EstadoTransaccion (Completada/Fallida/Reembolsada)
    │   ├── ItemCarrito.cs         → JuegoId, Titulo, Precio (pendiente de pago)
    │   ├── ItemDeseo.cs           → Wishlist (usuario + juego + fecha)
    │   ├── Reporte.cs             → TipoReporte (Juego/Usuario) + EstadoReporte (Pendiente/Revisado)
    │   ├── Notificacion.cs        → TipoNotificacion + SolicitudId opcional + Leida
    │   └── SolicitudAmistad.cs    → EstadoSolicitud (Pendiente/Aceptada/Rechazada)
    ├── Services/
    │   ├── DataStore.cs             → Singleton en memoria; semilla (admin, 5 categorías, 25 juegos reales) + carga JSON + migración de catálogo por versión
    │   ├── PersistenciaService.cs   → Archivos JSON en %LOCALAPPDATA%\NovaCore (usuarios, juegos, biblioteca, solicitudes, deseos, notificaciones, transacciones) + Avatares/ y Portadas/ + catalogo.version
    │   ├── SessionManager.cs        → Usuario activo + EsAdministrador
    │   ├── UsuarioService.cs        → Login, registro con validación, roles, baneo, saldo, avatar, amigos/solicitudes, eliminar cuenta
    │   ├── PasswordHasher.cs        → Hash/verificación de contraseñas (PBKDF2-SHA256, sal e iteraciones)
    │   ├── JuegoService.cs          → CRUD + categorías + PrepararImagenPortada + persistencia del catálogo
    │   ├── BibliotecaService.cs     → PoseeJuego, AgregarJuego, RegistrarSesionJugada
    │   ├── TransaccionService.cs    → ProcesarCompra + datos de ventas/ranking
    │   ├── CarritoService.cs        → Carrito por usuario (en memoria) + checkout
    │   ├── DeseoService.cs          → Wishlist con persistencia
    │   ├── NotificacionService.cs   → Crear/leer/marcar leídas/eliminar (persiste)
    │   ├── RecomendacionService.cs  → Puntaje por categorías poseídas + búsquedas recientes
    │   └── ReporteService.cs        → Reportar juego/usuario + moderar
    └── Views/ (20 vistas, una por pantalla)
        ├── UiTheme.cs                → Paleta "nova" + helpers (botones, back bar, grid, imágenes)
        ├── LoginView.cs / RegisterView.cs
        ├── StoreView.cs              → Banner + "Destacados y recomendados"
        ├── GameDetailView.cs         → Ficha tipo Steam (buy box, wishes, reportar)
        ├── SearchView.cs             → Búsqueda en tiempo real
        ├── RecomendacionesView.cs    → Sugerencias personalizadas
        ├── CartView.cs / WalletView.cs / WishlistView.cs
        ├── NewsView.cs / NotificationsView.cs
        ├── MenuPageView.cs / LibraryView.cs / ProfileView.cs
        ├── AdminDashboardView.cs / GameFormView.cs
        ├── UserManagementView.cs / ReportsView.cs / AdminReportsView.cs
```

---

## Modelos de datos

- **Usuario:** `UsuarioId`, `NombreUsuario`, `Correo`, `PasswordHash`
  (PBKDF2-SHA256 con sal, ver [Servicios](#servicios-de-negocio)), `Rol`
  (`Usuario`/`Administrador`), `Saldo`, `Activo` (ban/falso), `AvatarPath`,
  `FechaRegistro` y `Amigos` (nombres de usuario).
- **Juego:** `Titulo`, `Descripcion`, `Precio`, `CategoriaId/Nombre`,
  `Desarrollador`, `Publisher`, `Tags` (coma separada → `TagsLista`), review
  simuladas (`ReviewsResumen`, `ReviewsPorcentaje`, `ReviewsCantidad`),
  `RequisitosMinimos`/`RequisitosRecomendados`, `TieneContenidoMaduro` +
  `DescripcionContenidoMaduro`, `ImagenPortada`, `FechaPublicacion`, `Activo`.
- **Relaciones clave:** `BibliotecaItem` (usuario+juego, horas, última sesión),
  `Transaccion`, `ItemCarrito`, `ItemDeseo`, `Reporte`, `Notificacion`,
  `SolicitudAmistad`.

---

## Servicios de negocio

| Servicio                | Responsabilidad |
|-------------------------|-----------------|
| `SessionManager`        | Estado de la sesión activa. |
| `UsuarioService`        | Login (verifica con `PasswordHasher` y **migra** a hash las cuentas antiguas en texto plano; rechaza cuentas inactivas), registro con reglas (usuario 3–20, solo letras/números/guion bajo; `admin` reservado; correo válido; contraseña 4–64 con letras y números) y **hasheo PBKDF2 de la contraseña antes de guardar**, cambio de rol, baneo/reactivación, **recarga de saldo** (valida el rango $5–$500, persiste el nuevo saldo y registra el movimiento + notificación), avatar, búsqueda de usuarios/amigos, enviar/aceptar/rechazar solicitudes, eliminar cuenta (limpia biblioteca, transacciones, carrito, solicitudes, deseos y notificaciones). |
| `PasswordHasher`        | Hashea (`Hash`) y verifica (`Verificar`) contraseñas con **PBKDF2 / Rfc2898DeriveBytes (SHA-256)**, 100 000 iteraciones y sal aleatoria de 16 bytes por usuario. Formato `PBKDF2$iteraciones$salt$hash`; usa `CryptographicOperations.FixedTimeEquals` para comparar. Sin paquetes externos. |
| `JuegoService`          | Catálogo con filtros (texto + categoría), CRUD y copia de portadas a disco. |
| `BibliotecaService`     | Posesión, alta de juegos y registro de sesiones jugadas. |
| `TransaccionService`    | Procesa compras (valida posesión y saldo), registra **recargas de billetera** (con notificación), y produce ingresos/ventas/ranking (solo compras) y movimientos/recargas por usuario. |
| `CarritoService`        | Carrito por usuario (en memoria) + checkout en lote. |
| `DeseoService`          | Wishlist persistente. |
| `NotificacionService`   | Notificaciones persistentes con badge de no leídas. |
| `RecomendacionService`  | Registra búsquedas (máx. 10) y puntúa el catálogo (peso por categoría poseída + coincidencia con búsquedas + popularidad). |
| `ReporteService`        | Reportes contra juegos/usuarios y moderación. |

---

## Persistencia en disco

`PersistenciaService` serializa listas a **JSON** en
`%LOCALAPPDATA%\NovaCore` (o la carpeta de `NOVACORE_DATADIR`):

```
NovaCore/                   ← carpeta de datos
├── usuarios.json           ← cuentas, saldo, avatar, amigos, rol
├── juegos.json             ← catálogo (altas/ediciones/bajas del admin, con su portada)
├── catalogo.version        ← versión del catálogo sembrado (controla la migración)
├── biblioteca.json         ← juegos adquiridos, horas jugadas, última sesión
├── solicitudes.json        ← solicitudes de amistad y su estado
├── deseos.json             ← listas de deseos
├── notificaciones.json     ← notificaciones (leídas/no leídas)
├── transacciones.json      ← historial de movimientos (compras y recargas)
├── Avatares/               ← fotos de perfil copiadas desde la app
└── Portadas/               ← portadas de juegos elegidas por el admin
```

- `DataStore` carga estos archivos al arranque y descarta entradas huérfanas
  (usuarios/juegos que ya no existen). Si `usuarios.json` no existe o no tiene
  ningún Administrador, se garantiza la cuenta `admin`.
- **Versión de catálogo (`catalogo.version`):** `DataStore` guarda la versión
  del catálogo que sembró. Si el `juegos.json` guardado es de una versión
  anterior (por ejemplo, juegos ficticios de prueba), se **descarta**, se limpia
  la biblioteca de títulos obsoletos y se regenera con la semilla real actual.
  Basta con subir `VersionCatalogoActual` en `DataStore` para forzar la
  actualización en instalaciones existentes.
- El **catálogo de juegos** (con la ruta de su portada en `Portadas/`) se guarda
  al crear, editar o dar de baja un juego, por lo que las portadas se conservan
  entre reinicios hasta que el administrador las cambie. Al reemplazar una
  portada se borra del disco la anterior (solo si vive dentro de `Portadas/`).
- El **carrito no se persiste** (se pierde al cerrar la app) y las
  **búsquedas recientes** tampoco: están pensadas solo para la sesión actual.
- Todos los métodos de escritura están en `try/catch`: si no se puede escribir,
  la app sigue funcionando en memoria.

---

## Tema visual y escalado responsivo

- **Tema "nova"** (`Views/UiTheme.cs`): paleta negra galáctica con matiz
  violeta (`#08070D`), texto blanco cálido y acento **naranja incandescente**
  (`#FF7A1A`) con resplandor dorado, verde esmeralda para éxito y rojo para
  peligro.
- El logo se carga desde `Assets\logo.png` (junto al ejecutable). Si no
  existe, se dibuja una **nova vectorial** (halo radial + estrella de 4 puntas).
- `UiTheme.EscalaUI` se actualiza con el factor de escala para que los textos
  dibujados con `Graphics.DrawString` (banners, portadas placeholder) también
  se adapten al tamaño de ventana.

---

## Esquema SQL de referencia

`Database/schema.sql` es un esquema **compatible con SQL Server/SQLite** que
refleja el modelo actual:

`Categorias`, `Usuarios`, `Amigos`, `Juegos`, `Biblioteca`
(`UNIQUE(UsuarioId, JuegoId)` para evitar comprar dos veces), `Transacciones`,
`CarritoItems` y `Reportes`, más los datos semilla: 5 categorías, la cuenta
`admin` y **25 videojuegos reales** (5 por categoría) con sinopsis, precio USD,
desarrollador/publisher, tags, reviews aproximadas de Steam, requisitos de PC
completos, contenido maduro y fecha de lanzamiento.

Sirve como guía si se quiere migrar la persistencia JSON a una base real.
Para ello: agrega `Microsoft.Data.Sqlite` (línea comentada en el `.csproj`) y
reemplaza la implementación interna de `DataStore` y de los `*Service` — las
**vistas no cambian**.

---

## Historial de creación (bitácora)

El proyecto se construyó en **etapas** (una por commit relevante). Cada etapa
representó un cambio importante de diseño y alcance:

### Etapa 1 — `089dbbd` (12 sep 2026) · *"Inicial: clon de Steam (WinForms .NET 8) con persistencia local de usuarios"*

Nace el proyecto como **SteamCloneApp**: aplicación de escritorio WinForms
.NET 8 con la mayoría de las funciones básicas ya presentes:

- Login + registro de usuarios con saldo de bienvenida y **persistencia de
  usuarios en JSON** (`PersistenciaService`, 61 líneas).
- Tienda, ficha de juego, carrito con checkout, billetera, lista de deseos,
  biblioteca, perfil y **4 paneles de administración** (juegos CRUD, usuarios,
  reportes de ventas, moderación).
- Esquema SQL completo (`schema.sql`) y README de 149 líneas.

Arquitectura ya orientada a servicios (`Services/`), `DataStore` singleton y
vistas desacopladas.

### Etapa 2 — `54d4c8a` (12 sep 2026) · *"Agregar eliminación de cuenta para usuarios y filtros de validación en el registro"*

Capa de seguridad y UX en cuentas:

- **Eliminación de cuenta** (`UsuarioService.EliminarCuenta`): solo para
  usuarios no administradores; limpia biblioteca, transacciones, carrito,
  solicitudes, deseos y notificaciones, y persiste todos los cambios.
- **Filtros de validación en el registro** (`RegisterView`): sanado en vivo del
  campo de usuario (solo letras/números/`_`, máx. 20), confirmación de
  contraseña, resaltado rojo de campos inválidos antes de enviar, y mensajes de
  error específicos para cada regla.

### Etapa 3 — `30b0902` (17 sep 2026) · *"Renombrar a NovaCore con tema nova, portadas de juegos, escalado y traducción UI al español"*

**La gran transformación**: el clon de escritorio se convierte en
**NovaCore**, con identidad propia y rediseño estilo **app móvil de Steam**.

- **Rebranding:** carpeta renombrada de `SteamCloneApp` a `NovaCore`, UI
  traducida al español, tema "nova" (negro galáctico + naranja incandescente),
  logo propio en `Assets/` con fallback vectorial.
- **Formato móvil:** ventana de proporción teléfono 400&times;820 con
  **escalado responsivo** (0.55–2.5) y **animación de transición** entre
  vistas (slide-up ease-out, sin parpadeos).
- **Portadas de juegos:** nuevo campo `ImagenPortada`, botón "Elegir imagen…"
  en el formulario de juegos, copia a `%LOCALAPPDATA%\NovaCore\Portadas` y
  render "cover" con degradado nova como respaldo (tienda, banner, búsqueda y
  ficha).
- **Sistema de amigos:** nuevo modelo `SolicitudAmistad`, búsqueda de
  usuarios, enviar/aceptar/rechazar solicitudes con notificaciones, lista de
  amigos en el Perfil y reporte de amigos.
- **Notificaciones persistentes:** nuevo modelo `Notificacion` +
  `NotificacionService`; ya no se calculan al vuelo, se guardan en disco y solo
  se borran explícitamente (badge de no leídas en el 🔔).
- **Wishlist funcional y persistente:** nuevo `ItemDeseo` + `DeseoService`;
  agregar/quitar desde la ficha del juego y mover al carrito.
- **Búsqueda y recomendaciones:** `SearchView` rediseñada (filtra en tiempo
  real y registra términos) + nueva `RecomendacionService`/`RecomendacionesView`
  con puntaje por categorías poseídas y búsquedas.
- **Persistencia ampliada:** nueva capa de datos para biblioteca (con horas
  jugadas), solicitudes, deseos, notificaciones y transacciones; avatares en
  `Avatares/`.
- **Biblioteca "jugable":** simulación de "Jugar" con timer que acumula horas y
  las persiste.
- **Mayor seguridad de acceso:** los paneles de admin se movieron al menú
  (condicionados al rol) y cada vista verifica el rol en su constructor.

### Etapa 4 — *"Persistir el catálogo de juegos y sus portadas"*

**Notificación de cambio:** se cierra un hueco de persistencia. Antes, el
catálogo solo vivía en memoria: al crear o editar un juego en modo
administrador, la portada se copiaba a `Portadas/`, pero al cerrar la app el
catálogo volvía al *seed* y se perdía la referencia a la imagen. Ahora el
catálogo completo se guarda en disco, por lo que **la portada elegida se
conserva entre reinicios, la ven todos los usuarios y solo cambia cuando el
administrador edita o modifica el juego**.

- **Nuevo archivo `juegos.json`:** `PersistenciaService.GuardarJuegos` /
  `CargarJuegos` serializan el catálogo (con la ruta de su portada).
- **`DataStore`** carga el catálogo persistido al arranque, **antes** de la
  biblioteca (que valida contra él); si el archivo no existe, se mantiene el
  *seed*.
- **`JuegoService`** persiste en `Crear`, `Actualizar` y `Eliminar` (baja
  lógica), de modo que altas, ediciones y bajas sobreviven al reinicio.
- **Portadas:** la imagen ya se copiaba a `%LOCALAPPDATA%\NovaCore\Portadas`;
  ahora su referencia también persiste. Al reemplazar una portada se borra del
  disco la anterior, y **solo** si vive dentro de `Portadas/` (nunca la imagen
  original del usuario).
- **`Juego.TagsLista`** se marcó con `[JsonIgnore]` para no persistir el campo
  derivado en el JSON.

### Etapa 5 — *"Enlazar registro con login y hashear contraseñas (PBKDF2)"*

Se completa el flujo de alta de usuarios y se cierra el pendiente de seguridad
más importante:

- **Navegación Registro → Login:** el botón **"Volver a inicio de sesión"**
  cierra la vista de registro y abre el login (`MainForm.MostrarLogin()`).
- **Validación reforzada en `RegisterView`:** antes de enviar se comprueba que
  ningún campo esté vacío, que el usuario tenga 3–20 caracteres válidos, que el
  correo tenga formato correcto y que **contraseña y confirmación coincidan
  exactamente**; cada fallo muestra un mensaje claro en pantalla.
- **Contraseñas hasheadas:** nuevo `Services/PasswordHasher.cs` con **PBKDF2
  (Rfc2898DeriveBytes / SHA-256, 100 000 iteraciones, sal aleatoria de 16 bytes)**,
  sin dependencias externas. `UsuarioService.Registrar` guarda el hash y
  `Login` verifica con comparación de tiempo constante.
- **Migración automática:** las cuentas antiguas guardadas en texto plano se
  re-hashean en el primer login exitoso; las cuentas `admin` sembradas por
  `DataStore` ya se crean hasheadas.
- **Redirección tras el registro:** al crear la cuenta con éxito se muestra la
  confirmación y, ~1.2 s después, se abre el login para ingresar con las nuevas
  credenciales.

### Etapa 6 — *"Catálogo de 25 videojuegos reales + migración por versión"*

El catálogo de prueba (WARDOGS, Cyber Rift, Voidwalker Horizons…) se reemplaza
por **25 videojuegos reales, 5 por cada categoría**, con datos auténticos de
Steam:

- **Acción:** Cyberpunk 2077, Resident Evil 4, DOOM Eternal, GTA V, Devil May
  Cry 5. **RPG:** Elden Ring, The Witcher 3, Baldur's Gate 3, Persona 5 Royal,
  Monster Hunter: World. **Estrategia:** Age of Empires IV, Civilization VI,
  Hearts of Iron IV, StarCraft II, Cities: Skylines. **Indie:** Hollow Knight,
  Celeste, Hades, Stardew Valley, Undertale. **Aventura:** Red Dead Redemption
  2, God of War, Rise of the Tomb Raider, Stray, Uncharted: Legacy of Thieves
  Collection.
- **Datos completos por juego:** sinopsis oficial, precio en USD, desarrollador
  y publisher reales, `FechaPublicacion`, tags, reviews aproximadas de Steam
  (resumen, % y cantidad) y requisitos mínimos/recomendados de PC.
- **Migración por versión:** `PersistenciaService` guarda `catalogo.version`;
  si el `juegos.json` guardado es de una versión anterior, `DataStore` lo
  descarta, limpia la biblioteca de títulos obsoletos y regenera la semilla
  real. Subir `VersionCatalogoActual` fuerza la actualización en instalaciones
  existentes.
- **`Database/schema.sql`** se actualiza con la columna `CategoriaNombre` y las
  **25 sentencias `INSERT INTO Juegos`** equivalentes a la semilla.

### Etapa 7 — *"Rediseño de la Billetera: pasarela de pago, gift cards e historial"*

`WalletView` pasa de recargar con un clic a un flujo tipo Steam con pasarela de
pago simulada (overlay a pantalla completa dentro de la app):

- **Montos:** tarjetas estilizadas $10/$25/$50/$100 (con borde de acento y
  selección resaltada) **más** un campo de **monto personalizado** validado entre
  `$5.00` y `$500.00`.
- **Botón "Continuar con el pago":** no acredita nada todavía; abre la pasarela.
- **Pasarela de pago (Paso 1):** tres métodos seleccionables:
  - **Tarjeta de Crédito / Débito** con *Número (16 dígitos auto-formateado en
    bloques de 4)*, **Nombre**, **Vencimiento (MM/AA)** y **CVC (3 dígitos)** con
    **validación en tiempo real** (longitud, solo dígitos y fecha no vencida).
  - **Tarjeta de Regalo / Tarjeta Steam:** código simulado (ej.
    `STEAM-100USD-2026`) con valor propio predefinido.
  - **PayPal / Cuenta Digital:** inicio de sesión rápido simulado con correo.
- **Confirmación (Paso 2):** resumen con monto, método (tarjeta **enmascarada**
  `**** **** **** 4321`) y total, con botón **"Confirmar y Recargar"**.
- **Procesamiento (Paso 3):** indicador **"Procesando pago..."** (~1.4 s) y
  luego acreditación real.
- **Éxito (Paso 4):** confirma "...se han añadido $XX.00 USD a tu billetera
  exitosamente" y muestra el nuevo saldo.
- **Persistencia y notificaciones:** `UsuarioService.RecargarSaldo` (devuelve
  `bool`, valida el rango) suma y persiste el saldo en `usuarios.json`;
  `TransaccionService.RegistrarRecarga` registra el movimiento como *"Recarga de
  Billetera"* en `transacciones.json` **y crea la notificación** de sistema en
  `notificaciones.json`. `ObtenerRecargasUsuario` / `ObtenerMovimientosUsuario`
  alimentan el historial (recargas en verde `+`, compras en naranja `-`). Los
  reportes de ventas cuentan **solo compras** (las recargas no inflan ingresos).
- **Maquetación responsiva:** los sub-paneles y campos calculan su ancho relativo
  al contenedor (margen lateral de 15 px, `Vencimiento`/`CVC` en dos columnas de
  ~45%), usan `AutoScroll` para no cortarse y multiplican posiciones/tamaños por
  `UiTheme.EscalaUI`, de modo que nada desborda el ancho de la ventana (400 px)
  ni con escalado.

---

## Manual de creación paso a paso

Si quisieras **recrear este proyecto desde cero**, la secuencia natural (la
misma que sigue la bitácora) sería:

1. **Esqueleto del proyecto**
   * Crear un proyecto WinForms: `dotnet new winforms -n NovaCore`
     (target `net8.0-windows`, `OutputType=WinExe`, `UseWindowsForms=true`).
   * Definir las carpetas `Models/`, `Services/`, `Views/`, `Database/`, `Assets/`.

2. **Modelo de dominio** (`Models/`)
   * Empezar con `Usuario`, `Juego`, `Categoria` y luego crecer: `BibliotecaItem`,
     `Transaccion`, `ItemCarrito`, `ItemDeseo`, `Reporte`, `Notificacion`,
     `SolicitudAmistad` (marcar con `[JsonIgnore]` los campos transitorios,
     como el estado "en ejecución").

3. **Capa de datos y persistencia**
   * `DataStore` como singleton en memoria con semilla (categorías, 25 juegos
     reales —5 por categoría— y la cuenta `admin`), más un control de versión de
     catálogo para poder migrar la semilla en instalaciones existentes.
   * `PersistenciaService` con JSON por entidad en `%LOCALAPPDATA%\NovaCore`,
     con `try/catch` para no tumbar la app si no hay permisos de escritura.

4. **Servicios de negocio** (`Services/`)
   * Uno por dominio: `UsuarioService`, `JuegoService`, `BibliotecaService`,
     `TransaccionService`, `CarritoService`, `DeseoService`,
     `NotificacionService`, `RecomendacionService`, `ReporteService`,
     `SessionManager`.
   * Regla clave: **las vistas solo llaman a servicios**; el día que se cambie
     el almacenamiento, solo se tocan `DataStore` y los servicios.

5. **Shell de la ventana** (`MainForm`)
   * Construir el shell con header + `ContentPanel` (double buffering) + bottom
     nav.
   * Método único `MostrarVista(UserControl)` para navegar entre `UserControl`s.
   * Agregar escalado responsivo (factor sobre 400&times;820) y animación de
     transición.

6. **Vistas** (`Views/`)
   * `UiTheme` primero (paleta, helpers de botones, back bar, grid oscuro,
     carga de imágenes con caché).
   * Login/Registro → Tienda → Detalle de juego → Carrito/Billetera/Lista de
     deseos → Biblioteca/Perfil/Notificaciones → paneles de administración.

7. **Rediseño progresivo** (de clon de escritorio a app móvil)
   * Traducir la UI, fijar proporción de teléfono, soportar redimensionado y
     añadir identidad visual propia (tema + logo).

8. **Verificar** con `dotnet build` (sin advertencias ni errores) y probar los
   flujos principales: registro → compra → biblioteca → "Jugar", y
   amigos → solicitud → aceptar.

---

## Pendientes / mejoras futuras

- **Persistir el carrito** (hoy vive solo en memoria, `CarritoService`) y las
  **búsquedas recientes** (base para recomendaciones más durables).
- **Migrar a base de datos real** (SQLite/SQL Server) usando el esquema de
  `Database/schema.sql`.
- **Descarga real de juegos** (hoy "Ir a mi biblioteca"/"Jugar" simula la
  posesión y las horas jugadas con un timer).
- Completar los ítems "próximamente" del menú (Comunidad, Chat, Configuración).
- **Imágenes de portada del catálogo real:** hoy `ImagenPortada` queda en `null`
  para los 25 juegos sembrados (el proyecto solo trae `Assets/logo.png`), así que
  las vistas muestran su placeholder hasta que el administrador suba una portada.
- **Nunca** commitear credenciales reales ni secretos al repositorio.