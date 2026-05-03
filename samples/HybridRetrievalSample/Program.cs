// =============================================================================
// Sample: HybridRetrievalSample
// Demonstrates and compares three retrieval strategies:
//   1. VectorRetriever  – pure cosine similarity
//   2. KeywordRetriever – full-text / BM25
//   3. HybridRetriever  – RRF fusion of both (the recommended approach)
//
// For each strategy the sample prints the ranked results and their scores
// so you can observe how fusion changes the ranking.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagNet;
using RagNet.Abstractions;
using RagNet.Core.Options;
using RagNet.Core.Retrieval.Retrievers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

// -------------------------------------------------------------------------
// 1. Register RagNet (pipeline not used directly in this sample)
// -------------------------------------------------------------------------
// NOTE: Register IEmbeddingGenerator and IVectorStore before calling
//       AddAdvancedRag (done by your provider integration package).
// -------------------------------------------------------------------------
builder.Services.AddAdvancedRag(rag =>
{
    rag.AddPipeline("hybrid-demo", pipeline =>
    {
        pipeline.UseHybridRetrieval(alpha: 0.6, expandedTopK: 20);
    });
});

// Register individual retrievers for direct comparison
builder.Services.AddTransient<VectorRetriever>();
builder.Services.AddTransient<KeywordRetriever>();
builder.Services.AddTransient<HybridRetriever>(sp =>
{
    var vector  = sp.GetRequiredService<VectorRetriever>();
    var keyword = sp.GetRequiredService<KeywordRetriever>();
    var opts    = Options.Create(new HybridRetrieverOptions
    {
        Alpha        = 0.6,   // 60 % vector, 40 % keyword
        ExpandedTopK = 20,
        RrfK         = 60
    });
    return new HybridRetriever(vector, keyword, opts);
});

var host = builder.Build();

var vectorRetriever  = host.Services.GetRequiredService<VectorRetriever>();
var keywordRetriever = host.Services.GetRequiredService<KeywordRetriever>();
var hybridRetriever  = host.Services.GetRequiredService<HybridRetriever>();
var logger           = host.Services.GetRequiredService<ILogger<Program>>();

const int TopK = 5;

Console.WriteLine("=== HybridRetrievalSample ===");
Console.WriteLine($"Comparing VectorRetriever, KeywordRetriever and HybridRetriever (top {TopK}).");
Console.WriteLine("Type 'exit' to quit.\n");

while (true)
{
    Console.Write("Query > ");
    var query = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(query)) continue;
    if (query.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    // -------------------------------------------------------------------------
    // 2. Execute all three strategies in parallel
    // -------------------------------------------------------------------------
    var sw = System.Diagnostics.Stopwatch.StartNew();

    var vectorTask  = vectorRetriever.RetrieveAsync(query, TopK);
    var keywordTask = keywordRetriever.RetrieveAsync(query, TopK);
    var hybridTask  = hybridRetriever.RetrieveAsync(query, TopK);

    await Task.WhenAll(vectorTask, keywordTask, hybridTask);
    sw.Stop();

    // -------------------------------------------------------------------------
    // 3. Print side-by-side results
    // -------------------------------------------------------------------------
    PrintResults("Vector (cosine)",  vectorTask.Result,  query);
    PrintResults("Keyword (BM25)",   keywordTask.Result, query);
    PrintResults("Hybrid (RRF α=0.6)", hybridTask.Result, query);

    Console.WriteLine($"  Total elapsed: {sw.ElapsedMilliseconds} ms\n");
}

logger.LogInformation("Sample finished.");

// ─── Helpers ─────────────────────────────────────────────────────────────────

static void PrintResults(string label, IEnumerable<RagDocument> docs, string query)
{
    var list = docs.ToList();
    Console.WriteLine($"\n  ── {label} ── ({list.Count} results)");

    if (list.Count == 0)
    {
        Console.WriteLine("     (no results)");
        return;
    }

    for (int i = 0; i < list.Count; i++)
    {
        var doc   = list[i];
        var score = doc.Metadata.TryGetValue("_score", out var s) ? $"{s:F4}" : "N/A";
        var src   = doc.Metadata.TryGetValue("source", out var so) ? so?.ToString() : "?";
        var preview = doc.Content.Length > 120 ? doc.Content[..120] + "…" : doc.Content;

        Console.WriteLine($"  {i + 1}. score={score} | src={src}");
        Console.WriteLine($"     {preview}");
    }
}
