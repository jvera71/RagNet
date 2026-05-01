using Microsoft.Extensions.AI;
using RagNet.Abstractions;

namespace RagNet.Core.Retrieval.Rerankers;

/// <summary>
/// Reorders documents using a specialized Cross-Encoder model.
/// Evaluates the (query, document) pair jointly for higher precision.
/// </summary>
public class CrossEncoderReranker : IDocumentReranker
{
    private readonly IChatClient _crossEncoderClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrossEncoderReranker"/> class.
    /// </summary>
    /// <param name="crossEncoderClient">The client configured to hit the cross-encoder endpoint.</param>
    public CrossEncoderReranker(IChatClient crossEncoderClient)
    {
        _crossEncoderClient = crossEncoderClient;
    }

    /// <summary>
    /// Reorders the documents by relevance and returns the top-K best results.
    /// </summary>
    /// <param name="query">Original user query.</param>
    /// <param name="documents">Candidate documents to reorder.</param>
    /// <param name="topK">Maximum number of documents to return after reordering.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Top-K documents reordered by descending relevance.</returns>
    public async Task<IEnumerable<RagDocument>> RerankAsync(
        string query, 
        IEnumerable<RagDocument> documents, 
        int topK, 
        CancellationToken ct = default)
    {
        var docList = documents.ToList();
        if (docList.Count == 0) return docList;

        var scoredDocuments = new List<(RagDocument Doc, double Score)>();

        // Note: In a real implementation with a true Cross-Encoder REST API (like BGE or Cohere Rerank), 
        // you would send a single batch request: { query: "...", documents: ["...", "..."] } 
        // and receive an array of scores.
        // Since the interface specifies IChatClient, we simulate or adapt it here.
        // If the IChatClient represents an adapter for a reranking API, the call might be customized.
        // For demonstration, we'll assume the client is a generic LLM prompting for a score,
        // or that it's a specialized client where the text contains both.
        // To accurately match the doc's intent for CrossEncoder, we'll process them individually or as a batch.

        foreach (var doc in docList)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Create pair (query, doc.Content)
            var prompt = $"Query: {query}\nDocument: {doc.Content}\nScore this match from 0.0 to 1.0. Reply ONLY with the number.";
            
            try 
            {
                // 2. Send to cross-encoder -> get relevance score
                var response = await _crossEncoderClient.CompleteAsync(prompt, cancellationToken: ct);
                
                if (double.TryParse(response.Message.Text?.Trim(), out double score))
                {
                    scoredDocuments.Add((doc, score));
                }
                else
                {
                    scoredDocuments.Add((doc, 0));
                }
            }
            catch
            {
                // Fallback to 0 or original score on error
                scoredDocuments.Add((doc, 0));
            }
        }

        // 3. Order by descending score
        // 4. Return Top-K
        return scoredDocuments
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => 
            {
                var newMetadata = new Dictionary<string, object>(x.Doc.Metadata)
                {
                    ["_cross_encoder_score"] = x.Score,
                    ["_score"] = x.Score
                };
                return x.Doc with { Metadata = newMetadata };
            });
    }
}
