using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using RagNet.Abstractions;
using RagNet.Core.Options;

namespace RagNet.Core.Ingestion.Enrichment;

/// <summary>
/// Enriches document metadata using an LLM to extract structured information
/// like entities, keywords, and summaries.
/// </summary>
public class LLMMetadataEnricher : IMetadataEnricher
{
    private readonly IChatClient _chatClient;
    private readonly LLMMetadataEnricherOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMMetadataEnricher"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for LLM interactions.</param>
    /// <param name="options">Enricher configuration options.</param>
    public LLMMetadataEnricher(
        IChatClient chatClient,
        IOptions<LLMMetadataEnricherOptions> options)
    {
        _chatClient = chatClient;
        _options = options.Value;
    }

    /// <summary>
    /// Enriches a batch of documents with automatically extracted metadata.
    /// </summary>
    /// <param name="documents">Documents to enrich.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Documents with enriched metadata.</returns>
    public async Task<IEnumerable<RagDocument>> EnrichAsync(IEnumerable<RagDocument> documents, CancellationToken ct = default)
    {
        var docList = documents.ToList();
        if (docList.Count == 0 || !ShouldEnrich())
        {
            return docList;
        }

        var results = new List<RagDocument>();
        var batches = docList.Chunk(_options.BatchSize);
        var semaphore = new SemaphoreSlim(_options.MaxConcurrency);

        var tasks = batches.Select(async batch =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var enrichedBatch = await ProcessBatchAsync(batch, ct);
                lock (results)
                {
                    results.AddRange(enrichedBatch);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }

    private bool ShouldEnrich()
    {
        return _options.ExtractEntities || _options.ExtractKeywords || 
               _options.GenerateSummary || _options.DetectLanguage || 
               _options.ClassifyTopic;
    }

    private async Task<IEnumerable<RagDocument>> ProcessBatchAsync(RagDocument[] batch, CancellationToken ct)
    {
        // Construct prompt for the batch
        var prompt = BuildPrompt(batch);

        try
        {
            var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: ct);
            var responseText = response.Text ?? string.Empty;

            // Extract JSON from response (handling potential markdown blocks)
            var json = ExtractJson(responseText);
            if (string.IsNullOrEmpty(json)) return batch;

            var extractions = JsonSerializer.Deserialize<Dictionary<string, ExtractedMetadata>>(json);
            if (extractions == null) return batch;

            var resultBatch = new List<RagDocument>();

            foreach (var doc in batch)
            {
                if (extractions.TryGetValue(doc.Id, out var metadata))
                {
                    var newMeta = new Dictionary<string, object>(doc.Metadata);

                    if (_options.ExtractEntities && metadata.Entities != null)
                        newMeta["entities"] = metadata.Entities;
                    
                    if (_options.ExtractKeywords && metadata.Keywords != null)
                        newMeta["keywords"] = metadata.Keywords;
                        
                    if (_options.GenerateSummary && !string.IsNullOrEmpty(metadata.Summary))
                        newMeta["summary"] = metadata.Summary;
                        
                    if (_options.DetectLanguage && !string.IsNullOrEmpty(metadata.Language))
                        newMeta["language"] = metadata.Language;
                        
                    if (_options.ClassifyTopic && !string.IsNullOrEmpty(metadata.Topic))
                        newMeta["topic"] = metadata.Topic;

                    resultBatch.Add(doc with { Metadata = newMeta });
                }
                else
                {
                    resultBatch.Add(doc);
                }
            }

            return resultBatch;
        }
        catch (Exception)
        {
            // On failure, return the original batch without enrichment
            return batch;
        }
    }

    private string BuildPrompt(RagDocument[] batch)
    {
        var instructions = new List<string>();
        if (_options.ExtractEntities) instructions.Add("- \"entities\": [\"list of named entities mentioned\"]");
        if (_options.ExtractKeywords) instructions.Add("- \"keywords\": [\"3-5 keywords summarizing the fragment\"]");
        if (_options.GenerateSummary) instructions.Add("- \"summary\": \"1-2 sentence summary of the content\"");
        if (_options.DetectLanguage) instructions.Add("- \"language\": \"ISO language code\"");
        if (_options.ClassifyTopic) instructions.Add("- \"topic\": \"Main topic category\"");

        var prompt = $@"Analyze the following text fragments and extract the requested information.
Respond ONLY with a valid JSON object mapping each ID to its extracted data.

Format required:
{{
  ""[ID]"": {{
    {string.Join(",\n    ", instructions.Select(i => i.Replace("- ", "")))}
  }}
}}

FRAGMENTS:
";
        foreach (var doc in batch)
        {
            prompt += $"ID: {doc.Id}\nTEXT: {doc.Content}\n---\n";
        }

        return prompt;
    }

    private string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start != -1 && end != -1 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return string.Empty;
    }

    private class ExtractedMetadata
    {
        public string[]? Entities { get; set; }
        public string[]? Keywords { get; set; }
        public string? Summary { get; set; }
        public string? Language { get; set; }
        public string? Topic { get; set; }
    }
}
