using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using RagNet.Abstractions;

namespace RagNet.Core.Retrieval.Rerankers;

/// <summary>
/// Reorders documents using a general LLM.
/// The LLM acts as a judge, scoring each document's relevance to the query from 0 to 10.
/// </summary>
public class LLMReranker : IDocumentReranker
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMReranker"/> class.
    /// </summary>
    /// <param name="chatClient">The LLM chat client.</param>
    public LLMReranker(IChatClient chatClient)
    {
        _chatClient = chatClient;
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

        // 1. Build prompt with query and all documents
        var prompt = $@"Evaluate the relevance of each document to answer the query.
Score from 0 (irrelevant) to 10 (perfectly relevant).
Respond ONLY with a valid JSON array of objects, like this:
[{{""doc"": 0, ""score"": 8.5}}, {{""doc"": 1, ""score"": 2.0}}]

QUERY: ""{query}""

DOCUMENTS:
";
        for (int i = 0; i < docList.Count; i++)
        {
            prompt += $"[{i}] \"{docList[i].Content}\"\n\n";
        }

        try
        {
            // 2. Ask LLM to score
            var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: ct);
            var json = ExtractJson(response.Text ?? string.Empty);
            
            if (string.IsNullOrEmpty(json)) return docList.Take(topK);

            // 3. Parse scores
            var scores = JsonSerializer.Deserialize<List<DocScore>>(json);
            if (scores == null || scores.Count == 0) return docList.Take(topK);

            var scoredDocuments = new List<(RagDocument Doc, double Score)>();

            foreach (var scoreEntry in scores)
            {
                if (scoreEntry.Doc >= 0 && scoreEntry.Doc < docList.Count)
                {
                    scoredDocuments.Add((docList[scoreEntry.Doc], scoreEntry.Score));
                }
            }

            // 4. Sort by descending score -> Top-K
            return scoredDocuments
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => 
                {
                    var newMetadata = new Dictionary<string, object>(x.Doc.Metadata)
                    {
                        ["_llm_score"] = x.Score,
                        ["_score"] = x.Score
                    };
                    return x.Doc with { Metadata = newMetadata };
                });
        }
        catch
        {
            // Fallback to returning the original list if the LLM fails or parsing fails
            return docList.Take(topK);
        }
    }

    private string ExtractJson(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start != -1 && end != -1 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return string.Empty;
    }

    private class DocScore
    {
        [JsonPropertyName("doc")]
        public int Doc { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }
    }
}
