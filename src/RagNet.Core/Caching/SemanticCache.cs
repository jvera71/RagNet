using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using RagNet.Abstractions;

namespace RagNet.Core.Caching;

/// <summary>
/// Semantic cache to prevent re-processing semantically similar queries.
/// </summary>
public class SemanticCache
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly VectorStoreCollection<string, CacheRecord> _cache;
    private readonly double _similarityThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticCache"/> class.
    /// </summary>
    public SemanticCache(
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        VectorStoreCollection<string, CacheRecord> cache,
        double similarityThreshold = 0.95)
    {
        _embeddings = embeddings;
        _cache = cache;
        _similarityThreshold = similarityThreshold;
    }

    /// <summary>
    /// Retrieves a cached response if a semantically similar query exists.
    /// </summary>
    public async Task<RagResponse?> GetAsync(string query, CancellationToken ct = default)
    {
        // 1. Generate query embedding
        var queryVector = await _embeddings.GenerateAsync(query, cancellationToken: ct);

        // 2. Search cache by vector similarity
        var searchOptions = new VectorSearchOptions<CacheRecord>();
        var results = _cache.SearchAsync(queryVector.Vector, 1, searchOptions, ct);

        VectorSearchResult<CacheRecord>? bestMatch = null;
        await foreach (var res in results.WithCancellation(ct))
        {
            bestMatch = res;
            break;
        }

        // 3. If similarity exceeds the threshold, return the cached response
        if (bestMatch != null && bestMatch.Score >= _similarityThreshold)
        {
            return JsonSerializer.Deserialize<RagResponse>(bestMatch.Record.ResponseJson);
        }

        return null; // Cache miss
    }

    /// <summary>
    /// Saves a response into the semantic cache.
    /// </summary>
    public async Task SetAsync(string query, RagResponse response, CancellationToken ct = default)
    {
        var queryVector = await _embeddings.GenerateAsync(query, cancellationToken: ct);
        
        await _cache.UpsertAsync(new CacheRecord
        {
            Id = Guid.NewGuid().ToString(),
            QueryVector = queryVector.Vector,
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken: ct);
    }
}

/// <summary>
/// Internal record representing a cached RAG response.
/// </summary>
public class CacheRecord
{
    [VectorStoreKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> QueryVector { get; set; }

    [VectorStoreData]
    public string ResponseJson { get; set; } = string.Empty;

    [VectorStoreData]
    public DateTimeOffset CreatedAt { get; set; }
}
