using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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

            ModelName = document.Title ?? string.Empty;

            RunChecksCommand = new RelayCommand(ExecuteRunChecks, () => !IsRunning);
            ExportCommand = new RelayCommand(ExecuteExportPlaceholder);
            SelectInRevitCommand = new RelayCommand(ExecuteSelectInRevitPlaceholder);
            SelectAllCommand = new RelayCommand(ExecuteSelectAll, () => FilteredIssues.Count > 0);
            DeselectAllCommand = new RelayCommand(ExecuteDeselectAll, () => FilteredIssues.Count > 0);

            ApplyFilter();
        }

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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Texto del botón principal según si hay análisis en curso.
        /// </summary>
        public string RunChecksButtonText => IsRunning ? "Analizando..." : "▶ Ejecutar QC";

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
            }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnIssuesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseIssueRelatedProperties();
            CommandManager.InvalidateRequerySuggested();
        }

        private void RaiseIssueRelatedProperties()
        {
            OnPropertyChanged(nameof(HasIssues));
            OnPropertyChanged(nameof(HasNoIssues));
            OnPropertyChanged(nameof(ShowEmptySuccess));
            OnPropertyChanged(nameof(ShowRunPrompt));
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

        private void ExecuteExportPlaceholder()
        {
            MessageBox.Show(
                "La exportación a Excel estará disponible en la Fase 4.",
                "Control Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExecuteSelectInRevitPlaceholder()
        {
            MessageBox.Show(
                "Seleccionar elementos en Revit estará disponible en la Fase 4.",
                "Control Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExecuteSelectAll()
        {
            foreach (ElementIssue issue in FilteredIssues)
            {
                issue.IsSelected = true;
            }
        }

        private void ExecuteDeselectAll()
        {
            foreach (ElementIssue issue in FilteredIssues)
            {
                issue.IsSelected = false;
            }
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
