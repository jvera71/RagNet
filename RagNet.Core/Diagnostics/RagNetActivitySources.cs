using System.Diagnostics;

namespace RagNet.Core.Diagnostics;

/// <summary>
/// Centralized activity sources for all RagNet instrumentation.
/// </summary>
public static class RagNetActivitySources
{
    /// <summary>Root name for all RagNet activities.</summary>
    public const string RootName = "RagNet";

    /// <summary>Source for the main pipeline operations.</summary>
    public static readonly ActivitySource Pipeline = new($"{RootName}.Pipeline", "1.0.0");

    /// <summary>Source for ingestion operations.</summary>
    public static readonly ActivitySource Ingestion = new($"{RootName}.Ingestion", "1.0.0");

    /// <summary>Source for retrieval operations.</summary>
    public static readonly ActivitySource Retrieval = new($"{RootName}.Retrieval", "1.0.0");

    /// <summary>Source for reranking operations.</summary>
    public static readonly ActivitySource Reranking = new($"{RootName}.Reranking", "1.0.0");

    /// <summary>Source for generation operations.</summary>
    public static readonly ActivitySource Generation = new($"{RootName}.Generation", "1.0.0");

    /// <summary>
    /// Gets all source names for OpenTelemetry registration.
    /// </summary>
    public static IEnumerable<string> AllSourceNames => new[]
    {
        $"{RootName}.Pipeline",
        $"{RootName}.Ingestion",
        $"{RootName}.Retrieval",
        $"{RootName}.Reranking",
        $"{RootName}.Generation"
    };
}
