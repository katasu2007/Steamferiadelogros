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
| Login / Registro  | `LoginView` / `RegisterView`   | Iniciar sesión y crear cuenta con validación en vivo. |
| Tienda            | `StoreView`                    | Banner destacado + cards "Destacados y recomendados". |
| Detalle de juego  | `GameDetailView`               | Ficha completa: descripción, desarrollador/distribuidora, etiquetas, reseñas simuladas, caja de compra, wishlist, requisitos min/rec, contenido maduro y botón "Reportar este juego". |
| Búsqueda          | `SearchView`                   | Filtra el catálogo en tiempo real; registra términos para las recomendaciones. |
| Recomendaciones   | `RecomendacionesView`          | Sugerencias personalizadas por categorías poseídas + búsquedas recientes (o los más populares si aún no hay datos). |
| Carrito           | `CartView`                     | Lista de juegos a comprar, quitar ítems, ver total/saldo y confirmar compra (checkout). |
| Billetera         | `WalletView`                   | Muestra saldo y recarga montos predefinidos ($10/$25/$50/$100). |
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
    │   ├── DataStore.cs             → Singleton en memoria; semilla (admin, 5 categorías, 6 juegos) + carga JSON
    │   ├── PersistenciaService.cs   → Archivos JSON en %LOCALAPPDATA%\NovaCore (usuarios, biblioteca, solicitudes, deseos, notificaciones, transacciones) + Avatares/ y Portadas/
    │   ├── SessionManager.cs        → Usuario activo + EsAdministrador
    │   ├── UsuarioService.cs        → Login, registro con validación, roles, baneo, saldo, avatar, amigos/solicitudes, eliminar cuenta
    │   ├── JuegoService.cs          → CRUD + categorías + PrepararImagenPortada
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

- **Usuario:** `UsuarioId`, `NombreUsuario`, `Correo`, `PasswordHash` (texto
  plano por ahora), `Rol` (`Usuario`/`Administrador`), `Saldo`, `Activo`
  (ban/falso), `AvatarPath`, `FechaRegistro` y `Amigos` (nombres de usuario).
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
| `UsuarioService`        | Login (rechaza cuentas inactivas), registro con reglas (usuario 3–20, solo letras/números/guion bajo; `admin` reservado; correo válido; contraseña 4–64 con letras y números), cambio de rol, baneo/reactivación, recarga de saldo, avatar, búsqueda de usuarios/amigos, enviar/aceptar/rechazar solicitudes, eliminar cuenta (limpia biblioteca, transacciones, carrito, solicitudes, deseos y notificaciones). |
| `JuegoService`          | Catálogo con filtros (texto + categoría), CRUD y copia de portadas a disco. |
| `BibliotecaService`     | Posesión, alta de juegos y registro de sesiones jugadas. |
| `TransaccionService`    | Procesa compras (valida posesión y saldo), notifica, y produce ingresos/ventas/ranking. |
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
├── biblioteca.json         ← juegos adquiridos, horas jugadas, última sesión
├── solicitudes.json        ← solicitudes de amistad y su estado
├── deseos.json             ← listas de deseos
├── notificaciones.json     ← notificaciones (leídas/no leídas)
├── transacciones.json      ← historial de compras
├── Avatares/               ← fotos de perfil copiadas desde la app
└── Portadas/               ← portadas de juegos elegidas por el admin
```

- `DataStore` carga estos archivos al arranque y descarta entradas huérfanas
  (usuarios/juegos que ya no existen). Si `usuarios.json` no existe o no tiene
  ningún Administrador, se garantiza la cuenta `admin`.
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
`CarritoItems` y `Reportes`, más los datos semilla (5 categorías y la cuenta
`admin`).

Sirve como guía si se quiere migrar la persistencia JSON a una base real.
Para ello: agrega `Microsoft.Data.Sqlite` (línea comentada en el `.csproj`) y
reemplaza la implementación interna de `DataStore` y de los `*Service` — las
**vistas no cambian**.

---

## Historial de creación (bitácora)

El proyecto se construyó en **3 etapas** (3 commits). Cada etapa representó un
cambio importante de diseño y alcance:

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
   * `DataStore` como singleton en memoria con semilla (categorías, un juego de
     cada una y la cuenta `admin`).
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

- **Hashing real de contraseñas** (BCrypt o PBKDF2) en lugar de texto plano.
- **Persistir el carrito** (hoy vive solo en memoria, `CarritoService`) y las
  **búsquedas recientes** (base para recomendaciones más durables).
- **Migrar a base de datos real** (SQLite/SQL Server) usando el esquema de
  `Database/schema.sql`.
- **Descarga real de juegos** (hoy "Ir a mi biblioteca"/"Jugar" simula la
  posesión y las horas jugadas con un timer).
- Completar los ítems "próximamente" del menú (Comunidad, Chat, Configuración).
- Cifrar/ajustar `PasswordHash`: **nunca** commitear credenciales reales ni
  secretos al repositorio.