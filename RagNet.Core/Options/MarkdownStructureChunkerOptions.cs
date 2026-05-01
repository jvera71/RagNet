namespace RagNet.Core.Options;

/// <summary>
/// Configuration options for MarkdownStructureChunker.
/// </summary>
public class MarkdownStructureChunkerOptions
{
    /// <summary>Heading level that defines the chunk boundary (2 = H2).</summary>
    public int ChunkAtHeadingLevel { get; set; } = 2;

    /// <summary>Maximum size before subdividing by sub-sections.</summary>
    public int MaxChunkSize { get; set; } = 2000;

    /// <summary>Minimum size to merge with the adjacent section.</summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// Include the breadcrumb of parent titles as a prefix.
    /// Example: "Manual > Installation > Requirements"
    /// </summary>
    public bool IncludeBreadcrumb { get; set; } = true;
}
