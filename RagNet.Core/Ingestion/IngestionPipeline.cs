using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using RagNet.Abstractions;
using RagNet.Core.Models;

namespace RagNet.Core.Ingestion;

/// <summary>
/// Default implementation of the ingestion pipeline that orchestrates
/// parsing, chunking, enrichment, embedding, and storage.
/// </summary>
public class IngestionPipeline : IIngestionPipeline
{
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly ISemanticChunker _chunker;
    private readonly IMetadataEnricher? _enricher;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private readonly IVectorStoreRecordCollection<string, DefaultRagVectorRecord> _vectorCollection;
    private readonly EmbeddingBatcher _embeddingBatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipeline"/> class.
    /// </summary>
    public IngestionPipeline(
        IEnumerable<IDocumentParser> parsers,
        ISemanticChunker chunker,
        IVectorStoreRecordCollection<string, DefaultRagVectorRecord> vectorCollection,
        IMetadataEnricher? enricher = null,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null,
        EmbeddingBatcher? embeddingBatcher = null)
    {
        _parsers = parsers;
        _chunker = chunker;
        _vectorCollection = vectorCollection;
        _enricher = enricher;
        _embeddingGenerator = embeddingGenerator;
        _embeddingBatcher = embeddingBatcher ?? new EmbeddingBatcher();
    }

    /// <inheritdoc/>
    public async Task<IngestionResult> IngestAsync(Stream documentStream, string fileName, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // 1. Resolve parser
        var parser = _parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(extension));
        if (parser == null)
        {
            return new IngestionResult
            {
                ChunkCount = 0,
                Duration = stopwatch.Elapsed,
                Errors = new[] { $"No parser found for extension '{extension}'" }
            };
        }

        try
        {
            // 2. Parse
            var documentNode = await parser.ParseAsync(documentStream, fileName, ct);

            // 3. Chunk
            var chunks = await _chunker.ChunkAsync(documentNode, ct);
            var chunkList = chunks.ToList();

            // 4. Enrich
            if (_enricher != null)
            {
                var enriched = await _enricher.EnrichAsync(chunkList, ct);
                chunkList = enriched.ToList();
            }

            // 5. Embed
            if (_embeddingGenerator != null)
            {
                var embedded = await _embeddingBatcher.EmbedInBatchesAsync(chunkList, _embeddingGenerator, ct);
                chunkList = embedded.ToList();
            }

            // 6. Map to Vector Records
            var mappedRecords = chunkList.Select(MapToVectorRecord);

            // 7. Store
            // Make sure collection exists
            await _vectorCollection.CreateCollectionIfNotExistsAsync(cancellationToken: ct);

            // Upsert documents
            foreach (var record in mappedRecords)
            {
                ct.ThrowIfCancellationRequested();
                await _vectorCollection.UpsertAsync(record, cancellationToken: ct);
            }

            stopwatch.Stop();
            return new IngestionResult
            {
                ChunkCount = chunkList.Count,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new IngestionResult
            {
                ChunkCount = 0,
                Duration = stopwatch.Elapsed,
                Errors = new[] { $"Ingestion failed: {ex.Message}" }
            };
        }
    }

    private DefaultRagVectorRecord MapToVectorRecord(RagDocument doc)
    {
        doc.Metadata.TryGetValue("source", out var source);
        doc.Metadata.TryGetValue("section", out var section);
        doc.Metadata.TryGetValue("summary", out var summary);
        
        string keywords = string.Empty;
        if (doc.Metadata.TryGetValue("keywords", out var kwObj) && kwObj is string[] kwArray)
        {
            keywords = string.Join(", ", kwArray);
        }

        string entitiesJson = string.Empty;
        if (doc.Metadata.TryGetValue("entities", out var entObj))
        {
            entitiesJson = System.Text.Json.JsonSerializer.Serialize(entObj);
        }

        string metadataJson = System.Text.Json.JsonSerializer.Serialize(doc.Metadata);

        return new DefaultRagVectorRecord
        {
            Id = doc.Id,
            Content = doc.Content,
            Source = source?.ToString() ?? string.Empty,
            Section = section?.ToString() ?? string.Empty,
            Keywords = keywords,
            Summary = summary?.ToString() ?? string.Empty,
            EntitiesJson = entitiesJson,
            MetadataJson = metadataJson,
            Vector = doc.Vector
        };
    }
}
