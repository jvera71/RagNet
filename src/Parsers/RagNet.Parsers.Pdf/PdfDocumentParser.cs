using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using RagNet.Abstractions;

namespace RagNet.Parsers.Pdf;

/// <summary>
/// Parser for PDF documents using PdfPig.
/// Transforms PDF content into a hierarchical DocumentNode tree based on heuristic font analysis.
/// </summary>
public class PdfDocumentParser : IDocumentParser
{
    /// <summary>
    /// Supported file extensions for this parser.
    /// </summary>
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string> { ".pdf" };

    /// <summary>
    /// Parses a PDF document from a stream and returns its hierarchical representation.
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

            using var pdfDocument = PdfDocument.Open(documentStream);
            var currentSectionStack = new Stack<DocumentNode>();
            currentSectionStack.Push(rootNode);

            foreach (var page in pdfDocument.GetPages())
            {
                ct.ThrowIfCancellationRequested();
                var text = ContentOrderTextExtractor.GetText(page);

                // For a more advanced implementation, we would extract TextBlocks 
                // and analyze font sizes to infer headings vs paragraphs.
                // Here we perform a simplified line-based heuristic extraction.

                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Simplified heuristic: If line is short and title-cased, maybe it's a heading
                    bool isLikelyHeading = line.Length < 60 && !line.EndsWith(".") && !line.EndsWith(",");
                    
                    if (isLikelyHeading)
                    {
                        var sectionNode = new DocumentNode
                        {
                            NodeType = DocumentNodeType.Section,
                            Content = line,
                            Level = 1,
                            Metadata = new Dictionary<string, object> { { "page", page.Number } },
                            Children = new List<DocumentNode>()
                        };

                        ((List<DocumentNode>)rootNode.Children).Add(sectionNode);
                        
                        while(currentSectionStack.Count > 1) currentSectionStack.Pop();
                        currentSectionStack.Push(sectionNode);
                    }
                    else
                    {
                        var pNode = new DocumentNode
                        {
                            NodeType = DocumentNodeType.Paragraph,
                            Content = line,
                            Level = currentSectionStack.Peek().Level + 1,
                            Metadata = new Dictionary<string, object> { { "page", page.Number } },
                            Children = new List<DocumentNode>()
                        };
                        ((List<DocumentNode>)currentSectionStack.Peek().Children).Add(pNode);
                    }
                }
            }

            return rootNode;
        }, ct);
    }
}
