namespace ControlManager.Models
{
    /// <summary>
    /// Opción del filtro de severidad (clave estable para lógica + etiqueta para UI).
    /// </summary>
    public sealed class SeverityFilterOption
    {
        public SeverityFilterOption(string key, string label)
        {
            Key = key;
            Label = label;
        }

        /// <summary>
        /// Clave usada en el filtrado: Todos, Alta, Media, Baja.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Texto mostrado junto al indicador de color.
        /// </summary>
        public string Label { get; }

        public override string ToString() => Label;
    }
}
