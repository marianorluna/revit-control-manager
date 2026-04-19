using System.IO;
using System.Reflection;

namespace ControlManager.Utils
{
    /// <summary>
    /// Resuelve la ruta del HTML de política de privacidad copiado junto al ensamblado (carpeta docs).
    /// </summary>
    internal static class PrivacyPolicyHtmlPath
    {
        /// <summary>
        /// Devuelve la ruta absoluta si el archivo existe; en caso contrario null.
        /// </summary>
        public static string? GetExistingPath()
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(dir))
            {
                return null;
            }

            string path = Path.Combine(dir, "docs", "privacy-policy.html");
            return File.Exists(path) ? path : null;
        }
    }
}
