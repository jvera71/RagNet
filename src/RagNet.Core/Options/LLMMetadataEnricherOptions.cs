namespace RagNet.Core.Options;

/// <summary>
/// Configuration options for LLMMetadataEnricher.
/// </summary>
public class LLMMetadataEnricherOptions
{
    /// <summary>Extract named entities (people, organizations, places).</summary>
    public bool ExtractEntities { get; set; } = true;

    /// <summary>Extract representative keywords.</summary>
    public bool ExtractKeywords { get; set; } = true;

    /// <summary>Generate a brief summary (1-2 sentences).</summary>
    public bool GenerateSummary { get; set; } = true;

    /// <summary>Detect the language of the content.</summary>
    public bool DetectLanguage { get; set; } = false;

    /// <summary>Classify by topic/category.</summary>
    public bool ClassifyTopic { get; set; } = false;

    /// <summary>Number of chunks to process per LLM call.</summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>Maximum number of concurrent calls to the LLM.</summary>
    public int MaxConcurrency { get; set; } = 3;
}
