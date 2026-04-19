using System;
using System.Globalization;
using System.Windows;
using Autodesk.Revit.UI;
using ControlManager.UI;
using ControlManager.Utils;
using Microsoft.Win32;

namespace ControlManager.Services
{
    /// <summary>
    /// Flujo de primer uso: aceptación de EULA y registro en HKCU.
    /// </summary>
    public static class FirstRunService
    {
        private const string RegistryKey = @"SOFTWARE\ControlManager";
        private const string EulaAcceptedValue = "EulaAccepted";
        private const string InstallDateValue = "InstallDate";

        /// <summary>
        /// Indica si el usuario ya aceptó los términos (valor "1" en el registro).
        /// </summary>
        public static bool HasAcceptedEula()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: false);
                if (key == null)
                {
                    return false;
                }

                object? value = key.GetValue(EulaAcceptedValue);
                return value is string s && s == "1";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Marca la EULA como aceptada y guarda la fecha de instalación/aceptación.
        /// </summary>
        public static void SetEulaAccepted()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegistryKey, writable: true);
                if (key == null)
                {
                    return;
                }

                key.SetValue(EulaAcceptedValue, "1", RegistryValueKind.String);
                string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                key.SetValue(InstallDateValue, today, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                TaskDialog.Show(
                    "Control Manager",
                    "No se pudo guardar la aceptación de términos en el registro:\n" + ex.Message);
            }
        }

        /// <summary>
        /// Muestra el diálogo de bienvenida si aún no se aceptó la EULA.
        /// </summary>
        /// <param name="owner">Ventana propietaria para el visor de política (puede ser null).</param>
        /// <returns>True si puede continuar; false si el usuario rechaza los términos.</returns>
        public static bool ShowFirstRunDialogIfNeeded(Window? owner)
        {
            if (HasAcceptedEula())
            {
                return true;
            }

            while (true)
            {
                var td = new TaskDialog("Control Manager")
                {
                    MainInstruction = "Bienvenido a Control Manager",
                    MainContent =
                        "Control Manager te ayuda a realizar Quality Control (QC) sobre modelos de Revit.\n\n" +
                        "Antes de continuar debes aceptar los términos de uso. " +
                        "El complemento opera de forma local y no envía datos a Internet. " +
                        "Puedes revisar la política de privacidad antes de aceptar.",
                    ExpandedContent = "Al aceptar, confirmas que has leído y aceptas el uso del software según la política de privacidad y la licencia aplicable."
                };

                td.AddCommandLink(
                    TaskDialogCommandLinkId.CommandLink1,
                    "Acepto los términos y continuar",
                    "Guardar aceptación y abrir Control Manager.");
                td.AddCommandLink(
                    TaskDialogCommandLinkId.CommandLink2,
                    "Ver política de privacidad",
                    "Abre el documento de privacidad en una ventana.");
                td.CommonButtons = TaskDialogCommonButtons.Cancel;
                td.DefaultButton = TaskDialogResult.CommandLink1;

                TaskDialogResult result = td.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    SetEulaAccepted();
                    return true;
                }

                if (result == TaskDialogResult.CommandLink2)
                {
                    var privacyWindow = new PrivacyPolicyWindow { Owner = owner };
                    privacyWindow.ShowDialog();
                    continue;
                }

                return false;
            }
        }
    }
}
