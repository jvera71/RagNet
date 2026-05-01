using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RagNet.Abstractions;

namespace RagNet.Parsers.Office;

/// <summary>
/// Parser for Word documents (.docx) using OpenXml.
/// Transforms Word content into a hierarchical DocumentNode tree based on heading styles.
/// </summary>
public class WordDocumentParser : IDocumentParser
{
    /// <summary>
    /// Supported file extensions for this parser.
    /// </summary>
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string> { ".docx" };

    /// <summary>
    /// Parses a Word document from a stream and returns its hierarchical representation.
    /// </summary>
    /// <param name="documentStream">Stream of the source file.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Root node of the document's hierarchical tree.</returns>
    public async Task<DocumentNode> ParseAsync(Stream documentStream, string fileName, CancellationToken ct = default)
    {
        // Run synchronously as OpenXml stream loading doesn't have an async equivalent that avoids blocking in the same way,
        // but wrap it in Task.Run to not block the calling thread if it's large.
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

            var currentSectionStack = new Stack<DocumentNode>();
            currentSectionStack.Push(rootNode);

            using var wordDocument = WordprocessingDocument.Open(documentStream, false);
            var body = wordDocument.MainDocumentPart?.Document?.Body;

            if (body == null) return rootNode;

            foreach (var element in body.Elements())
            {
                ct.ThrowIfCancellationRequested();

                if (element is Paragraph paragraph)
                {
                    ProcessParagraph(paragraph, currentSectionStack);
                }
                else if (element is Table table)
                {
                    ProcessTable(table, currentSectionStack);
                }
            }

            return rootNode;
        }, ct);
    }

    private void ProcessParagraph(Paragraph paragraph, Stack<DocumentNode> sectionStack)
    {
        var text = paragraph.InnerText;
        if (string.IsNullOrWhiteSpace(text)) return;

        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        int level = 0;

        if (style != null && style.StartsWith("Heading"))
        {
            if (int.TryParse(style.Replace("Heading", ""), out int parsedLevel))
            {
                level = parsedLevel;
            }
        }

        if (level > 0)
        {
            // Heading detected
            while (sectionStack.Count > 1 && sectionStack.Peek().Level >= level)
            {
                sectionStack.Pop();
            }

            var headingNode = new DocumentNode
            {
                NodeType = DocumentNodeType.Heading,
                Content = text,
                Level = level,
                Children = new List<DocumentNode>()
            };

            var sectionNode = new DocumentNode
            {
                NodeType = DocumentNodeType.Section,
                Content = text,
                Level = level,
                Children = new List<DocumentNode> { headingNode }
            };

            ((List<DocumentNode>)sectionStack.Peek().Children).Add(sectionNode);
            sectionStack.Push(sectionNode);
        }
        else
        {
            // Regular paragraph or list
            var nodeType = paragraph.ParagraphProperties?.NumberingProperties != null ? DocumentNodeType.ListItem : DocumentNodeType.Paragraph;
            
            var node = new DocumentNode
            {
                NodeType = nodeType,
                Content = text,
                Level = sectionStack.Peek().Level + 1,
                Children = new List<DocumentNode>()
            };

            ((List<DocumentNode>)sectionStack.Peek().Children).Add(node);
        }
    }

    private void ProcessTable(Table table, Stack<DocumentNode> sectionStack)
    {
        var tableContent = new StringBuilder();
        foreach (var row in table.Elements<TableRow>())
        {
            var rowContent = new List<string>();
            foreach (var cell in row.Elements<TableCell>())
            {
                rowContent.Add(cell.InnerText.Trim());
            }
            tableContent.AppendLine(string.Join(" | ", rowContent));
        }

        if (tableContent.Length > 0)
        {
            var node = new DocumentNode
            {
                NodeType = DocumentNodeType.Table,
                Content = tableContent.ToString().Trim(),
                Level = sectionStack.Peek().Level + 1,
                Children = new List<DocumentNode>()
            };
            ((List<DocumentNode>)sectionStack.Peek().Children).Add(node);
        }
    }
}
