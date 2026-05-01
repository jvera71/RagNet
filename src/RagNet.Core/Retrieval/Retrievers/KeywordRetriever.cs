using System.Text.Json;
using Microsoft.Extensions.VectorData;
using RagNet.Abstractions;
using RagNet.Core.Models;

namespace RagNet.Core.Retrieval.Retrievers;

/// <summary>
/// Retrieves documents based on keyword/full-text search (BM25 or similar).
/// </summary>
public class KeywordRetriever : IRetriever
{
    private readonly VectorStoreCollection<string, DefaultRagVectorRecord> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordRetriever"/> class.
    /// </summary>
    /// <param name="collection">The vector store collection.</param>
    public KeywordRetriever(VectorStoreCollection<string, DefaultRagVectorRecord> collection)
    {
        _collection = collection;
    }

    /// <summary>
    /// Searches for and returns the most relevant documents for the given query using keyword search.
    /// </summary>
    /// <param name="query">Original user query.</param>
    /// <param name="topK">Maximum number of documents to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Documents ordered by descending text relevance.</returns>
    public async Task<IEnumerable<RagDocument>> RetrieveAsync(string query, int topK, CancellationToken ct = default)
    {
        // Conceptual implementation.
        // MEVD doesn't have a standardized full-text search API across all providers yet,
        // but providers like Azure AI Search expose it via custom VectorSearchOptions or specific clients.
        // For demonstration, we'll assume a generic approach or simulate it if fetching all.
        // In a real provider like Azure AI Search, we would pass the text query in the options.

        // Simulated/Placeholder for compilation:
        // var filter = new VectorSearchFilter().EqualTo(nameof(DefaultRagVectorRecord.Content), query);
        
        // This is a placeholder since pure Keyword search requires a specific FTS provider.
        // We'll return an empty list here to satisfy the contract, but in reality, 
        // this would call the FTS endpoint of the underlying database.
        
        var results = new List<RagDocument>();
        
        /* 
        var textSearchResults = await _collection.TextSearchAsync(
            query, 
            new TextSearchOptions { Top = topK }, 
            ct);
            
        await foreach (var result in textSearchResults.Results) ...
        */

        return results;
    }

    // Helper to map record to RagDocument, identical to VectorRetriever
    private RagDocument MapToRagDocument(DefaultRagVectorRecord record, double score)
    {
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

        metadata["_score"] = score;

        return new RagDocument(
            Id: record.Id,
            Content: record.Content,
            Vector: record.Vector,
            Metadata: metadata
        );
    }
}
