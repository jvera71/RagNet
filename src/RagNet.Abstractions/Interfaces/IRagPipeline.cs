namespace RagNet.Abstractions;

/// <summary>
/// Complete RAG pipeline that orchestrates transformation, retrieval,
/// reordering, and generation in a unified flow.
/// Supports both complete execution and streaming.
/// </summary>
public interface IRagPipeline
{
    /// <summary>
    /// Executes the full RAG pipeline and returns the final response.
    /// </summary>
    /// <param name="query">User query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete response with citations and execution metadata.</returns>
    Task<RagResponse> ExecuteAsync(
        string query,
        CancellationToken ct = default);

    /// <summary>
    /// Executes the RAG pipeline with response streaming.
    /// Fragments are emitted as the LLM generates tokens.
    /// </summary>
    /// <param name="query">User query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async stream of response fragments.</returns>
    IAsyncEnumerable<StreamingRagResponse> ExecuteStreamingAsync(
        string query,
        CancellationToken ct = default);
}
