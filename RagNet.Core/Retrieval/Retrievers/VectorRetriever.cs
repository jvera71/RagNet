using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using RagNet.Abstractions;
using RagNet.Core.Models;

namespace RagNet.Core.Retrieval.Retrievers;

/// <summary>
/// Retrieves documents based on vector similarity search using MEVD.
/// </summary>
public class VectorRetriever : IRetriever
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IVectorStoreRecordCollection<string, DefaultRagVectorRecord> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorRetriever"/> class.
    /// </summary>
    /// <param name="embeddingGenerator">The generator used to embed the query.</param>
    /// <param name="collection">The vector store collection.</param>
    public VectorRetriever(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IVectorStoreRecordCollection<string, DefaultRagVectorRecord> collection)
    {
        _embeddingGenerator = embeddingGenerator;
        _collection = collection;
    }

    /// <summary>
    /// Searches for and returns the most relevant documents for the given query using vector similarity.
    /// </summary>
    /// <param name="query">Original user query.</param>
    /// <param name="topK">Maximum number of documents to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Documents ordered by descending vector similarity.</returns>
    public async Task<IEnumerable<RagDocument>> RetrieveAsync(string query, int topK, CancellationToken ct = default)
    {
        // 1. Generate query embedding
        var queryEmbedding = await _embeddingGenerator.GenerateAsync(query, cancellationToken: ct);

        // 2. Search for similar vectors in MEVD
        var searchResults = await _collection.VectorizedSearchAsync(
            queryEmbedding.Vector,
            new VectorSearchOptions { Top = topK },
            ct);

        // 3. Map results to RagDocument
        var results = new List<RagDocument>();
        await foreach (var result in searchResults.Results.WithCancellation(ct))
        {
            var record = result.Record;
            
            // Reconstruct Metadata
            Dictionary<string, object> metadata = new();
            if (!string.IsNullOrEmpty(record.MetadataJson))
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(record.MetadataJson) ?? new();
            }

            metadata["source"] = record.Source;
            metadata["section"] = record.Section;
            metadata["summary"] = record.Summary;
            
            if (!string.IsNullOrEmpty(record.Keywords))
            {
                metadata["keywords"] = record.Keywords.Split(", ", StringSplitOptions.RemoveEmptyEntries);
            }
            
            if (!string.IsNullOrEmpty(record.EntitiesJson))
            {
                metadata["entities"] = JsonSerializer.Deserialize<string[]>(record.EntitiesJson) ?? Array.Empty<string>();
            }

            // 4. Include score in Metadata["_score"]
            metadata["_score"] = result.Score;

            results.Add(new RagDocument(
                Id: record.Id,
                Content: record.Content,
                Vector: record.Vector,
                Metadata: metadata
            ));
        }

        return results;
    }
}
