using SteamCloneApp.Models;
using System.Text.Json;

namespace SteamCloneApp.Services
{
    /// <summary>
    /// Persiste en disco los usuarios registrados y sus fotos de perfil
    /// (carpeta %LOCALAPPDATA%\SteamCloneApp), para que no se pierdan al
    /// cerrar la aplicación.
    /// </summary>
    public static class PersistenciaService
    {
        private static readonly string CarpetaDatos = ObtenerCarpetaBase();

        private static string ObtenerCarpetaBase()
        {
            var overrideVar = Environment.GetEnvironmentVariable("STEAMCLONE_DATADIR");
            if (!string.IsNullOrWhiteSpace(overrideVar)) return overrideVar;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SteamCloneApp");
        }

        public static string CarpetaAvatares => Path.Combine(CarpetaDatos, "Avatares");

        private static string ArchivoUsuarios => Path.Combine(CarpetaDatos, "usuarios.json");

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
    }
}