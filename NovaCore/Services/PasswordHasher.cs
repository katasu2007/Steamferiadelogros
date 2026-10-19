using System.Security.Cryptography;

namespace NovaCore.Services
{
    /// <summary>
    /// Hashea y verifica contraseñas con PBKDF2 (Rfc2898DeriveBytes, SHA-256),
    /// usando una sal aleatoria por usuario. No requiere paquetes externos.
    /// Formato almacenado: "PBKDF2$&lt;iteraciones&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt;"
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefijo = "PBKDF2";
        private const int Iteraciones = 100_000;
        private const int BytesSalt = 16;
        private const int BytesHash = 32;

        /// <summary>Genera el hash completo (con sal) de una contraseña en texto plano.</summary>
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(BytesSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iteraciones, HashAlgorithmName.SHA256, BytesHash);

            return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Comprueba si la contraseña en texto plano coincide con el hash almacenado.
        /// Si el valor almacenado no tiene formato de hash (registros antiguos en
        /// texto plano), compara directamente como respaldo de compatibilidad.
        /// </summary>
        public static bool Verificar(string password, string almacenado)
        {
            if (string.IsNullOrEmpty(almacenado)) return false;

            string[] partes = almacenado.Split('$');
            if (partes.Length != 4 || partes[0] != Prefijo)
                return almacenado == password; // migración: registro antiguo en texto plano

            if (!int.TryParse(partes[1], out int iteraciones)) return false;

            byte[] salt = Convert.FromBase64String(partes[2]);
            byte[] hashEsperado = Convert.FromBase64String(partes[3]);

            byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }

        /// <summary>Indica si un valor almacenado ya está hasheado (útil para migrar registros antiguos).</summary>
        public static bool EsHash(string almacenado) =>
            !string.IsNullOrEmpty(almacenado) && almacenado.Split('$')[0] == Prefijo;
    }
}
