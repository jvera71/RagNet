namespace RagNet.Core.Options;

/// <summary>
/// Configuration options for the HybridRetriever.
/// </summary>
public class HybridRetrieverOptions
{
    /// <summary>
    /// Balance between vector and keyword (0.0 = only keyword, 1.0 = only vector).
    /// Value 0.5 = equal weight for both strategies.
    /// </summary>
    public double Alpha { get; set; } = 0.5;

    /// <summary>
    /// Expanded Top-K for each sub-retriever before fusion.
    /// Must be greater than the final Top-K to have enough candidates.
    /// </summary>
    public int ExpandedTopK { get; set; } = 20;

    /// <summary>
    /// Constant K for the RRF algorithm. Typical values: 60.
    /// Controls how much documents with lower ranks are penalized.
    /// </summary>
    public int RrfK { get; set; } = 60;
}
