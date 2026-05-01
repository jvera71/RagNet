namespace RagNet.Abstractions;

/// <summary>
/// Transforms the user's original query into one or more optimized queries
/// to improve recall in document retrieval.
/// </summary>
public interface IQueryTransformer
{
    /// <summary>
    /// Transforms an original query into optimized queries.
    /// </summary>
    /// <param name="originalQuery">Original user query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>One or more transformed queries.</returns>
    Task<IEnumerable<string>> TransformAsync(
        string originalQuery,
        CancellationToken ct = default);
}
