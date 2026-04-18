using Autodesk.Revit.DB;

namespace ControlManager.Utils
{
    /// <summary>
    /// Utilidades de lectura segura sobre la API de Revit.
    /// </summary>
    public static class RevitHelper
    {
        public static string GetElementName(Element e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(e.Name))
                {
                    return e.Name.Trim();
                }

                string mark = SafeGetParameterValue(e, BuiltInParameter.ALL_MODEL_MARK);
                if (!string.IsNullOrWhiteSpace(mark))
                {
                    return mark.Trim();
                }

                return "Sin nombre";
            }
            catch
            {
                return "Sin nombre";
            }
        }

        public static string GetCategoryName(Element e)
        {
            try
            {
                return e.Category?.Name ?? "Sin categoría";
            }
            catch
            {
                return "Sin categoría";
            }
        }

        public static string SafeGetParameterValue(Element e, BuiltInParameter param)
        {
            try
            {
                Parameter? p = e.get_Parameter(param);
                if (p == null)
                {
                    return string.Empty;
                }

                if (!p.HasValue)
                {
                    return string.Empty;
                }

                if (p.StorageType == StorageType.String)
                {
                    return p.AsString() ?? string.Empty;
                }

                string? s = p.AsValueString();
                return s ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string SafeGetParameterValue(Element e, string paramName)
        {
            try
            {
                string trimmed = paramName?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(trimmed))
                {
                    return string.Empty;
                }

                Parameter? p = e.LookupParameter(trimmed);
                if (p == null)
                {
                    return string.Empty;
                }

                if (!p.HasValue)
                {
                    return string.Empty;
                }

                if (p.StorageType == StorageType.String)
                {
                    return p.AsString() ?? string.Empty;
                }

                string? s = p.AsValueString();
                return s ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool IsProjectDocument(Document doc)
        {
            if (doc == null)
            {
                return false;
            }

            try
            {
                return !doc.IsFamilyDocument;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cuenta elementos del proyecto excluyendo tipos (para validar si el modelo tiene contenido analizable).
        /// </summary>
        public static int GetNonTypeElementCount(Document doc)
        {
            if (doc == null)
            {
                return 0;
            }

            try
            {
                return new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElementIds().Count;
            }
            catch
            {
                return 0;
            }
        }
    }
}
