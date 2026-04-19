using System.Text;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ControlManager.BundleInstaller;

internal static class Program
{
    private const string BundleFolderName = "ControlManager.bundle";
    private const string EmbeddedBundleResourceName = "ControlManager.BundleInstaller.Assets.ControlManager.bundle.zip";
    private const string InstallSwitch = "--install";
    private const string UninstallSwitch = "--uninstall";
    private const string ToggleSwitch = "--toggle";
    private const string LegacyUninstallSwitch = "/uninstall";
    private const uint MessageBoxOkCancel = 0x00000001;
    private const uint MessageBoxOkInformation = 0x00000040;
    private const uint MessageBoxOkError = 0x00000010;
    private const int MessageBoxIdOk = 1;
    private const string MessageBoxTitle = "Control Manager Installer";

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            InstallerMode mode = ResolveMode(args, out bool isToggleMode);
            if (mode == InstallerMode.Uninstall)
            {
                if (isToggleMode && !ConfirmToggleUninstall())
                {
                    WriteInfo("Operación cancelada por el usuario.");
                    return 0;
                }

                return UninstallBundle();
            }

            return InstallBundleFromEmbeddedResource();
        }
        catch (Exception ex)
        {
            WriteError("Error inesperado del instalador.", ex.Message);
            ShowErrorDialog($"Error inesperado del instalador.\n\n{ex.Message}");
            return 1;
        }
    }

    private static InstallerMode ResolveMode(IReadOnlyList<string> args, out bool isToggleMode)
    {
        isToggleMode = false;

        foreach (string arg in args)
        {
            if (arg.Equals(UninstallSwitch, StringComparison.OrdinalIgnoreCase) ||
                arg.Equals(LegacyUninstallSwitch, StringComparison.OrdinalIgnoreCase))
            {
                return InstallerMode.Uninstall;
            }

            if (arg.Equals(InstallSwitch, StringComparison.OrdinalIgnoreCase))
            {
                return InstallerMode.Install;
            }

            if (arg.Equals(ToggleSwitch, StringComparison.OrdinalIgnoreCase))
            {
                isToggleMode = true;
                return Directory.Exists(GetDestinationBundlePath())
                    ? InstallerMode.Uninstall
                    : InstallerMode.Install;
            }
        }

        // Modo por defecto: un clic. Si existe instalación, desinstala; si no, instala.
        isToggleMode = true;
        return Directory.Exists(GetDestinationBundlePath())
            ? InstallerMode.Uninstall
            : InstallerMode.Install;
    }

    private static bool ConfirmToggleUninstall()
    {
        const string message =
            "Se detectó una instalación existente de Control Manager.\n\n" +
            "¿Deseas desinstalarla ahora?\n\n" +
            "Pulsa OK para desinstalar o Cancelar para salir.";

        int result = MessageBoxW(IntPtr.Zero, message, MessageBoxTitle, MessageBoxOkCancel);
        return result == MessageBoxIdOk;
    }

    private static int InstallBundleFromEmbeddedResource()
    {
        string extractionRoot = Path.Combine(Path.GetTempPath(), "ControlManagerInstaller", Guid.NewGuid().ToString("N"));
        string extractedBundlePath = Path.Combine(extractionRoot, BundleFolderName);

        try
        {
            Directory.CreateDirectory(extractionRoot);
            ExtractEmbeddedBundleZip(extractionRoot);
            return InstallBundle(extractedBundlePath);
        }
        finally
        {
            TryDeleteDirectory(extractionRoot);
        }
    }

    private static void ExtractEmbeddedBundleZip(string extractionRoot)
    {
        using Stream? bundleZipStream = typeof(Program).Assembly.GetManifestResourceStream(EmbeddedBundleResourceName);
        if (bundleZipStream is null)
        {
            throw new FileNotFoundException(
                $"No se encontró el recurso embebido del bundle: {EmbeddedBundleResourceName}");
        }

        string zipPath = Path.Combine(extractionRoot, "ControlManager.bundle.zip");
        using (var fileStream = File.Create(zipPath))
        {
            bundleZipStream.CopyTo(fileStream);
        }

        ZipFile.ExtractToDirectory(zipPath, extractionRoot, overwriteFiles: true);
    }

    private static int InstallBundle(string sourceBundlePath)
    {
        string packageContentsPath = Path.Combine(sourceBundlePath, "PackageContents.xml");
        if (!File.Exists(packageContentsPath))
        {
            const string msg = "El recurso embebido no contiene un bundle válido (falta PackageContents.xml).\n\nIntenta descargar el instalador de nuevo.";
            WriteError("No se encontró el bundle de origen.", $"Ruta esperada: {sourceBundlePath}", msg);
            ShowErrorDialog(msg);
            return 1;
        }

        string destinationBundlePath = GetDestinationBundlePath();
        string destinationRoot = Path.GetDirectoryName(destinationBundlePath)!;

        WriteInfo("Instalando Control Manager...");
        WriteInfo($"Origen:  {sourceBundlePath}");
        WriteInfo($"Destino: {destinationBundlePath}");

        Directory.CreateDirectory(destinationRoot);

        if (Directory.Exists(destinationBundlePath))
        {
            WriteInfo("Eliminando instalación anterior...");
            DeleteDirectorySafe(destinationBundlePath);
        }

        CopyDirectoryRecursive(sourceBundlePath, destinationBundlePath);
        UnblockFilesRecursive(destinationBundlePath);

        string destinationPackageContentsPath = Path.Combine(destinationBundlePath, "PackageContents.xml");
        if (!File.Exists(destinationPackageContentsPath))
        {
            const string msg = "La instalación terminó pero el bundle no quedó correctamente copiado (falta PackageContents.xml en destino).\n\nIntenta ejecutar el instalador como Administrador.";
            WriteError(msg);
            ShowErrorDialog(msg);
            return 1;
        }

        WriteSuccess("Instalación completada.");
        WriteInfo("Reinicia Revit para cargar el plugin.");
        ShowInfoDialog(
            "Instalación completada.\n\nReinicia Revit para cargar el plugin.");
        return 0;
    }

    private static int UninstallBundle()
    {
        string destinationBundlePath = GetDestinationBundlePath();
        if (!Directory.Exists(destinationBundlePath))
        {
            WriteInfo("No hay instalación para eliminar.");
            WriteInfo($"Ruta: {destinationBundlePath}");
            return 0;
        }

        WriteInfo("Desinstalando Control Manager...");
        WriteInfo($"Ruta: {destinationBundlePath}");
        DeleteDirectorySafe(destinationBundlePath);
        WriteSuccess("Desinstalación completada.");
        ShowInfoDialog("Desinstalación completada.");
        return 0;
    }

    private static void ShowInfoDialog(string message)
    {
        MessageBoxW(IntPtr.Zero, message, MessageBoxTitle, MessageBoxOkInformation);
    }

    private static void ShowErrorDialog(string message)
    {
        MessageBoxW(IntPtr.Zero, message, MessageBoxTitle, MessageBoxOkError);
    }

    private static string GetDestinationBundlePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Autodesk", "ApplicationPlugins", BundleFolderName);
    }

    private static void DeleteDirectorySafe(string path)
    {
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                Thread.Sleep(300);
            }
        }

        throw new IOException($"No se pudo eliminar la carpeta: {path}. Cierra Revit e inténtalo de nuevo.");
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Limpieza best-effort de temporales.
        }
    }

    private static void CopyDirectoryRecursive(string sourcePath, string destinationPath)
    {
        var sourceDirectory = new DirectoryInfo(sourcePath);
        if (!sourceDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"No existe la carpeta de origen: {sourcePath}");
        }

        Directory.CreateDirectory(destinationPath);

        foreach (FileInfo file in sourceDirectory.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            string destinationFilePath = Path.Combine(destinationPath, file.Name);
            file.CopyTo(destinationFilePath, overwrite: true);
        }

        foreach (DirectoryInfo subDirectory in sourceDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly))
        {
            string destinationSubDirPath = Path.Combine(destinationPath, subDirectory.Name);
            CopyDirectoryRecursive(subDirectory.FullName, destinationSubDirPath);
        }
    }

    private static void UnblockFilesRecursive(string rootPath)
    {
        foreach (string filePath in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                string zoneIdentifierStream = filePath + ":Zone.Identifier";
                if (File.Exists(zoneIdentifierStream))
                {
                    File.Delete(zoneIdentifierStream);
                }
            }
            catch
            {
                // No bloquea instalación; intentamos desbloquear de forma best-effort.
            }
        }
    }

    private static void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteError(params string[] lines)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        foreach (string line in lines)
        {
            Console.WriteLine(line);
        }
        Console.ResetColor();
    }

    private enum InstallerMode
    {
        Install,
        Uninstall
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
