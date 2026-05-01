namespace RagNet.Abstractions;

/// <summary>
/// Partial fragment of a RAG response, emitted during streaming.
/// Allows sending tokens to the client while the LLM generates the response.
/// </summary>
public record StreamingRagResponse
{
    /// <summary>Text fragment (token or group of tokens).</summary>
    public required string ContentFragment { get; init; }

    /// <summary>Indicates if this is the last fragment of the response.</summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Citations available so far. Populated progressively
    /// or only in the final fragment (IsComplete = true).
    /// </summary>
    public IReadOnlyList<Citation>? Citations { get; init; }
}
