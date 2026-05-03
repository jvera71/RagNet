// =============================================================================
// Sample: ConsoleRetrieval
// Demonstrates basic vector retrieval using VectorRetriever.
// The user types a query and gets back the most relevant chunks with their scores.
// =============================================================================

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagNet;
using RagNet.Abstractions;
using RagNet.Core.Retrieval.Retrievers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

// -------------------------------------------------------------------------
// 1. Configure RagNet with a single vector-based retriever pipeline
// -------------------------------------------------------------------------
// NOTE: Replace the placeholder registrations below with your actual
//       IEmbeddingGenerator and IVectorStore implementations
//       (e.g., OllamaEmbeddingGenerator + InMemoryVectorStore).
// -------------------------------------------------------------------------
builder.Services.AddAdvancedRag(rag =>
{
    rag.AddPipeline("vector", pipeline =>
    {
        // Uses vector similarity search (cosine) via Microsoft.Extensions.VectorData
        pipeline.UseHybridRetrieval(alpha: 1.0, expandedTopK: 10); // alpha=1.0 → pure vector
    });
});

// Register the VectorRetriever as the IRetriever implementation
// (normally done by your vector store integration package)
builder.Services.AddTransient<IRetriever, VectorRetriever>();

var host = builder.Build();

// -------------------------------------------------------------------------
// 2. Resolve the retriever directly to show results without generation
// -------------------------------------------------------------------------
var retriever = host.Services.GetRequiredService<IRetriever>();
var logger    = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("=== ConsoleRetrieval Sample ===");
logger.LogInformation("Type a query and press Enter to retrieve the top 5 most relevant chunks.");
logger.LogInformation("Type 'exit' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("Query > ");
    var query = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(query))
        continue;

    if (query.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // -------------------------------------------------------------------------
        // 3. Retrieve top-5 documents
        // -------------------------------------------------------------------------
        var documents = (await retriever.RetrieveAsync(query, topK: 5)).ToList();
        sw.Stop();

        if (documents.Count == 0)
        {
            Console.WriteLine("  [No results found. Make sure the vector store has ingested documents.]\n");
            continue;
        }

        Console.WriteLine($"\n  Found {documents.Count} chunk(s) in {sw.ElapsedMilliseconds} ms:\n");

        for (int i = 0; i < documents.Count; i++)
        {
            var doc   = documents[i];
            var score = doc.Metadata.TryGetValue("_score", out var s) ? $"{s:F4}" : "N/A";
            var source = doc.Metadata.TryGetValue("source", out var src) ? src?.ToString() : "unknown";

            Console.WriteLine($"  [{i + 1}] Score: {score} | Source: {source}");
            Console.WriteLine($"       {doc.Content[..Math.Min(200, doc.Content.Length)]}...");
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during retrieval.");
    }
}

logger.LogInformation("Sample finished.");
