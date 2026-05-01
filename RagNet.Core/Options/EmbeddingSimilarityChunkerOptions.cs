namespace RagNet.Core.Options;

/// <summary>
/// Configuration options for EmbeddingSimilarityChunker.
/// </summary>
public class EmbeddingSimilarityChunkerOptions
{
    /// <summary>
    /// Cosine similarity threshold (0.0-1.0). Values below
    /// this threshold indicate a topic change -> new chunk.
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.85;

    /// <summary>Maximum chunk size in characters.</summary>
    public int MaxChunkSize { get; set; } = 1500;

    /// <summary>Minimum chunk size in characters.</summary>
    public int MinChunkSize { get; set; } = 200;

    /// <summary>
    /// Size of the sliding window to calculate similarity
    /// (compares with the group average, not just the last element).
    /// </summary>
    public int WindowSize { get; set; } = 3;
}
