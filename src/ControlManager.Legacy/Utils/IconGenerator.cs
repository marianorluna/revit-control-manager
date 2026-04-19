using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace ControlManager.Utils
{
    /// <summary>
    /// Carga los íconos del ribbon desde PNG embebidos (mismo arte que el paquete .bundle).
    /// Regenerar los PNG con <c>installer/generate_bundle_icons.ps1</c> si cambia el diseño.
    /// </summary>
    public static class IconGenerator
    {
        /// <summary>
        /// Devuelve el bitmap 16 o 32 px para el ribbon (escudo y check).
        /// </summary>
        public static BitmapSource GetIcon(int size)
        {
            if (size < 8 || size > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            string fileName = size >= 32 ? "icon_32.png" : "icon_16.png";
            var assembly = Assembly.GetExecutingAssembly();
            string assemblyName = assembly.GetName().Name ?? "ControlManager";
            var uri = new Uri(
                $"pack://application:,,,/{assemblyName};component/Resources/Icons/{fileName}",
                UriKind.Absolute);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
