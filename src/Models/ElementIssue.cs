using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace ControlManager.Models
{
    /// <summary>
    /// Representa un problema de QC sobre un elemento del modelo.
    /// </summary>
    public sealed class ElementIssue : INotifyPropertyChanged
    {
        public int RevitElementId { get; set; }

        public ElementId ElementId { get; set; } = ElementId.InvalidElementId;

        public string ElementName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string TypeName { get; set; } = string.Empty;

        public IssueType IssueType { get; set; }

        public string IssueDescription { get; set; } = string.Empty;

        /// <summary>
        /// Texto para tooltip de fila: ID y descripción completa.
        /// </summary>
        public string RowTooltip => $"ID: {RevitElementId} | {IssueDescription}";

        public Severity Severity { get; set; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Etiqueta en español del tipo de incidencia (columna en la rejilla).
        /// </summary>
        public string IssueTypeLabel
        {
            get
            {
                switch (IssueType)
                {
                    case IssueType.EmptyName:
                        return "Nombre vacío";
                    case IssueType.MissingComment:
                        return "Comentarios faltantes";
                    case IssueType.UnplacedRoom:
                        return "Habitación sin colocar";
                    case IssueType.UnboundedRoom:
                        return "Habitación sin cerrar";
                    case IssueType.DuplicateMark:
                        return "Mark duplicado";
                    case IssueType.MissingLevel:
                        return "Nivel faltante";
                    default:
                        return IssueType.ToString();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string SeverityLabel
        {
            get
            {
                switch (Severity)
                {
                    case Severity.High:
                        return "Alta";
                    case Severity.Medium:
                        return "Media";
                    default:
                        return "Baja";
                }
            }
        }
    }
}
