using System.ComponentModel;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ControlManager.ViewModels;

namespace ControlManager.UI
{
    /// <summary>
    /// Ventana principal WPF de Control Manager.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(Document document, UIDocument uidocument)
        {
            InitializeComponent();
            DataContext = new MainViewModel(document, uidocument);
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // No cancelar el cierre; no se requiere lógica adicional para la sesión de Revit.
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectAllCommand.Execute(null);
            }
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.DeselectAllCommand.Execute(null);
            }
        }
    }
}
