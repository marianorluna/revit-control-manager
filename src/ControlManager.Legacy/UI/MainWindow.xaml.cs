using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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
            var viewModel = new MainViewModel(document, uidocument);
            viewModel.RequestCloseWindow += (_, _) => Close();
            DataContext = viewModel;
            Closing += MainWindow_Closing;
        }

        /// <summary>
        /// Indica si la ventana sigue mostrando el mismo documento que el activo en Revit.
        /// </summary>
        internal bool IsBoundToDocument(Document doc)
        {
            return DataContext is MainViewModel vm && ReferenceEquals(vm.Document, doc);
        }

        /// <summary>
        /// Trae la ventana al frente y la restaura si estaba minimizada (reuso desde el ribbon).
        /// </summary>
        internal void ActivateForReuse()
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // No cancelar el cierre; no se requiere lógica adicional para la sesión de Revit.
        }

        private void HeaderSelectAllCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ToggleSelectAllFiltered();
            }

            e.Handled = true;
        }

        private void PrivacyPolicyLink_Click(object sender, RoutedEventArgs e)
        {
            var window = new PrivacyPolicyWindow { Owner = this };
            window.ShowDialog();
        }
    }
}
