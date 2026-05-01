using RagNet.Abstractions;

namespace RagNet.Core.Retrieval.Transformers;

/// <summary>
/// Combines multiple query transformers, running them sequentially or concurrently,
/// and returns the union of all generated queries.
/// </summary>
public class CompositeQueryTransformer : IQueryTransformer
{
    private readonly IEnumerable<IQueryTransformer> _transformers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeQueryTransformer"/> class.
    /// </summary>
    /// <param name="transformers">The transformers to combine.</param>
    public CompositeQueryTransformer(IEnumerable<IQueryTransformer> transformers)
    {
        _transformers = transformers ?? Enumerable.Empty<IQueryTransformer>();
    }

    /// <summary>
    /// Transforms the query using all registered transformers and returns the unique set of queries.
    /// </summary>
    /// <param name="originalQuery">Original user query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A union of all generated queries.</returns>
    public async Task<IEnumerable<string>> TransformAsync(string originalQuery, CancellationToken ct = default)
    {
        var allQueries = new HashSet<string> { originalQuery };

        foreach (var transformer in _transformers)
        {
            ct.ThrowIfCancellationRequested();
            var transformed = await transformer.TransformAsync(originalQuery, ct);
            allQueries.UnionWith(transformed.Where(q => !string.IsNullOrWhiteSpace(q)));
        }

        return allQueries;
    }
}
