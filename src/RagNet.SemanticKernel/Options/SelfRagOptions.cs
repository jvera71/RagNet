namespace RagNet.SemanticKernel.Options;

/// <summary>
/// Strategy to apply when a hallucination is detected by Self-RAG.
/// </summary>
public enum HallucinationStrategy
{
    /// <summary>Only add a warning in the ExecutionMetadata.</summary>
    Warn,
    
    /// <summary>Filter out non-supported claims from the response.</summary>
    Filter,
    
    /// <summary>Regenerate the response with a more restrictive prompt.</summary>
    Regenerate
}

/// <summary>
/// Configuration options for the Self-RAG validation mechanism.
/// </summary>
public class SelfRagOptions
{
    /// <summary>Enable Self-RAG validation.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Strategy to apply when a hallucination is detected.
    /// </summary>
    public HallucinationStrategy Strategy { get; set; } = HallucinationStrategy.Warn;

    /// <summary>Minimum percentage of supported claims to pass validation.</summary>
    public double MinSupportedRatio { get; set; } = 0.8;
}
