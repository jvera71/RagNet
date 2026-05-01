using System.Text;
using Markdig;
using Markdig.Syntax;
using RagNet.Abstractions;

namespace RagNet.Parsers.Markdown;

/// <summary>
/// Parser for Markdown documents using Markdig.
/// Transforms Markdown content into a hierarchical DocumentNode tree.
/// </summary>
public class MarkdownDocumentParser : IDocumentParser
{
    /// <summary>
    /// Supported file extensions for this parser.
    /// </summary>
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string> { ".md", ".markdown" };

    /// <summary>
    /// Parses a Markdown document from a stream and returns its hierarchical representation.
    /// </summary>
    /// <param name="documentStream">Stream of the source file.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Root node of the document's hierarchical tree.</returns>
    public async Task<DocumentNode> ParseAsync(Stream documentStream, string fileName, CancellationToken ct = default)
    {
        using var reader = new StreamReader(documentStream, Encoding.UTF8);
        var markdownText = await reader.ReadToEndAsync(ct);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var markdownDocument = Markdig.Markdown.Parse(markdownText, pipeline);

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

        foreach (var block in markdownDocument)
        {
            ProcessBlock(block, currentSectionStack);
        }

        return rootNode;
    }

    private void ProcessBlock(Block block, Stack<DocumentNode> sectionStack)
    {
        var content = ExtractTextFromBlock(block);
        if (string.IsNullOrWhiteSpace(content)) return;

        DocumentNode? newNode = null;

        if (block is HeadingBlock headingBlock)
        {
            var level = headingBlock.Level;
            
            // Pop sections from stack until we find a parent with a lower level
            while (sectionStack.Count > 1 && sectionStack.Peek().Level >= level)
            {
                sectionStack.Pop();
            }

            newNode = new DocumentNode
            {
                NodeType = DocumentNodeType.Heading,
                Content = content,
                Level = level,
                Children = new List<DocumentNode>()
            };

            var sectionNode = new DocumentNode
            {
                NodeType = DocumentNodeType.Section,
                Content = content,
                Level = level,
                Children = new List<DocumentNode> { newNode }
            };

            ((List<DocumentNode>)sectionStack.Peek().Children).Add(sectionNode);
            sectionStack.Push(sectionNode);
        }
        else
        {
            var nodeType = block switch
            {
                ParagraphBlock => DocumentNodeType.Paragraph,
                ListBlock => DocumentNodeType.List,
                FencedCodeBlock => DocumentNodeType.CodeBlock,
                QuoteBlock => DocumentNodeType.Quote,
                _ => DocumentNodeType.Paragraph
            };

            newNode = new DocumentNode
            {
                NodeType = nodeType,
                Content = content,
                Level = sectionStack.Peek().Level + 1,
                Children = new List<DocumentNode>()
            };

            ((List<DocumentNode>)sectionStack.Peek().Children).Add(newNode);
        }
    }

    private string ExtractTextFromBlock(Block block)
    {
        if (block is LeafBlock leafBlock && leafBlock.Lines.Lines != null)
        {
            var sb = new StringBuilder();
            foreach (var line in leafBlock.Lines.Lines)
            {
                if (line.Slice.Text != null)
                {
                    sb.AppendLine(line.Slice.ToString());
                }
            }
            return sb.ToString().Trim();
        }
        
        // For container blocks like lists, quote blocks, a more recursive extraction would be needed.
        // Simplified here for the core structure.
        return string.Empty; 
    }
}
