using RagNet.Abstractions;

namespace RagNet.Core.Pipeline;

/// <summary>
/// Mutable context that flows through the RAG pipeline.
/// Each step can read and modify the state.
/// </summary>
public class RagPipelineContext
{
    /// <summary>Original user query.</summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>Transformed queries (after IQueryTransformer).</summary>
    public IEnumerable<string> TransformedQueries { get; set; } = Enumerable.Empty<string>();

    /// <summary>Retrieved documents (after IRetriever).</summary>
    public IEnumerable<RagDocument> RetrievedDocuments { get; set; } = Enumerable.Empty<RagDocument>();

    /// <summary>Ranked documents (after IDocumentReranker).</summary>
    public IEnumerable<RagDocument> RankedDocuments { get; set; } = Enumerable.Empty<RagDocument>();

    /// <summary>Generated response (after IRagGenerator).</summary>
    public RagResponse? Response { get; set; }

    /// <summary>Cancellation token.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>Additional properties for custom steps.</summary>
    public Dictionary<string, object> Properties { get; } = new();
}
