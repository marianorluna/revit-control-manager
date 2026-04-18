using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ControlManager.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class MainCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                string version = commandData.Application.Application.VersionNumber;
                TaskDialog.Show("Control Manager v1.0", "Plugin iniciado correctamente. Revit " + version);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = "Error al ejecutar Control Manager: " + ex.Message;
                return Result.Failed;
            }
        }
    }
}
