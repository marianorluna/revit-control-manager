using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ControlManager.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ControlManager.Services
{
    /// <summary>
    /// Exportación del reporte QC a Excel (.xlsx) con Open XML SDK (sin Excel ni ClosedXML).
    /// </summary>
    public static class ExportService
    {
        private const string TitleHex = "FF1A73C6";
        private const string HeaderHex = "FF2B2B2B";
        private const string TotalHex = "FFF0F0F0";
        private const string HighHex = "FFFFDDDD";
        private const string MediumHex = "FFFFFBCC";
        private const string LowHex = "FFDDFFEE";
        private const string WhiteHex = "FFFFFFFF";

        /// <summary>
        /// Genera un libro Excel con el reporte QC y la hoja de resumen.
        /// </summary>
        public static string ExportToExcel(List<ElementIssue> issues, string suggestedPath, string modelName)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            if (string.IsNullOrWhiteSpace(suggestedPath))
            {
                throw new ArgumentException("La ruta sugerida no es válida.", nameof(suggestedPath));
            }

            string? dir = Path.GetDirectoryName(suggestedPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            int high = issues.Count(i => i.Severity == Severity.High);
            int medium = issues.Count(i => i.Severity == Severity.Medium);
            int low = issues.Count(i => i.Severity == Severity.Low);
            int total = issues.Count;
            string analysisStamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-ES"));
            string modelLabel = string.IsNullOrWhiteSpace(modelName) ? "—" : modelName.Trim();

            Stylesheet stylesheet = BuildStylesheet(
                out uint stTitle,
                out uint stSubtitle,
                out uint stHeader,
                out uint stDataHigh,
                out uint stDataMedium,
                out uint stDataLow,
                out uint stTotal);

            using (SpreadsheetDocument doc = SpreadsheetDocument.Create(suggestedPath, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = doc.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorkbookStylesPart stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = stylesheet;
                stylesPart.Stylesheet.Save();

                // --- Hoja Reporte QC
                WorksheetPart reportPart = workbookPart.AddNewPart<WorksheetPart>();
                SheetData reportData = new SheetData();

                int totalsRow = issues.Count == 0 ? 5 : 4 + issues.Count + 1;
                int lastDataRowForFilter = issues.Count == 0 ? 4 : 4 + issues.Count;

                reportData.Append(CreateMergedTextRow(1, "A", "CONTROL MANAGER — Reporte de Quality Control", stTitle));
                reportData.Append(CreateMergedTextRow(2, "A", $"Fecha y hora: {analysisStamp}  |  Modelo: {modelLabel}", stSubtitle));
                reportData.Append(CreateMergedTextRow(3, "A", string.Empty, null));

                var headerRow = new Row { RowIndex = 4 };
                string[] headers =
                {
                    "ID Revit",
                    "Categoría",
                    "Nombre",
                    "Tipo de Problema",
                    "Descripción",
                    "Severidad"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    headerRow.Append(CreateTextCell(headers[i], 4, i + 1, stHeader));
                }

                reportData.Append(headerRow);

                int row = 5;
                foreach (ElementIssue issue in issues)
                {
                    uint st = issue.Severity switch
                    {
                        Severity.High => stDataHigh,
                        Severity.Medium => stDataMedium,
                        _ => stDataLow
                    };

                    var dataRow = new Row { RowIndex = (uint)row };
                    dataRow.Append(CreateNumberCell(issue.RevitElementId, row, 1, st));
                    dataRow.Append(CreateTextCell(issue.Category, row, 2, st));
                    dataRow.Append(CreateTextCell(issue.ElementName, row, 3, st));
                    dataRow.Append(CreateTextCell(issue.IssueTypeLabel, row, 4, st));
                    dataRow.Append(CreateTextCell(issue.IssueDescription, row, 5, st));
                    dataRow.Append(CreateTextCell(issue.SeverityLabel, row, 6, st));
                    reportData.Append(dataRow);
                    row++;
                }

                reportData.Append(CreateMergedTextRow(totalsRow, "A",
                    $"Totales — Alta: {high} | Media: {medium} | Baja: {low} | Total: {total}", stTotal));

                MergeCells mergeReport = new MergeCells(
                    new MergeCell { Reference = "A1:F1" },
                    new MergeCell { Reference = "A2:F2" },
                    new MergeCell { Reference = "A3:F3" },
                    new MergeCell { Reference = $"A{totalsRow}:F{totalsRow}" });

                SheetViews reportViews = new SheetViews(
                    new SheetView(
                        new Pane
                        {
                            VerticalSplit = 4d,
                            TopLeftCell = "A5",
                            ActivePane = PaneValues.BottomLeft,
                            State = PaneStateValues.Frozen
                        })
                    {
                        WorkbookViewId = 0U
                    });

                Columns reportCols = new Columns(
                    new Column { Min = 1, Max = 1, Width = 12, CustomWidth = true },
                    new Column { Min = 2, Max = 2, Width = 22, CustomWidth = true },
                    new Column { Min = 3, Max = 3, Width = 28, CustomWidth = true },
                    new Column { Min = 4, Max = 4, Width = 22, CustomWidth = true },
                    new Column { Min = 5, Max = 5, Width = 36, CustomWidth = true },
                    new Column { Min = 6, Max = 6, Width = 12, CustomWidth = true });

                string filterRef = issues.Count == 0 ? "A4:F4" : $"A4:F{lastDataRowForFilter}";
                AutoFilter autoFilter = new AutoFilter { Reference = filterRef };

                reportPart.Worksheet = new Worksheet(reportViews, reportCols, reportData, mergeReport, autoFilter);

                // --- Hoja Resumen
                WorksheetPart summaryPart = workbookPart.AddNewPart<WorksheetPart>();
                SheetData summaryData = new SheetData();

                Row sumR1 = new Row { RowIndex = 1 };
                sumR1.Append(CreateTextCell("Análisis realizado:", 1, 1, null));
                sumR1.Append(CreateTextCell(analysisStamp, 1, 2, null));
                summaryData.Append(sumR1);

                var sumHeader = new Row { RowIndex = 3 };
                sumHeader.Append(CreateTextCell("Categoría", 3, 1, stHeader));
                sumHeader.Append(CreateTextCell("Alta", 3, 2, stHeader));
                sumHeader.Append(CreateTextCell("Media", 3, 3, stHeader));
                sumHeader.Append(CreateTextCell("Baja", 3, 4, stHeader));
                sumHeader.Append(CreateTextCell("Total", 3, 5, stHeader));
                summaryData.Append(sumHeader);

                List<string> categories = issues
                    .Select(i => i.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int sRow = 4;
                foreach (string cat in categories)
                {
                    List<ElementIssue> inCat = issues.Where(i => string.Equals(i.Category, cat, StringComparison.OrdinalIgnoreCase)).ToList();
                    int h = inCat.Count(x => x.Severity == Severity.High);
                    int m = inCat.Count(x => x.Severity == Severity.Medium);
                    int l = inCat.Count(x => x.Severity == Severity.Low);
                    int t = inCat.Count;

                    var r = new Row { RowIndex = (uint)sRow };
                    r.Append(CreateTextCell(cat, sRow, 1, null));
                    r.Append(CreateNumberCell(h, sRow, 2, null));
                    r.Append(CreateNumberCell(m, sRow, 3, null));
                    r.Append(CreateNumberCell(l, sRow, 4, null));
                    r.Append(CreateNumberCell(t, sRow, 5, null));
                    summaryData.Append(r);
                    sRow++;
                }

                Columns sumCols = new Columns(
                    new Column { Min = 1, Max = 1, Width = 28, CustomWidth = true },
                    new Column { Min = 2, Max = 5, Width = 10, CustomWidth = true });

                summaryPart.Worksheet = new Worksheet(sumCols, summaryData);

                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet { Name = "Reporte QC", SheetId = 1, Id = workbookPart.GetIdOfPart(reportPart) });
                sheets.Append(new Sheet { Name = "Resumen", SheetId = 2, Id = workbookPart.GetIdOfPart(summaryPart) });

                workbookPart.Workbook.Save();
            }

            return suggestedPath;
        }

        private static Row CreateMergedTextRow(int rowIndex, string startCol, string text, uint? styleIndex)
        {
            var row = new Row { RowIndex = (uint)rowIndex };
            row.Append(CreateTextCell(text, rowIndex, 1, styleIndex));
            return row;
        }

        private static Cell CreateTextCell(string text, int row, int col1, uint? styleIndex)
        {
            string cellRef = GetColumnName(col1) + row;
            var cell = new Cell
            {
                CellReference = cellRef,
                DataType = CellValues.InlineString
            };

            if (styleIndex.HasValue)
            {
                cell.StyleIndex = styleIndex.Value;
            }

            cell.AppendChild(new InlineString(new Text(text ?? string.Empty)));
            return cell;
        }

        private static Cell CreateNumberCell(int value, int row, int col1, uint? styleIndex)
        {
            string cellRef = GetColumnName(col1) + row;
            var cell = new Cell
            {
                CellReference = cellRef,
                DataType = CellValues.Number
            };

            if (styleIndex.HasValue)
            {
                cell.StyleIndex = styleIndex.Value;
            }

            cell.AppendChild(new CellValue(value.ToString(CultureInfo.InvariantCulture)));
            return cell;
        }

        private static Stylesheet BuildStylesheet(
            out uint styleTitle,
            out uint styleSubtitle,
            out uint styleHeader,
            out uint styleDataHigh,
            out uint styleDataMedium,
            out uint styleDataLow,
            out uint styleTotal)
        {
            // Fills 0–1: requeridos por Excel (ninguno + gris125).
            var fonts = new Fonts(
                new Font(new FontSize { Val = 11 }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 14 }, new Color { Rgb = new HexBinaryValue(WhiteHex) }, new FontName { Val = "Calibri" }),
                new Font(new FontSize { Val = 11 }, new Color { Rgb = new HexBinaryValue("FF333333") }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 11 }, new Color { Rgb = new HexBinaryValue(WhiteHex) }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 11 }, new Color { Rgb = new HexBinaryValue("FF222222") }, new FontName { Val = "Calibri" }));

            var fills = new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue(TitleHex) }) { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue(HeaderHex) }) { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue(HighHex) }) { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue(MediumHex) }) { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue(LowHex) }) { PatternType = PatternValues.Solid }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue(TotalHex) }) { PatternType = PatternValues.Solid }));

            var borders = new Borders(
                new Border(
                    new LeftBorder(),
                    new RightBorder(),
                    new TopBorder(),
                    new BottomBorder(),
                    new DiagonalBorder()));

            var cellFormats = new CellFormats(
                new CellFormat { FontId = 0, FillId = 0, BorderId = 0 },
                new CellFormat
                {
                    FontId = 1,
                    FillId = 2,
                    BorderId = 0,
                    ApplyFont = true,
                    ApplyFill = true,
                    Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center, WrapText = true }
                },
                new CellFormat
                {
                    FontId = 2,
                    FillId = 0,
                    BorderId = 0,
                    ApplyFont = true,
                    Alignment = new Alignment { WrapText = true }
                },
                new CellFormat
                {
                    FontId = 3,
                    FillId = 3,
                    BorderId = 0,
                    ApplyFont = true,
                    ApplyFill = true,
                    Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center, WrapText = true }
                },
                new CellFormat
                {
                    FontId = 0,
                    FillId = 4,
                    BorderId = 0,
                    ApplyFill = true,
                    Alignment = new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }
                },
                new CellFormat
                {
                    FontId = 0,
                    FillId = 5,
                    BorderId = 0,
                    ApplyFill = true,
                    Alignment = new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }
                },
                new CellFormat
                {
                    FontId = 0,
                    FillId = 6,
                    BorderId = 0,
                    ApplyFill = true,
                    Alignment = new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }
                },
                new CellFormat
                {
                    FontId = 4,
                    FillId = 7,
                    BorderId = 0,
                    ApplyFont = true,
                    ApplyFill = true,
                    Alignment = new Alignment { Vertical = VerticalAlignmentValues.Center, WrapText = true }
                });

            styleTitle = 1;
            styleSubtitle = 2;
            styleHeader = 3;
            styleDataHigh = 4;
            styleDataMedium = 5;
            styleDataLow = 6;
            styleTotal = 7;

            return new Stylesheet(fonts, fills, borders, cellFormats);
        }

        private static string GetColumnName(int columnIndex1Based)
        {
            string columnName = string.Empty;
            int dividend = columnIndex1Based;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        /// <summary>
        /// Construye el nombre de archivo estándar del reporte QC.
        /// </summary>
        public static string BuildDefaultFileName(string modelName, DateTime timestamp)
        {
            string safe = SanitizeFileNameSegment(modelName);
            if (string.IsNullOrEmpty(safe))
            {
                safe = "Modelo";
            }

            string stamp = timestamp.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
            return $"ControlManager_QC_{safe}_{stamp}.xlsx";
        }

        private static string SanitizeFileNameSegment(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string trimmed = name.Trim();
            string invalid = new string(Path.GetInvalidFileNameChars());
            string pattern = "[" + Regex.Escape(invalid) + "]";
            string cleaned = Regex.Replace(trimmed, pattern, "_");
            cleaned = Regex.Replace(cleaned, @"\s+", "_");
            return cleaned;
        }
    }
}
