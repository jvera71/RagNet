namespace RagNet.Abstractions;

/// <summary>
/// Generates synthesized responses from retrieved documents
/// and the user query, using a language model.
/// </summary>
public interface IRagGenerator
{
    /// <summary>
    /// Generates a complete response based on the provided context.
    /// </summary>
    /// <param name="query">Original user query.</param>
    /// <param name="context">Relevant retrieved and reordered documents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete response with citations.</returns>
    Task<RagResponse> GenerateAsync(
        string query,
        IEnumerable<RagDocument> context,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a response with token streaming.
    /// </summary>
    /// <param name="query">Original user query.</param>
    /// <param name="context">Relevant retrieved and reordered documents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Stream of response fragments.</returns>
    IAsyncEnumerable<StreamingRagResponse> GenerateStreamingAsync(
        string query,
        IEnumerable<RagDocument> context,
        CancellationToken ct = default);
}
