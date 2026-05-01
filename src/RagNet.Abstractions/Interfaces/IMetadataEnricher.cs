namespace RagNet.Abstractions;

/// <summary>
/// Enriches chunk metadata using automatic analysis
/// (typically via LLM). Extracts entities, keywords, summaries, etc.
/// </summary>
public interface IMetadataEnricher
{
    /// <summary>
    /// Enriches a batch of documents with automatically extracted additional metadata.
    /// </summary>
    /// <param name="documents">Documents to enrich.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Documents with enriched metadata in their Metadata dictionary.</returns>
    Task<IEnumerable<RagDocument>> EnrichAsync(
        IEnumerable<RagDocument> documents,
        CancellationToken ct = default);
}
