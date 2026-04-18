using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ControlManager.Models;
using ControlManager.Services;
using ControlManager.Utils;
using Microsoft.Win32;

namespace ControlManager.ViewModels
{
    /// <summary>
    /// ViewModel principal de la ventana de Control Manager (MVVM).
    /// </summary>
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly Document _document;
        private readonly UIDocument _uidocument;

        private string _selectedSeverityFilterKey = "Todos";
        private string _statusMessage = "Listo";
        private bool _isRunning;
        private string _modelName = string.Empty;
        private string _analysisDateTime = "—";
        private bool _hasRunAnalysis;

        public MainViewModel(Document document, UIDocument uidocument)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _uidocument = uidocument ?? throw new ArgumentNullException(nameof(uidocument));

            AllIssues = new ObservableCollection<ElementIssue>();
            FilteredIssues = new ObservableCollection<ElementIssue>();

            AllIssues.CollectionChanged += OnIssuesCollectionChanged;
            FilteredIssues.CollectionChanged += OnIssuesCollectionChanged;
            FilteredIssues.CollectionChanged += OnFilteredIssuesCollectionChangedForHeader;

            ModelName = document.Title ?? string.Empty;

            RunChecksCommand = new RelayCommand(ExecuteRunChecks, () => !IsRunning);
            ExportCommand = new RelayCommand(ExecuteExport, () => !IsRunning);
            SelectInRevitCommand = new RelayCommand(ExecuteSelectInRevit, () => !IsRunning);
            SelectAllCommand = new RelayCommand(ExecuteSelectAll, () => FilteredIssues.Count > 0);
            DeselectAllCommand = new RelayCommand(ExecuteDeselectAll, () => FilteredIssues.Count > 0);

