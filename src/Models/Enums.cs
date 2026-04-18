namespace ControlManager.Models
{
    /// <summary>
    /// Tipos de incidencia detectadas en el modelo (mensajes al usuario en español desde QCService).
    /// </summary>
    public enum IssueType
    {
        EmptyName,
        MissingComment,
        UnplacedRoom,
        UnboundedRoom,
        DuplicateMark,
        MissingLevel
    }

    public enum Severity
    {
        Low,
        Medium,
        High
    }
}
