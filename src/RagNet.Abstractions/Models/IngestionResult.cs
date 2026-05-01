namespace RagNet.Abstractions;

/// <summary>
/// Result of a document ingestion process.
/// </summary>
public record IngestionResult
{
    /// <summary>Number of chunks successfully processed and stored.</summary>
    public int ChunkCount { get; init; }

    /// <summary>Total duration of the ingestion process.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Any errors or warnings encountered during ingestion.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
