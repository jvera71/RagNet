using Microsoft.Extensions.Options;
using RagNet.Abstractions;
using RagNet.Core.Options;

namespace RagNet.Core.Retrieval.Retrievers;

/// <summary>
/// Combines the results of VectorRetriever and KeywordRetriever using 
/// Reciprocal Rank Fusion (RRF) to obtain the best of both approaches.
/// </summary>
public class HybridRetriever : IRetriever
{
    private readonly VectorRetriever _vectorRetriever;
    private readonly KeywordRetriever _keywordRetriever;
    private readonly HybridRetrieverOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridRetriever"/> class.
    /// </summary>
    /// <param name="vectorRetriever">The vector retriever instance.</param>
    /// <param name="keywordRetriever">The keyword retriever instance.</param>
    /// <param name="options">The hybrid retriever options.</param>
    public HybridRetriever(
        VectorRetriever vectorRetriever,
        KeywordRetriever keywordRetriever,
        IOptions<HybridRetrieverOptions> options)
    {
        _vectorRetriever = vectorRetriever;
        _keywordRetriever = keywordRetriever;
        _options = options.Value;
    }

    /// <summary>
    /// Searches using both vector and keyword strategies and fuses the results.
    /// </summary>
    /// <param name="query">Original user query.</param>
    /// <param name="topK">Maximum number of documents to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Documents ordered by fused RRF relevance.</returns>
    public async Task<IEnumerable<RagDocument>> RetrieveAsync(string query, int topK, CancellationToken ct = default)
    {
        // 1. Execute BOTH searches in parallel
        var vectorTask = _vectorRetriever.RetrieveAsync(query, _options.ExpandedTopK, ct);
        var keywordTask = _keywordRetriever.RetrieveAsync(query, _options.ExpandedTopK, ct);

        await Task.WhenAll(vectorTask, keywordTask);

        var vectorResults = vectorTask.Result.ToList();
        var keywordResults = keywordTask.Result.ToList();

        // 2. Fuse results using RRF
        var fused = ReciprocalRankFusion(vectorResults, keywordResults, _options.Alpha, _options.RrfK);

        // 3. Return Top-K fused results
        return fused.Take(topK);
    }

    private IEnumerable<RagDocument> ReciprocalRankFusion(
        List<RagDocument> vectorResults, 
        List<RagDocument> keywordResults, 
        double alpha, 
        int k)
    {
        var scores = new Dictionary<string, double>();
        var documents = new Dictionary<string, RagDocument>();

        // Process vector results
        for (int i = 0; i < vectorResults.Count; i++)
        {
            var doc = vectorResults[i];
            if (!scores.ContainsKey(doc.Id))
            {
                scores[doc.Id] = 0.0;
                documents[doc.Id] = doc;
            }
            
            // rank_i is 1-indexed
            double rrfScore = alpha / (k + (i + 1));
            scores[doc.Id] += rrfScore;
        }

        // Process keyword results
        for (int i = 0; i < keywordResults.Count; i++)
        {
            var doc = keywordResults[i];
            if (!scores.ContainsKey(doc.Id))
            {
                scores[doc.Id] = 0.0;
                documents[doc.Id] = doc;
            }
            
            // rank_i is 1-indexed, weight is (1 - alpha)
            double rrfScore = (1.0 - alpha) / (k + (i + 1));
            scores[doc.Id] += rrfScore;
        }

        // Update documents with the new RRF score in metadata
        var fusedDocuments = scores
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => 
            {
                var doc = documents[kvp.Key];
                var newMetadata = new Dictionary<string, object>(doc.Metadata)
                {
                    ["_score"] = kvp.Value,
                    ["_rrf_score"] = kvp.Value
                };
                return doc with { Metadata = newMetadata };
            });

        return fusedDocuments;
    }
}
