using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ControlManager.UI;

namespace ControlManager.Commands
{
    /// <summary>
    /// Mantiene una única ventana de Control Manager por sesión de Revit; reactiva la existente si el comando se ejecuta de nuevo.
    /// </summary>
    internal static class MainWindowSession
    {
        private static MainWindow? _window;

        public static void ShowSingleton(Document doc, UIDocument uiDoc)
        {
            if (_window != null && _window.IsLoaded)
            {
                if (_window.IsBoundToDocument(doc))
                {
                    _window.ActivateForReuse();
                    return;
                }

                _window.Close();
            }

            _window = new MainWindow(doc, uiDoc);
            _window.Closed += OnWindowClosed;
            _window.ShowDialog();
        }

        private static void OnWindowClosed(object? sender, EventArgs e)
        {
            if (sender is MainWindow closed && ReferenceEquals(closed, _window))
            {
                _window = null;
            }
        }
    }
}
