namespace RagNet.Abstractions;

/// <summary>
/// Reorders a list of candidate documents based on their actual relevance
/// to the query, using more precise (but more expensive) models
/// than the initial vector search.
/// </summary>
public interface IDocumentReranker
{
    /// <summary>
    /// Reorders documents by relevance and returns the top-K best results.
    /// </summary>
    /// <param name="query">Original user query.</param>
    /// <param name="documents">Candidate documents to reorder.</param>
    /// <param name="topK">Maximum number of documents to return after reordering.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Top-K documents reordered by descending relevance.</returns>
    Task<IEnumerable<RagDocument>> RerankAsync(
        string query,
        IEnumerable<RagDocument> documents,
        int topK,
        CancellationToken ct = default);
}
