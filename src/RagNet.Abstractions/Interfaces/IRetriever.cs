namespace RagNet.Abstractions;

/// <summary>
/// Retrieves relevant documents from vector or full-text storage,
/// given a query and a maximum number of results.
/// </summary>
public interface IRetriever
{
    /// <summary>
    /// Searches for and returns the most relevant documents for the given query.
    /// </summary>
    /// <param name="query">Search query (text or embedding depending on implementation).</param>
    /// <param name="topK">Maximum number of documents to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Documents ordered by descending relevance.</returns>
    Task<IEnumerable<RagDocument>> RetrieveAsync(
        string query,
        int topK,
        CancellationToken ct = default);
}