            ApplyFilter();
        }

        /// <summary>
        /// Solicita cerrar la ventana WPF (p. ej. tras seleccionar en Revit).
        /// </summary>
        public event EventHandler? RequestCloseWindow;

        public ObservableCollection<ElementIssue> AllIssues { get; }

        public ObservableCollection<ElementIssue> FilteredIssues { get; }

        public List<SeverityFilterOption> SeverityFilterOptions { get; } = new List<SeverityFilterOption>
        {
            new SeverityFilterOption("Todos", "Todos"),
            new SeverityFilterOption("Alta", "Alta"),
            new SeverityFilterOption("Media", "Media"),
            new SeverityFilterOption("Baja", "Baja")
        };

        /// <summary>
        /// Clave del filtro de severidad (Todos, Alta, Media, Baja); enlazada al ComboBox con SelectedValuePath.
        /// </summary>
        public string SelectedSeverityFilterKey
        {
            get => _selectedSeverityFilterKey;
            set
            {
                if (!SetProperty(ref _selectedSeverityFilterKey, value))
                {
                    return;
                }

                ApplyFilter();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (!SetProperty(ref _isRunning, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(RunChecksButtonText));
                OnPropertyChanged(nameof(CanInteractWithModel));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Deshabilita filtros y acciones secundarias mientras corre el análisis.
        /// </summary>
        public bool CanInteractWithModel => !IsRunning;

        /// <summary>
        /// Texto del botón principal según si hay análisis en curso.
        /// </summary>
        public string RunChecksButtonText => IsRunning ? "⏳ Analizando..." : "▶ Ejecutar QC";

        public bool HasIssues => FilteredIssues.Count > 0;

        public bool HasNoIssues => !HasIssues;

        /// <summary>
        /// Indica si ya se ejecutó al menos un análisis QC (para mensaje de estado vacío).
        /// </summary>
        public bool HasRunAnalysis
        {
            get => _hasRunAnalysis;
            private set
            {
                if (!SetProperty(ref _hasRunAnalysis, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(ShowEmptySuccess));
                OnPropertyChanged(nameof(ShowRunPrompt));
                OnPropertyChanged(nameof(ShowResultsGrid));
            }
        }

        /// <summary>
        /// Muestra la rejilla de resultados tras un QC con incidencias.
        /// </summary>
        public bool ShowResultsGrid => HasRunAnalysis && AllIssues.Count > 0;

        /// <summary>
        /// Muestra el mensaje de éxito cuando el análisis terminó sin incidencias en el modelo.
        /// </summary>
        public bool ShowEmptySuccess => HasRunAnalysis && AllIssues.Count == 0;

        /// <summary>
        /// Muestra la indicación de ejecutar QC antes del primer análisis.
        /// </summary>
        public bool ShowRunPrompt => !HasRunAnalysis && AllIssues.Count == 0;

        public int TotalCount { get; private set; }

        public int HighCount { get; private set; }

        public int MediumCount { get; private set; }

        public int LowCount { get; private set; }

        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        public string AnalysisDateTime
        {
            get => _analysisDateTime;
            set => SetProperty(ref _analysisDateTime, value);
        }

        public RelayCommand RunChecksCommand { get; }

        public RelayCommand ExportCommand { get; }

        public RelayCommand SelectInRevitCommand { get; }

        public RelayCommand SelectAllCommand { get; }

        public RelayCommand DeselectAllCommand { get; }

        /// <summary>
        /// Estado del check del encabezado: todos los visibles, ninguno o mezcla (indeterminado).
        /// </summary>
        public bool? HeaderSelectAllCheckState
        {
            get
            {
                if (FilteredIssues.Count == 0)
                {
                    return false;
                }

                int selected = FilteredIssues.Count(i => i.IsSelected);
                if (selected == 0)
                {
                    return false;
                }

                if (selected == FilteredIssues.Count)
                {
                    return true;
                }

                return null;
            }
        }

        /// <summary>
        /// Invierte entre seleccionar todos los visibles y deseleccionar todos (cabecera del grid).
        /// </summary>
        public void ToggleSelectAllFiltered()
        {
            if (FilteredIssues.Count == 0)
            {
                return;
            }

            bool selectAll = HeaderSelectAllCheckState != true;
            foreach (ElementIssue issue in FilteredIssues)
            {
                issue.IsSelected = selectAll;
            }

            OnPropertyChanged(nameof(HeaderSelectAllCheckState));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnIssuesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseIssueRelatedProperties();
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnFilteredIssuesCollectionChangedForHeader(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ElementIssue issue in e.OldItems)
                {
                    issue.PropertyChanged -= FilteredIssuePropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (ElementIssue issue in e.NewItems)
                {
                    issue.PropertyChanged += FilteredIssuePropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (ElementIssue issue in FilteredIssues)
                {
                    issue.PropertyChanged -= FilteredIssuePropertyChanged;
                    issue.PropertyChanged += FilteredIssuePropertyChanged;
                }
            }

            OnPropertyChanged(nameof(HeaderSelectAllCheckState));
        }

        private void FilteredIssuePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ElementIssue.IsSelected))
            {
                OnPropertyChanged(nameof(HeaderSelectAllCheckState));
            }
        }

        private void RaiseIssueRelatedProperties()
        {
            OnPropertyChanged(nameof(HasIssues));
            OnPropertyChanged(nameof(HasNoIssues));
            OnPropertyChanged(nameof(ShowEmptySuccess));
            OnPropertyChanged(nameof(ShowRunPrompt));
            OnPropertyChanged(nameof(ShowResultsGrid));
        }

        private void ExecuteRunChecks()
        {
            IsRunning = true;
            StatusMessage = "Analizando...";

            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                RunChecksCore();
                return;
            }

            dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(RunChecksCore));
        }

        private void RunChecksCore()
        {
            try
            {
                var qc = new QCService();
                List<ElementIssue> issues = qc.RunAllChecks(_document);

                AllIssues.Clear();
                foreach (ElementIssue issue in issues)
                {
                    AllIssues.Add(issue);
                }

                HasRunAnalysis = true;
                AnalysisDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-ES"));
                ApplyFilter();
                UpdateCounters();
                StatusMessage = "Listo";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                TaskDialog.Show(
                    "Control Manager",
                    "Error al ejecutar las comprobaciones QC:\n" + ex.Message);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void ExecuteExport()
        {
            if (FilteredIssues.Count == 0)
            {
                TaskDialog.Show(
                    "Control Manager",
                    "No hay datos para exportar.");
                return;
            }

            string defaultName = ExportService.BuildDefaultFileName(ModelName, DateTime.Now);
            var dialog = new SaveFileDialog
            {
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = defaultName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                DefaultExt = ".xlsx",
                AddExtension = true
            };

            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            try
            {
                string path = Path.GetFullPath(dialog.FileName);
                ExportService.ExportToExcel(FilteredIssues.ToList(), path, ModelName);
                ShowExportSuccessTaskDialog(path);
            }
            catch (Exception ex)
            {
                TaskDialog.Show(
                    "Control Manager",
                    "No se pudo exportar el archivo Excel:\n" + ex.Message);
            }
        }

        private static void ShowExportSuccessTaskDialog(string path)
        {
            var td = new TaskDialog("Control Manager")
            {
                MainInstruction = "Exportación completada",
                MainContent = "El archivo se guardó correctamente.",
                ExpandedContent = path
            };
            td.AddCommandLink(
                TaskDialogCommandLinkId.CommandLink1,
                "Abrir archivo",
                "Abrir con la aplicación predeterminada.");
            td.CommonButtons = TaskDialogCommonButtons.Close;
            td.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult result = td.Show();
            if (result == TaskDialogResult.CommandLink1)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    TaskDialog.Show(
                        "Control Manager",
                        "No se pudo abrir el archivo:\n" + ex.Message);
                }
            }
        }

        private void ExecuteSelectInRevit()
        {
            if (!FilteredIssues.Any(i => i.IsSelected))
            {
                StatusMessage = "⚠️ Marca al menos un elemento para seleccionar";
                return;
            }

            try
            {
                QCService.SelectElementsInRevit(_uidocument, FilteredIssues.ToList());
                StatusMessage = "Elementos seleccionados en Revit.";
                RequestCloseWindow?.Invoke(this, EventArgs.Empty);
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                TaskDialog.Show(
                    "Control Manager",
                    "No se pudieron seleccionar los elementos en Revit:\n" + ex.Message);
            }
        }

        private void ExecuteSelectAll()
        {
            foreach (ElementIssue issue in FilteredIssues)
            {
                issue.IsSelected = true;
            }

            OnPropertyChanged(nameof(HeaderSelectAllCheckState));
        }

        private void ExecuteDeselectAll()
        {
            foreach (ElementIssue issue in FilteredIssues)
            {
                issue.IsSelected = false;
            }

            OnPropertyChanged(nameof(HeaderSelectAllCheckState));
        }

        private void ApplyFilter()
        {
            FilteredIssues.Clear();

            IEnumerable<ElementIssue> query = AllIssues.AsEnumerable();

            switch (_selectedSeverityFilterKey)
            {
                case "Alta":
                    query = query.Where(i => i.Severity == Severity.High);
                    break;
                case "Media":
                    query = query.Where(i => i.Severity == Severity.Medium);
                    break;
                case "Baja":
                    query = query.Where(i => i.Severity == Severity.Low);
                    break;
            }

            foreach (ElementIssue issue in query)
            {
                FilteredIssues.Add(issue);
            }

            UpdateCounters();
            RaiseIssueRelatedProperties();
        }

        private void UpdateCounters()
        {
            TotalCount = AllIssues.Count;
            HighCount = AllIssues.Count(i => i.Severity == Severity.High);
            MediumCount = AllIssues.Count(i => i.Severity == Severity.Medium);
            LowCount = AllIssues.Count(i => i.Severity == Severity.Low);

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(HighCount));
            OnPropertyChanged(nameof(MediumCount));
            OnPropertyChanged(nameof(LowCount));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
