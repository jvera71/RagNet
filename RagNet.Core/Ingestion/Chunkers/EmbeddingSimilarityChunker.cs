using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using RagNet.Abstractions;
using RagNet.Core.Options;

namespace RagNet.Core.Ingestion.Chunkers;

/// <summary>
/// Groups consecutive sentences or paragraphs based on the cosine similarity
/// of their embeddings. Creates a new chunk when similarity drops below a threshold.
/// </summary>
public class EmbeddingSimilarityChunker : ISemanticChunker
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly EmbeddingSimilarityChunkerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingSimilarityChunker"/> class.
    /// </summary>
    /// <param name="embeddingGenerator">Generator used to create embeddings for similarity comparison.</param>
    /// <param name="options">The chunker options.</param>
    public EmbeddingSimilarityChunker(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<EmbeddingSimilarityChunkerOptions> options)
    {
        _embeddingGenerator = embeddingGenerator;
        _options = options.Value;
    }

    /// <summary>
    /// Splits a parsed document into semantic fragments using embedding similarity.
    /// </summary>
    /// <param name="rootNode">Root node of the parsed document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of RAG documents (chunks) without assigned vectors.</returns>
    public async Task<IEnumerable<RagDocument>> ChunkAsync(DocumentNode rootNode, CancellationToken ct = default)
    {
        var paragraphs = FlattenToParagraphs(rootNode);
        if (!paragraphs.Any()) return Enumerable.Empty<RagDocument>();

        // Generate embeddings for all paragraphs
        var textsToEmbed = paragraphs.Select(p => p.text).ToList();
        var embeddings = await _embeddingGenerator.GenerateAsync(textsToEmbed, cancellationToken: ct);

        var chunks = new List<RagDocument>();
        var currentChunkTexts = new List<string>();
        int currentLength = 0;
        
        for (int i = 0; i < paragraphs.Count; i++)
        {
            var text = paragraphs[i].text;
            
            if (currentChunkTexts.Count == 0)
            {
                currentChunkTexts.Add(text);
                currentLength += text.Length;
                continue;
            }

            // Compare with previous embedding
            // In a more advanced implementation, this would compare against a rolling window average
            float similarity = CalculateCosineSimilarity(embeddings[i - 1].Vector.Span, embeddings[i].Vector.Span);

            bool isBreakPoint = similarity < _options.SimilarityThreshold;
            bool isTooLarge = currentLength + text.Length > _options.MaxChunkSize;

            if (isBreakPoint || isTooLarge)
            {
                // Ensure minimum size if possible (simplified logic)
                if (currentLength >= _options.MinChunkSize || isTooLarge)
                {
                    chunks.Add(CreateRagDocument(currentChunkTexts, paragraphs[i - 1].contextTitle));
                    currentChunkTexts.Clear();
                    currentLength = 0;
                }
            }

            currentChunkTexts.Add(text);
            currentLength += text.Length;
        }

        // Add remaining
        if (currentChunkTexts.Count > 0)
        {
            chunks.Add(CreateRagDocument(currentChunkTexts, paragraphs.Last().contextTitle));
        }

        return chunks;
    }

    private RagDocument CreateRagDocument(List<string> texts, string contextTitle)
    {
        return new RagDocument(
            Id: Guid.NewGuid().ToString(),
            Content: string.Join("\n", texts),
            Vector: ReadOnlyMemory<float>.Empty,
            Metadata: new Dictionary<string, object>
            {
                { "chunkType", "embeddingSimilarity" },
                { "contextTitle", contextTitle }
            }
        );
    }

    private float CalculateCosineSimilarity(ReadOnlySpan<float> vector1, ReadOnlySpan<float> vector2)
    {
        if (vector1.Length != vector2.Length) return 0f;

        float dotProduct = 0f;
        float norm1 = 0f;
        float norm2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            norm1 += vector1[i] * vector1[i];
            norm2 += vector2[i] * vector2[i];
        }

        if (norm1 == 0f || norm2 == 0f) return 0f;

        return (float)(dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2)));
    }

    private List<(string text, string contextTitle)> FlattenToParagraphs(DocumentNode node, string currentTitle = "")
    {
        var result = new List<(string, string)>();

        if (node.NodeType == DocumentNodeType.Heading || node.NodeType == DocumentNodeType.Section)
        {
            currentTitle = node.Content;
        }
        else if (node.NodeType == DocumentNodeType.Paragraph || node.NodeType == DocumentNodeType.ListItem)
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
