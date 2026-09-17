using NovaCore.Models;
using System.Text.Json;

namespace NovaCore.Services
{
    /// <summary>
    /// Persiste en disco los usuarios registrados y sus fotos de perfil
    /// (carpeta %LOCALAPPDATA%\NovaCore), para que no se pierdan al
    /// cerrar la aplicación.
    /// </summary>
    public static class PersistenciaService
    {
        private static readonly string CarpetaDatos = ObtenerCarpetaBase();

        private static string ObtenerCarpetaBase()
        {
            var overrideVar = Environment.GetEnvironmentVariable("NOVACORE_DATADIR");
            if (!string.IsNullOrWhiteSpace(overrideVar)) return overrideVar;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovaCore");
        }

        public static string CarpetaAvatares => Path.Combine(CarpetaDatos, "Avatares");

        public static string CarpetaPortadas => Path.Combine(CarpetaDatos, "Portadas");

        private static string ArchivoUsuarios => Path.Combine(CarpetaDatos, "usuarios.json");

        private static string ArchivoBiblioteca => Path.Combine(CarpetaDatos, "biblioteca.json");

        private static string ArchivoSolicitudes => Path.Combine(CarpetaDatos, "solicitudes.json");

        private static string ArchivoDeseos => Path.Combine(CarpetaDatos, "deseos.json");

        private static string ArchivoNotificaciones => Path.Combine(CarpetaDatos, "notificaciones.json");

        private static string ArchivoTransacciones => Path.Combine(CarpetaDatos, "transacciones.json");

        private static readonly JsonSerializerOptions Opciones = new()
        {
            WriteIndented = true
        };

        public static void GuardarUsuarios(IEnumerable<Usuario> usuarios)
        {
            try
            {
                Directory.CreateDirectory(CarpetaDatos);
                var json = JsonSerializer.Serialize(new List<Usuario>(usuarios), Opciones);
                File.WriteAllText(ArchivoUsuarios, json);
            }
            catch
            {
                // Si no se puede escribir, la app sigue funcionando en memoria.
            }
        }

        public static List<Usuario> CargarUsuarios()
        {
            try
            {
                if (!File.Exists(ArchivoUsuarios)) return null;
                var json = File.ReadAllText(ArchivoUsuarios);
                return JsonSerializer.Deserialize<List<Usuario>>(json, Opciones);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Guarda la biblioteca de todos los usuarios (juegos adquiridos y horas jugadas).</summary>
        public static void GuardarBiblioteca(IEnumerable<BibliotecaItem> biblioteca)
        {
            try
            {
                Directory.CreateDirectory(CarpetaDatos);
                var json = JsonSerializer.Serialize(new List<BibliotecaItem>(biblioteca), Opciones);
                File.WriteAllText(ArchivoBiblioteca, json);
            }
            catch
            {
                // Si no se puede escribir, la app sigue funcionando en memoria.
            }
        }

        public static List<BibliotecaItem> CargarBiblioteca()
        {
            try
            {
                if (!File.Exists(ArchivoBiblioteca)) return null;
                var json = File.ReadAllText(ArchivoBiblioteca);
                return JsonSerializer.Deserialize<List<BibliotecaItem>>(json, Opciones);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Guarda las solicitudes de amistad de todos los usuarios.</summary>
        public static void GuardarSolicitudes(IEnumerable<SolicitudAmistad> solicitudes)
        {
            try
            {
                Directory.CreateDirectory(CarpetaDatos);
                var json = JsonSerializer.Serialize(new List<SolicitudAmistad>(solicitudes), Opciones);
                File.WriteAllText(ArchivoSolicitudes, json);
            }
            catch
            {
                // Si no se puede escribir, la app sigue funcionando en memoria.
            }
        }

        public static List<SolicitudAmistad> CargarSolicitudes()
        {
            try
            {
                if (!File.Exists(ArchivoSolicitudes)) return null;
                var json = File.ReadAllText(ArchivoSolicitudes);
                return JsonSerializer.Deserialize<List<SolicitudAmistad>>(json, Opciones);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Guarda la lista de deseos de todos los usuarios.</summary>
        public static void GuardarDeseos(IEnumerable<ItemDeseo> deseos)
        {
            Guardar(ArchivoDeseos, new List<ItemDeseo>(deseos));
        }

        public static List<ItemDeseo> CargarDeseos() => Cargar<ItemDeseo>(ArchivoDeseos);

        /// <summary>Guarda las notificaciones de todos los usuarios.</summary>
        public static void GuardarNotificaciones(IEnumerable<Notificacion> notificaciones)
        {
            Guardar(ArchivoNotificaciones, new List<Notificacion>(notificaciones));
        }

        public static List<Notificacion> CargarNotificaciones() => Cargar<Notificacion>(ArchivoNotificaciones);

        /// <summary>Guarda las transacciones (ventas) registradas.</summary>
        public static void GuardarTransacciones(IEnumerable<Transaccion> transacciones)
        {
            Guardar(ArchivoTransacciones, new List<Transaccion>(transacciones));
        }

        public static List<Transaccion> CargarTransacciones() => Cargar<Transaccion>(ArchivoTransacciones);

        private static void Guardar<T>(string archivo, List<T> datos)
        {
            try
            {
                Directory.CreateDirectory(CarpetaDatos);
                var json = JsonSerializer.Serialize(datos, Opciones);
                File.WriteAllText(archivo, json);
            }
            catch
            {
                // Si no se puede escribir, la app sigue funcionando en memoria.
            }
        }

        private static List<T> Cargar<T>(string archivo)
        {
            try
            {
                if (!File.Exists(archivo)) return null;
                var json = File.ReadAllText(archivo);
                return JsonSerializer.Deserialize<List<T>>(json, Opciones);
            }
            catch
            {
                return null;
            }
        }
    }
}