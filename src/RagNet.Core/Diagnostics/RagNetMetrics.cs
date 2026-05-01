using System.Diagnostics.Metrics;

namespace RagNet.Core.Diagnostics;

/// <summary>
/// Centralized metrics for RagNet components.
/// </summary>
public static class RagNetMetrics
{
    private static readonly Meter Meter = new("RagNet", "1.0.0");

    // Counters
    
    /// <summary>Total processed queries.</summary>
    public static readonly Counter<long> QueriesProcessed =
        Meter.CreateCounter<long>(
            "ragnet.queries.processed",
            description: "Total number of queries processed");

    /// <summary>Total ingested documents.</summary>
    public static readonly Counter<long> DocumentsIngested =
        Meter.CreateCounter<long>(
            "ragnet.documents.ingested",
            description: "Total number of ingested documents");

    /// <summary>Total chunks created.</summary>
    public static readonly Counter<long> ChunksCreated =
        Meter.CreateCounter<long>(
            "ragnet.chunks.created",
            description: "Total number of chunks created");

    /// <summary>Total LLM calls made.</summary>
    public static readonly Counter<long> LlmCallsTotal =
        Meter.CreateCounter<long>(
            "ragnet.llm.calls.total",
            description: "Total number of LLM calls");

    // Histograms
    
    /// <summary>Latency of the query pipeline.</summary>
    public static readonly Histogram<double> QueryLatency =
        Meter.CreateHistogram<double>(
            "ragnet.query.duration",
            unit: "ms",
            description: "Latency of the query pipeline");

    /// <summary>Latency of the ingestion pipeline.</summary>
    public static readonly Histogram<double> IngestionLatency =
        Meter.CreateHistogram<double>(
            "ragnet.ingestion.duration",
            unit: "ms",
            description: "Latency of the ingestion pipeline");

    /// <summary>Number of documents retrieved per query.</summary>
    public static readonly Histogram<int> RetrievedDocumentCount =
        Meter.CreateHistogram<int>(
            "ragnet.retrieval.document_count",
            description: "Number of documents retrieved per query");

    /// <summary>Score of the most relevant document.</summary>
    public static readonly Histogram<double> TopRelevanceScore =
        Meter.CreateHistogram<double>(
            "ragnet.retrieval.top_score",
            description: "Score of the most relevant document");

    /// <summary>Tokens consumed per generation.</summary>
    public static readonly Histogram<int> TokensConsumed =
        Meter.CreateHistogram<int>(
            "ragnet.generation.tokens",
            description: "Tokens consumed per generation");
}
