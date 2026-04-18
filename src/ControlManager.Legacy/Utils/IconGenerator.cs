using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ControlManager.Utils
{
    /// <summary>
    /// Genera íconos del ribbon (escudo y check) como <see cref="BitmapSource"/> sin archivos PNG externos.
    /// </summary>
    public static class IconGenerator
    {
        private const double DesignSize = 32.0;

        private static readonly Color ShieldFill = Color.FromRgb(0x1A, 0x73, 0xC6);

        /// <summary>
        /// Crea un bitmap cuadrado (16 o 32 px) con escudo azul y check blanco.
        /// </summary>
        /// <param name="size">Tamaño en píxeles (típicamente 16 o 32).</param>
        public static BitmapSource GetIcon(int size)
        {
            if (size < 8 || size > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var visual = new DrawingVisual();
            double scale = size / DesignSize;

            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(scale, scale));

                var shield = Geometry.Parse(
                    "M 16 2.5 L 29.2 7.5 L 29.2 23.5 Q 29.2 28 16 31.2 Q 2.8 28 2.8 23.5 L 2.8 7.5 Z");
                dc.DrawGeometry(new SolidColorBrush(ShieldFill), null, shield);

                var check = Geometry.Parse("M 8.5 16.2 L 13.8 21.5 L 24.5 9.8");
                var checkPen = new Pen(Brushes.White, 2.8)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                dc.DrawGeometry(null, checkPen, check);

                dc.Pop();
            }

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }
    }
}
