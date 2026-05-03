// =============================================================================
// Sample: QueryTransformationSample
// Demonstrates all query transformers available in RagNet.Core:
//
//   1. QueryRewriter      – rewrites ambiguous queries for better retrieval
//   2. HyDETransformer    – generates a hypothetical document (HyDE)
//   3. StepBackTransformer – produces the original + a more abstract query
//   4. TranslationTransformer – translates the query to the corpus language
//   5. CompositeQueryTransformer – chains multiple transformers sequentially
//
// For each transformer, the sample prints the original query and
// the transformed versions side by side.
// =============================================================================

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagNet;
using RagNet.Abstractions;
using RagNet.Core.Retrieval.Transformers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

// -------------------------------------------------------------------------
// 1. Register RagNet (pipeline not strictly needed but required by builder)
// -------------------------------------------------------------------------
// NOTE: Register IChatClient before calling AddAdvancedRag
//       (e.g., builder.Services.AddOllamaChatClient("llama3")).
// -------------------------------------------------------------------------
builder.Services.AddAdvancedRag(rag =>
{
    rag.AddPipeline("query-transform-demo", pipeline =>
    {
        pipeline.UseQueryTransformation<QueryRewriter>();
    });
});

// Register all transformers individually for direct demonstration
builder.Services.AddTransient<QueryRewriter>();
builder.Services.AddTransient<HyDETransformer>();
builder.Services.AddTransient<StepBackTransformer>();
builder.Services.AddTransient<TranslationTransformer>();
builder.Services.AddTransient<CompositeQueryTransformer>(sp =>
{
    // Compose: rewrite first, then expand with step-back
    return new CompositeQueryTransformer(new IQueryTransformer[]
    {
        sp.GetRequiredService<QueryRewriter>(),
        sp.GetRequiredService<StepBackTransformer>()
    });
});

var host = builder.Build();

var rewriter    = host.Services.GetRequiredService<QueryRewriter>();
var hyde        = host.Services.GetRequiredService<HyDETransformer>();
var stepBack    = host.Services.GetRequiredService<StepBackTransformer>();
var translation = host.Services.GetRequiredService<TranslationTransformer>();
var composite   = host.Services.GetRequiredService<CompositeQueryTransformer>();
var logger      = host.Services.GetRequiredService<ILogger<Program>>();

Console.WriteLine("=== QueryTransformationSample ===");
Console.WriteLine("Runs every query transformer and prints the results.");
Console.WriteLine("Type 'exit' to quit.\n");

while (true)
{
    Console.Write("Original query > ");
    var query = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(query)) continue;
    if (query.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    Console.WriteLine();

    // -------------------------------------------------------------------------
    // 2. Run and display each transformer
    // -------------------------------------------------------------------------
    await RunAndPrint("1. QueryRewriter",           rewriter,    query);
    await RunAndPrint("2. HyDE",                    hyde,        query);
    await RunAndPrint("3. StepBack",                stepBack,    query);
    await RunAndPrint("4. Translation (→ English)", translation, query);
    await RunAndPrint("5. Composite (Rewrite+StepBack)", composite, query);

    Console.WriteLine();
}

logger.LogInformation("Sample finished.");

// ─── Helpers ─────────────────────────────────────────────────────────────────

static async Task RunAndPrint(string label, IQueryTransformer transformer, string originalQuery)
{
    Console.WriteLine($"  ── {label} ──");
    Console.WriteLine($"     Original  : {originalQuery}");

    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var transformed = (await transformer.TransformAsync(originalQuery)).ToList();
        sw.Stop();

        for (int i = 0; i < transformed.Count; i++)
        {
            Console.WriteLine($"     Result [{i + 1}]: {transformed[i]}");
        }

        Console.WriteLine($"     Elapsed   : {sw.ElapsedMilliseconds} ms");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"     ERROR: {ex.Message}");
    }

    Console.WriteLine();
}
