using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RagNet.Abstractions;
using RagNet.Core.Options;

namespace RagNet.Core.Ingestion.Chunkers;

/// <summary>
/// Splits a parsed document into semantic fragments based on natural language boundaries
/// (sentences, paragraphs) to determine where to cut.
/// </summary>
public class NLPBoundaryChunker : ISemanticChunker
{
    private readonly NLPBoundaryChunkerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NLPBoundaryChunker"/> class.
    /// </summary>
    /// <param name="options">The chunker options.</param>
    public NLPBoundaryChunker(IOptions<NLPBoundaryChunkerOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Splits a parsed document into semantic fragments.
    /// </summary>
    /// <param name="rootNode">Root node of the parsed document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of RAG documents (chunks) without assigned vectors.</returns>
    public Task<IEnumerable<RagDocument>> ChunkAsync(DocumentNode rootNode, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var chunks = new List<RagDocument>();
            var paragraphs = FlattenToParagraphs(rootNode);

            var currentSentences = new List<string>();
            int currentLength = 0;
            string currentContextTitle = "";

            foreach (var (paragraph, contextTitle) in paragraphs)
            {
                ct.ThrowIfCancellationRequested();

                // Simple sentence splitting based on punctuation.
                // In a production scenario, a more robust NLP sentence tokenizer should be used.
                var sentences = Regex.Split(paragraph, @"(?<=[\.!\?])\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                foreach (var sentence in sentences)
                {
                    if (string.IsNullOrEmpty(currentContextTitle))
                    {
                        currentContextTitle = contextTitle;
                    }

                    // If adding this sentence exceeds MaxChunkSize, flush the buffer
                    if (currentLength + sentence.Length > _options.MaxChunkSize && currentSentences.Count > 0)
                    {
                        FlushBuffer(chunks, currentSentences, currentContextTitle);
                        
                        // Keep overlap sentences
                        int overlapCount = Math.Min(_options.OverlapSentences, currentSentences.Count);
                        var overlap = currentSentences.Skip(currentSentences.Count - overlapCount).ToList();
                        
                        currentSentences.Clear();
                        currentSentences.AddRange(overlap);
                        currentLength = overlap.Sum(s => s.Length);
                        currentContextTitle = contextTitle; // Update context for new chunk
                    }

                    currentSentences.Add(sentence);
                    currentLength += sentence.Length;
                }
            }

            // Flush remaining if > MinChunkSize or if it's the only content
            if (currentSentences.Count > 0 && (currentLength >= _options.MinChunkSize || chunks.Count == 0))
            {
                FlushBuffer(chunks, currentSentences, currentContextTitle);
            }

            return (IEnumerable<RagDocument>)chunks;
        }, ct);
    }

    private void FlushBuffer(List<RagDocument> chunks, List<string> sentences, string contextTitle)
    {
        var content = string.Join(" ", sentences);
        
        if (_options.IncludeSectionTitle && !string.IsNullOrEmpty(contextTitle))
        {
            content = $"[{contextTitle}] {content}";
        }

        var document = new RagDocument(
            Id: Guid.NewGuid().ToString(),
            Content: content,
            Vector: ReadOnlyMemory<float>.Empty,
            Metadata: new Dictionary<string, object>
            {
                { "chunkType", "nlpBoundary" },
                { "contextTitle", contextTitle }
            });

        chunks.Add(document);
    }

    private List<(string text, string contextTitle)> FlattenToParagraphs(DocumentNode node, string currentTitle = "")
    {
        var result = new List<(string, string)>();

        if (node.NodeType == DocumentNodeType.Heading || node.NodeType == DocumentNodeType.Section)
        {
            currentTitle = node.Content;
        }
        else if (node.NodeType == DocumentNodeType.Paragraph || node.NodeType == DocumentNodeType.ListItem || node.NodeType == DocumentNodeType.Table)
        {
            if (!string.IsNullOrWhiteSpace(node.Content))
            {
                result.Add((node.Content, currentTitle));
            }
        }

        foreach (var child in node.Children)
        {
            result.AddRange(FlattenToParagraphs(child, currentTitle));
        }

        return result;
    }
}
