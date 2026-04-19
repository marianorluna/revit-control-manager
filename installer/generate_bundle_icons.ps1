# Genera icon_32.png e icon_16.png (escudo + check, mismo diseño que IconGenerator) en:
# - src/ControlManager.Legacy/Resources/Icons (recursos embebidos en el DLL)
# - bundle/ControlManager.bundle/Resources (paquete Autodesk)
# Requiere Windows PowerShell 5+ (WPF). Ejecutar: powershell -ExecutionPolicy Bypass -File installer/generate_bundle_icons.ps1
$ErrorActionPreference = 'Stop'
$assemblies = @(
    [System.Reflection.Assembly]::LoadWithPartialName('PresentationCore').Location,
    [System.Reflection.Assembly]::LoadWithPartialName('WindowsBase').Location,
    [System.Reflection.Assembly]::LoadWithPartialName('System.Xaml').Location
) | Where-Object { $_ }

$code = @'
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class ControlManagerIconExporter
{
    private const double DesignSize = 32.0;

    public static void Save(int size, string path)
    {
        if (size < 8 || size > 256)
            throw new ArgumentOutOfRangeException("size");

        Color shieldFill = Color.FromRgb(0x1A, 0x73, 0xC6);
        var visual = new DrawingVisual();
        double scale = size / DesignSize;

        using (DrawingContext dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));

            var shield = Geometry.Parse(
                "M 16 2.5 L 29.2 7.5 L 29.2 23.5 Q 29.2 28 16 31.2 Q 2.8 28 2.8 23.5 L 2.8 7.5 Z");
            dc.DrawGeometry(new SolidColorBrush(shieldFill), null, shield);

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

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using (var fs = File.Create(path))
            encoder.Save(fs);
    }
}
'@

Add-Type -TypeDefinition $code -ReferencedAssemblies $assemblies -Language CSharp -ErrorAction Stop

$root = Split-Path $PSScriptRoot -Parent
$legacyIcons = Join-Path $root 'src\ControlManager.Legacy\Resources\Icons'
$bundleRes = Join-Path $root 'bundle\ControlManager.bundle\Resources'

foreach ($size in @(16, 32)) {
    $name = "icon_$size.png"
    $p1 = Join-Path $legacyIcons $name
    $p2 = Join-Path $bundleRes $name
    [ControlManagerIconExporter]::Save($size, $p1)
    [ControlManagerIconExporter]::Save($size, $p2)
    Write-Host "OK: $name"
}

Write-Host "Listo: $legacyIcons y $bundleRes"
