namespace RagNet.Core.Options;

/// <summary>
/// Configuration options for NLPBoundaryChunker.
/// </summary>
public class NLPBoundaryChunkerOptions
{
    /// <summary>Maximum chunk size in characters.</summary>
    public int MaxChunkSize { get; set; } = 1000;

    /// <summary>Minimum chunk size in characters.</summary>
    public int MinChunkSize { get; set; } = 200;

    /// <summary>Number of overlapping sentences between consecutive chunks.</summary>
    public int OverlapSentences { get; set; } = 2;

    /// <summary>Include the parent section title as a chunk prefix.</summary>
    public bool IncludeSectionTitle { get; set; } = true;
}
