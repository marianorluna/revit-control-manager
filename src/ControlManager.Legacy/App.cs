using System;
using System.Reflection;
using Autodesk.Revit.UI;
using ControlManager.Utils;

namespace ControlManager
{
    public class App : IExternalApplication
    {
        private const string PanelName = "Quality Control";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Un solo botón: se registra en el tab Add-Ins (Complementos), no en un tab propio.
                RibbonPanel panel = application.CreateRibbonPanel(PanelName);
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                PushButtonData buttonData = new PushButtonData(
                    "ControlManagerButton",
                    "Iniciar\nControl QC",
                    assemblyPath,
                    "ControlManager.Commands.MainCommand")
                {
                    ToolTip = "Ejecuta verificaciones de Quality Control sobre el modelo activo",
                    LongDescription = "Detecta elementos con parámetros vacíos, cuartos sin encerrar y nombres faltantes. Compatible con Revit 2023-2027."
                };

                RibbonItem ribbonItem = panel.AddItem(buttonData);
                if (ribbonItem is PushButton pushButton)
                {
                    pushButton.LargeImage = IconGenerator.GetIcon(32);
                    pushButton.Image = IconGenerator.GetIcon(16);
                    pushButton.SetContextualHelp(
                        new ContextualHelp(
                            ContextualHelpType.Url,
                            "https://github.com/marianorluna/revit-control-manager/blob/main/README.md"));
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Control Manager", "Error al iniciar el plugin:\n" + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
