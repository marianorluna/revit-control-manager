using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using ControlManager.Models;
using ControlManager.Utils;

namespace ControlManager.Services
{
    /// <summary>
    /// Motor de comprobaciones QC sobre el documento activo (solo lectura).
    /// </summary>
    public sealed class QCService
    {
        public List<ElementIssue> CheckEmptyNames(Document doc)
        {
            var issues = new List<ElementIssue>();
            if (doc == null)
            {
                return issues;
            }

            BuiltInCategory[] categories =
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Rooms,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_Roofs
            };

            foreach (BuiltInCategory bic in categories)
            {
                IList<Element> elements = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (Element e in elements)
                {
                    string displayName = RevitHelper.GetElementName(e);
                    string typeName = GetTypeName(doc, e);
                    bool nameEmpty = string.IsNullOrWhiteSpace(displayName) || displayName == "Sin nombre";
                    bool typeEmpty = string.IsNullOrWhiteSpace(typeName);

                    if (!nameEmpty && !typeEmpty)
                    {
                        continue;
                    }

                    issues.Add(CreateIssue(
                        doc,
                        e,
                        IssueType.EmptyName,
                        "Nombre o nombre de tipo vacío.",
                        Severity.Medium));
                }
            }

            return issues;
        }

        public List<ElementIssue> CheckMissingComments(Document doc)
        {
            var issues = new List<ElementIssue>();
            if (doc == null)
            {
                return issues;
            }

            BuiltInCategory[] categories =
            {
                BuiltInCategory.OST_Rooms,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors
            };

            foreach (BuiltInCategory bic in categories)
            {
                IList<Element> elements = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (Element e in elements)
                {
                    string comments = RevitHelper.SafeGetParameterValue(e, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (!string.IsNullOrWhiteSpace(comments))
                    {
                        continue;
                    }

                    issues.Add(CreateIssue(
                        doc,
                        e,
                        IssueType.MissingComment,
                        "Falta el valor en el parámetro Comentarios.",
                        Severity.Low));
                }
            }

            return issues;
        }

        public List<ElementIssue> CheckUnplacedRooms(Document doc)
        {
            var issues = new List<ElementIssue>();
            if (doc == null)
            {
                return issues;
            }

            IList<Element> rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (Element element in rooms)
            {
                if (!(element is Room room))
                {
                    continue;
                }

                // Revit 2023: no expone IsUnplaced en Room/SpatialElement; habitación sin colocar suele tener Location nulo.
                if (room.Location == null)
                {
                    issues.Add(CreateIssue(
                        doc,
                        room,
                        IssueType.UnplacedRoom,
                        "Habitación sin colocar.",
                        Severity.High));
                    continue;
                }

                double area = room.Area;
                if (Math.Abs(area) < 1e-9)
                {
                    issues.Add(CreateIssue(
                        doc,
                        room,
                        IssueType.UnboundedRoom,
                        "Habitación sin cerrar o con área cero.",
                        Severity.High));
                }
            }

            return issues;
        }

        public List<ElementIssue> CheckDuplicateMarks(Document doc)
        {
            var issues = new List<ElementIssue>();
            if (doc == null)
            {
                return issues;
            }

            BuiltInCategory[] categories =
            {
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Walls
            };

            var elements = new List<Element>();
            foreach (BuiltInCategory bic in categories)
            {
                elements.AddRange(
                    new FilteredElementCollector(doc)
                        .OfCategory(bic)
                        .WhereElementIsNotElementType()
                        .ToElements());
            }

            IEnumerable<IGrouping<string, Element>> groups = elements
                .Select(e => new { Element = e, Mark = RevitHelper.SafeGetParameterValue(e, BuiltInParameter.ALL_MODEL_MARK).Trim() })
                .Where(x => !string.IsNullOrEmpty(x.Mark))
                .GroupBy(x => x.Mark, x => x.Element);

            foreach (IGrouping<string, Element> group in groups)
            {
                List<Element> list = group.ToList();
                if (list.Count < 2)
                {
                    continue;
                }

                foreach (Element e in list)
                {
                    issues.Add(CreateIssue(
                        doc,
                        e,
                        IssueType.DuplicateMark,
                        $"Mark duplicado: {group.Key}",
                        Severity.Medium));
                }
            }

            return issues;
        }

        public List<ElementIssue> CheckMissingLevels(Document doc)
        {
            var issues = new List<ElementIssue>();
            if (doc == null)
            {
                return issues;
            }

            BuiltInCategory[] categories =
            {
                BuiltInCategory.OST_Rooms,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows
            };

            foreach (BuiltInCategory bic in categories)
            {
                IList<Element> elements = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (Element e in elements)
                {
                    if (!ShouldHaveLevel(e))
                    {
                        continue;
                    }

                    ElementId levelId = e.LevelId;
                    if (levelId != null && levelId != ElementId.InvalidElementId)
                    {
                        continue;
                    }

                    issues.Add(CreateIssue(
                        doc,
                        e,
                        IssueType.MissingLevel,
                        "El elemento no tiene nivel asignado.",
                        Severity.High));
                }
            }

            return issues;
        }

        public List<ElementIssue> RunAllChecks(Document doc)
        {
            var all = new List<ElementIssue>();
            all.AddRange(CheckEmptyNames(doc));
            all.AddRange(CheckMissingComments(doc));
            all.AddRange(CheckUnplacedRooms(doc));
            all.AddRange(CheckDuplicateMarks(doc));
            all.AddRange(CheckMissingLevels(doc));

            return all
                .OrderByDescending(i => (int)i.Severity)
                .ThenBy(i => i.Category)
                .ThenBy(i => i.RevitElementId)
                .ToList();
        }

        /// <summary>
        /// Selecciona en Revit los elementos de las incidencias marcadas (<see cref="ElementIssue.IsSelected"/>).
        /// </summary>
        /// <exception cref="InvalidOperationException">Si ninguna incidencia está marcada.</exception>
        public static void SelectElementsInRevit(UIDocument uidoc, List<ElementIssue> issues)
        {
            if (uidoc == null)
            {
                throw new ArgumentNullException(nameof(uidoc));
            }

            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            List<ElementIssue> selected = issues.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0)
            {
                throw new InvalidOperationException(
                    "Debe marcar al menos un elemento en la lista para seleccionarlo en Revit.");
            }

            ICollection<ElementId> ids = selected.Select(i => i.ElementId).ToList();
            uidoc.Selection.SetElementIds(ids);
        }

        private static bool ShouldHaveLevel(Element e)
        {
            if (e is Room room)
            {
                return room.Location != null;
            }

            return e is FamilyInstance;
        }

        private static string GetTypeName(Document doc, Element e)
        {
            try
            {
                ElementId typeId = e.GetTypeId();
                if (typeId == ElementId.InvalidElementId)
                {
                    return string.Empty;
                }

                Element? typeElement = doc.GetElement(typeId);
                if (typeElement is ElementType et)
                {
                    return et.Name?.Trim() ?? string.Empty;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static ElementIssue CreateIssue(
            Document doc,
            Element e,
            IssueType issueType,
            string description,
            Severity severity)
        {
            return new ElementIssue
            {
                RevitElementId = e.Id.IntegerValue,
                ElementId = e.Id,
                ElementName = RevitHelper.GetElementName(e),
                Category = RevitHelper.GetCategoryName(e),
                TypeName = GetTypeName(doc, e),
                IssueType = issueType,
                IssueDescription = description,
                Severity = severity,
                IsSelected = false
            };
        }
    }
}
