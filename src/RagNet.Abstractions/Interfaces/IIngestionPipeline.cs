namespace RagNet.Abstractions;

/// <summary>
/// Orchestrates the ingestion of documents: parsing, chunking, enrichment,
/// embedding generation, and vector storage.
/// </summary>
public interface IIngestionPipeline
{
    /// <summary>
    /// Ingests a document from a stream through the complete pipeline.
    /// </summary>
    /// <param name="documentStream">Stream of the source file.</param>
    /// <param name="fileName">Name of the file (used to determine format).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the ingestion process.</returns>
    Task<IngestionResult> IngestAsync(Stream documentStream, string fileName, CancellationToken ct = default);
}
