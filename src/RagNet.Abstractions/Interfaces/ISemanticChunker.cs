namespace RagNet.Abstractions;

/// <summary>
/// Analyzes the hierarchical structure of a parsed document and splits it
/// into semantically coherent fragments (chunks), ready to be
/// embedded and stored.
/// </summary>
public interface ISemanticChunker
{
    /// <summary>
    /// Splits a parsed document into semantic fragments.
    /// </summary>
    /// <param name="rootNode">Root node of the parsed document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of RAG documents (chunks) without assigned vectors.</returns>
    Task<IEnumerable<RagDocument>> ChunkAsync(
        DocumentNode rootNode,
        CancellationToken ct = default);
}
