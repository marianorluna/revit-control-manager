using System;
using System.Diagnostics;
using System.Windows;
using Autodesk.Revit.UI;
using ControlManager.Utils;

namespace ControlManager.UI
{
    /// <summary>
    /// Muestra la política de privacidad en HTML (WebBrowser).
    /// </summary>
    public partial class PrivacyPolicyWindow : Window
    {
        public PrivacyPolicyWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string? path = PrivacyPolicyHtmlPath.GetExistingPath();
                if (!string.IsNullOrEmpty(path))
                {
                    PolicyBrowser.Navigate(new Uri(path, UriKind.Absolute));
                }
                else
                {
                    PolicyBrowser.NavigateToString(
                        "<html><head><meta charset=\"utf-8\"/></head><body style=\"font-family:Segoe UI,sans-serif;padding:16px;\">" +
                        "<p>No se encontró el archivo <code>docs/privacy-policy.html</code> junto al complemento.</p>" +
                        "<p>Reinstala Control Manager o copia el archivo desde la documentación del proyecto.</p>" +
                        "</body></html>");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show(
                    "Control Manager",
                    "No se pudo cargar la política de privacidad:\n" + ex.Message);
            }
        }

        private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? path = PrivacyPolicyHtmlPath.GetExistingPath();
                if (string.IsNullOrEmpty(path))
                {
                    TaskDialog.Show(
                        "Control Manager",
                        "No se encontró el archivo de política de privacidad en el equipo.");
                    return;
                }

                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                TaskDialog.Show(
                    "Control Manager",
                    "No se pudo abrir el archivo en el navegador:\n" + ex.Message);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
