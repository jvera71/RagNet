// =============================================================================
// Sample: ConsoleRag  (End-to-End)
// Demonstrates the complete RAG pipeline:
//   PDF Ingestion → Vector Retrieval → LLM Reranking → SK Generation
// The user types questions and receives grounded answers with citations.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagNet;
using RagNet.Abstractions;
using RagNet.Core.Ingestion.Chunkers;
using RagNet.Core.Retrieval.Rerankers;
using RagNet.Parsers.Pdf;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

// -------------------------------------------------------------------------
// 1. Configure the full RAG pipeline
// -------------------------------------------------------------------------
// NOTE: Register your IEmbeddingGenerator, IChatClient and IVectorStore
//       before calling AddAdvancedRag. The code below assumes those
//       registrations are already present (e.g., from an Ollama or OpenAI
//       integration package).
// -------------------------------------------------------------------------
builder.Services.AddAdvancedRag(rag =>
{
    // --- Ingestion sub-pipeline ---
    rag.AddIngestion(ingestion =>
    {
        ingestion.AddParser<PdfDocumentParser>();
        ingestion.UseSemanticChunker<NLPBoundaryChunker>();
        ingestion.UseLLMMetadataEnrichment(
            extractEntities: true,
            extractKeywords: true,
            generateSummary: true);
        ingestion.UseCollection("rag-docs");
    });

    // --- Query pipeline ---
    rag.AddPipeline("default", pipeline =>
    {
        // Step 1: Hybrid retrieval (vector + keyword, RRF fusion)
        pipeline.UseHybridRetrieval(alpha: 0.6, expandedTopK: 20);

        // Step 2: LLM-based reranking to surface the best 5 chunks
        pipeline.UseReranking<LLMReranker>(topK: 5);

        // Step 3: Response generation with Semantic Kernel
        pipeline.UseSemanticKernelGenerator();
    });
});

var host = builder.Build();

// -------------------------------------------------------------------------
// 2. Ingest documents on first run (optional step, guarded by flag)
// -------------------------------------------------------------------------
var ingestionPipeline = host.Services.GetRequiredService<IIngestionPipeline>();
var logger            = host.Services.GetRequiredService<ILogger<Program>>();

string pdfDirectory = Path.Combine(AppContext.BaseDirectory, "Documents");
if (!Directory.Exists(pdfDirectory))
{
    Directory.CreateDirectory(pdfDirectory);
    logger.LogInformation("Created '{PdfDir}'. Add PDF files there and restart the sample.", pdfDirectory);
}
else
{
    var pdfs = Directory.GetFiles(pdfDirectory, "*.pdf");
    if (pdfs.Length > 0)
    {
        logger.LogInformation("Ingesting {Count} PDF file(s)...", pdfs.Length);
        foreach (var pdf in pdfs)
        {
            try
            {
                using var stream = File.OpenRead(pdf);
                var result = await ingestionPipeline.IngestAsync(stream, Path.GetFileName(pdf));
                logger.LogInformation("  '{File}' → {Chunks} chunks.", Path.GetFileName(pdf), result.ChunkCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "  Failed to ingest '{File}'.", Path.GetFileName(pdf));
            }
        }
        logger.LogInformation("Ingestion complete.\n");
    }
}

// -------------------------------------------------------------------------
// 3. Interactive Q&A loop
// -------------------------------------------------------------------------
var ragPipeline = host.Services.GetRequiredService<IRagPipeline>();

Console.WriteLine("=== ConsoleRag – End-to-End RAG Sample ===");
Console.WriteLine("Ask questions about the ingested documents.");
Console.WriteLine("Type 'exit' to quit.\n");

while (true)
{
    Console.Write("Question > ");
    var question = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(question))
        continue;

    if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Execute the full pipeline: transform → retrieve → rerank → generate
        var response = await ragPipeline.ExecuteAsync(question);
        sw.Stop();

        // -------------------------------------------------------------------------
        // 4. Display the answer
        // -------------------------------------------------------------------------
        Console.WriteLine($"\n  Answer ({sw.ElapsedMilliseconds} ms):");
        Console.WriteLine($"  {response.Answer}");

        // -------------------------------------------------------------------------
        // 5. Display citations
        // -------------------------------------------------------------------------
        if (response.Citations.Count > 0)
        {
            Console.WriteLine("\n  Sources:");
            foreach (var citation in response.Citations)
            {
                Console.WriteLine($"    · [{citation.DocumentId}] \"{citation.Excerpt[..Math.Min(120, citation.Excerpt.Length)]}...\"");
            }
        }

        // -------------------------------------------------------------------------
        // 6. Display execution metadata
        // -------------------------------------------------------------------------
        if (response.ExecutionMetadata.Count > 0)
        {
            Console.WriteLine("\n  Metadata:");
            foreach (var kvp in response.ExecutionMetadata)
            {
                Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
            }
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Pipeline execution failed.");
    }
}

logger.LogInformation("Sample finished.");
