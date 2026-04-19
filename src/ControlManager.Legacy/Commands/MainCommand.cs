using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ControlManager.Services;
using ControlManager.Utils;

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
                UIDocument? uiDoc = commandData.Application.ActiveUIDocument;
                Document? doc = uiDoc?.Document;

                if (doc == null)
                {
                    TaskDialog.Show(
                        "Control Manager",
                        "No hay ningún modelo abierto.");
                    return Result.Cancelled;
                }

                if (!RevitHelper.IsProjectDocument(doc))
                {
                    TaskDialog.Show(
                        "Control Manager",
                        "Este plugin solo funciona en proyectos de Revit, no en familias.");
                    return Result.Cancelled;
                }

                if (RevitHelper.GetNonTypeElementCount(doc) == 0)
                {
                    TaskDialog.Show(
                        "Control Manager",
                        "El modelo está vacío o no tiene elementos analizables.");
                    return Result.Cancelled;
                }

                bool eulaOk = FirstRunService.ShowFirstRunDialogIfNeeded(null);
                if (!eulaOk)
                {
                    return Result.Cancelled;
                }

                MainWindowSession.ShowSingleton(doc, uiDoc!);
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
