using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using RagNet.Abstractions;

namespace RagNet.Parsers.Office;

/// <summary>
/// Parser for Excel documents (.xlsx) using OpenXml.
/// Transforms Excel content into a hierarchical DocumentNode tree where each sheet is a section.
/// </summary>
public class ExcelDocumentParser : IDocumentParser
{
    /// <summary>
    /// Supported file extensions for this parser.
    /// </summary>
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string> { ".xlsx" };

    /// <summary>
    /// Parses an Excel document from a stream and returns its hierarchical representation.
    /// </summary>
    /// <param name="documentStream">Stream of the source file.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Root node of the document's hierarchical tree.</returns>
    public async Task<DocumentNode> ParseAsync(Stream documentStream, string fileName, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var rootNode = new DocumentNode
            {
                NodeType = DocumentNodeType.Document,
                Content = fileName,
                Level = 0,
                Metadata = new Dictionary<string, object> { { "fileName", fileName } },
                Children = new List<DocumentNode>()
            };

            using var spreadsheetDocument = SpreadsheetDocument.Open(documentStream, false);
            var workbookPart = spreadsheetDocument.WorkbookPart;
            if (workbookPart == null) return rootNode;

            var sharedStringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
            var sharedStringTable = sharedStringTablePart?.SharedStringTable;

            var sheets = workbookPart.Workbook?.Sheets?.Elements<Sheet>();
            if (sheets == null) return rootNode;

            foreach (var sheet in sheets)
            {
                ct.ThrowIfCancellationRequested();
                var sheetName = sheet.Name?.Value ?? "Unknown Sheet";

                var sectionNode = new DocumentNode
                {
                    NodeType = DocumentNodeType.Section,
                    Content = $"Sheet: {sheetName}",
                    Level = 1,
                    Children = new List<DocumentNode>()
                };

                var worksheetPart = (WorksheetPart?)workbookPart.GetPartById(sheet.Id!);
                var sheetData = worksheetPart?.Worksheet?.Elements<SheetData>().FirstOrDefault();

                if (sheetData != null)
                {
                    var tableNode = ParseSheetData(sheetData, sharedStringTable);
                    if (tableNode != null)
                    {
                        ((List<DocumentNode>)sectionNode.Children).Add(tableNode);
                    }
                }

                ((List<DocumentNode>)rootNode.Children).Add(sectionNode);
            }

            return rootNode;
        }, ct);
    }

    private DocumentNode? ParseSheetData(SheetData sheetData, SharedStringTable? sharedStringTable)
    {
        var tableContent = new StringBuilder();
        var rows = sheetData.Elements<Row>().ToList();
        
        if (rows.Count == 0) return null;

        foreach (var row in rows)
        {
            var rowContent = new List<string>();
            foreach (var cell in row.Elements<Cell>())
            {
                var cellValue = GetCellValue(cell, sharedStringTable);
                rowContent.Add(cellValue);
            }
            if (rowContent.Any(c => !string.IsNullOrWhiteSpace(c)))
            {
                tableContent.AppendLine(string.Join(" | ", rowContent));
            }
        }

        if (tableContent.Length == 0) return null;

        return new DocumentNode
        {
            NodeType = DocumentNodeType.Table,
            Content = tableContent.ToString().Trim(),
            Level = 2,
            Children = new List<DocumentNode>()
        };
    }

    private string GetCellValue(Cell cell, SharedStringTable? sharedStringTable)
    {
        var value = cell.CellValue?.Text ?? string.Empty;

        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            if (sharedStringTable != null && int.TryParse(value, out int index))
            {
                value = sharedStringTable.ElementAt(index).InnerText;
            }
        }

        return value;
    }
}
