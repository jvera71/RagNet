// =============================================================================
// Sample: SemanticChunkingComparison
// Compares the three chunkers available in RagNet.Core on the same document:
//
//   1. NLPBoundaryChunker         – splits at sentence/paragraph NLP boundaries
//   2. MarkdownStructureChunker   – splits by heading hierarchy (H2 by default)
//   3. EmbeddingSimilarityChunker – groups paragraphs by embedding cosine similarity
//
// For each chunker, the sample prints:
//   · Number of chunks produced
//   · Average / min / max chunk size (chars)
//   · Elapsed time
//   · A preview of the first 3 chunks
//
// Place a Markdown (.md) file in the "Documents" folder next to the binary.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagNet;
using RagNet.Abstractions;
using RagNet.Core.Ingestion.Chunkers;
using RagNet.Core.Options;
using RagNet.Parsers.Markdown;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

// -------------------------------------------------------------------------
// 1. Register RagNet (ingestion pipeline not used directly)
// -------------------------------------------------------------------------
// NOTE: EmbeddingSimilarityChunker needs IEmbeddingGenerator registered.
//       Replace the stub below with your actual provider.
// -------------------------------------------------------------------------
builder.Services.AddAdvancedRag(rag =>
{
    rag.AddIngestion(ingestion =>
    {
        ingestion.AddParser<MarkdownDocumentParser>();
        ingestion.UseSemanticChunker<NLPBoundaryChunker>(); // default for ingestion
    });
});

// Register all three chunkers individually for direct comparison
builder.Services.AddTransient<NLPBoundaryChunker>(sp =>
    new NLPBoundaryChunker(
        Options.Create(new NLPBoundaryChunkerOptions
        {
            MaxChunkSize = 1500,
            MinChunkSize = 150
        })));

builder.Services.AddTransient<MarkdownStructureChunker>(sp =>
    new MarkdownStructureChunker(
        Options.Create(new MarkdownStructureChunkerOptions
        {
            ChunkAtHeadingLevel = 2,    // chunk at H2
            IncludeBreadcrumb   = true,
            MinChunkSize        = 100,
            MaxChunkSize        = 3000
        })));

builder.Services.AddTransient<EmbeddingSimilarityChunker>(sp =>
    new EmbeddingSimilarityChunker(
        sp.GetRequiredService<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>(),
        Options.Create(new EmbeddingSimilarityChunkerOptions
        {
            SimilarityThreshold = 0.75f,
            MinChunkSize        = 100,
            MaxChunkSize        = 3000
        })));

var host = builder.Build();

var nlp       = host.Services.GetRequiredService<NLPBoundaryChunker>();
var markdown  = host.Services.GetRequiredService<MarkdownStructureChunker>();
var embedding = host.Services.GetRequiredService<EmbeddingSimilarityChunker>();
var parser    = host.Services.GetRequiredService<IDocumentParser>();
var logger    = host.Services.GetRequiredService<ILogger<Program>>();

// -------------------------------------------------------------------------
// 2. Load the Markdown document
// -------------------------------------------------------------------------
string docsDir = Path.Combine(AppContext.BaseDirectory, "Documents");
if (!Directory.Exists(docsDir))
{
    Directory.CreateDirectory(docsDir);
    logger.LogWarning("Created '{Dir}'. Place a .md file there and restart.", docsDir);
    return;
}

var mdFiles = Directory.GetFiles(docsDir, "*.md");
if (mdFiles.Length == 0)
{
    logger.LogWarning("No .md files found in '{Dir}'. Place a Markdown file there.", docsDir);
    return;
}

var filePath = mdFiles[0];
logger.LogInformation("Using file: {File}", Path.GetFileName(filePath));

await using var stream = File.OpenRead(filePath);
var rootNode = await parser.ParseAsync(stream, Path.GetFileName(filePath));

Console.WriteLine("\n=== SemanticChunkingComparison ===\n");

// -------------------------------------------------------------------------
// 3. Run each chunker and print metrics
// -------------------------------------------------------------------------
await CompareChunker("NLPBoundaryChunker",           nlp,       rootNode);
await CompareChunker("MarkdownStructureChunker (H2)", markdown,  rootNode);
await CompareChunker("EmbeddingSimilarityChunker",   embedding, rootNode);

logger.LogInformation("Comparison finished.");

// ─── Helpers ─────────────────────────────────────────────────────────────────

static async Task CompareChunker(string label, ISemanticChunker chunker, DocumentNode root)
{
    Console.WriteLine($"── {label} ──");

    var sw     = System.Diagnostics.Stopwatch.StartNew();
    var chunks = (await chunker.ChunkAsync(root)).ToList();
    sw.Stop();

    if (chunks.Count == 0)
    {
        Console.WriteLine("   (no chunks produced)\n");
        return;
    }

    var sizes = chunks.Select(c => c.Content.Length).ToList();
    Console.WriteLine($"   Chunks   : {chunks.Count}");
    Console.WriteLine($"   Avg size : {sizes.Average():F0} chars");
    Console.WriteLine($"   Min size : {sizes.Min()} chars");
    Console.WriteLine($"   Max size : {sizes.Max()} chars");
    Console.WriteLine($"   Elapsed  : {sw.ElapsedMilliseconds} ms");
    Console.WriteLine();

    // Print first 3 chunks as preview
    int preview = Math.Min(3, chunks.Count);
    Console.WriteLine($"   First {preview} chunk(s):");
    for (int i = 0; i < preview; i++)
    {
        var content = chunks[i].Content;
        var snip    = content.Length > 200 ? content[..200] + "…" : content;
        Console.WriteLine($"   [{i + 1}] ({content.Length} chars) {snip}");
    }

    Console.WriteLine();
}
