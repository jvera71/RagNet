using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RagNet;
using RagNet.Abstractions;
using RagNet.Core.Ingestion.Chunkers;
using RagNet.Parsers.Pdf;

var builder = Host.CreateApplicationBuilder(args);

// Logging configuration to see progress in the console
builder.Logging.AddConsole();


// 1. Configure RagNet with the IngestionPipeline
builder.Services.AddAdvancedRag(rag =>
{
    rag.AddIngestion(ingestion =>
    {
        // Add the specialized PDF parser
        ingestion.AddParser<PdfDocumentParser>();
        
        // Semantic partitioning using NLP (Natural Language Processing)
        ingestion.UseSemanticChunker<NLPBoundaryChunker>();
        
        // Enrich chunk metadata using an LLM
        ingestion.UseLLMMetadataEnrichment(
            extractEntities: true, 
            extractKeywords: true, 
            generateSummary: false);
            
        // IVectorStore collection where vectors will be uploaded
        ingestion.UseCollection("pdf-docs");
    });
});

var host = builder.Build();

// 2. Get the ingestion pipeline from DI
var pipeline = host.Services.GetRequiredService<IIngestionPipeline>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting PDF Ingestion Worker...");

// 3. Create a test folder (if it doesn't exist) and process the PDFs
string pdfDirectory = Path.Combine(AppContext.BaseDirectory, "PdfsToIngest");
if (!Directory.Exists(pdfDirectory))
{
    Directory.CreateDirectory(pdfDirectory);
    logger.LogInformation("Created folder '{PdfDirectory}'. Place PDF files there.", pdfDirectory);
}
else
{
    var pdfFiles = Directory.GetFiles(pdfDirectory, "*.pdf");
    if (pdfFiles.Length == 0)
    {
        logger.LogWarning("No PDF files found in '{PdfDirectory}'.", pdfDirectory);
    }
    else
    {
        foreach (var pdfFile in pdfFiles)
        {
            logger.LogInformation("Ingesting file: {FileName}", Path.GetFileName(pdfFile));
            
            try
            {
                using var stream = File.OpenRead(pdfFile);
                
                // 4. Ingest the document (parse, split, enrich, and save to IVectorStore)
                var result = await pipeline.IngestAsync(stream, Path.GetFileName(pdfFile));
                
                logger.LogInformation("Ingestion completed for '{FileName}'. Chunks generated: {ChunkCount}", 
                    Path.GetFileName(pdfFile), result.ChunkCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting file '{FileName}'", Path.GetFileName(pdfFile));
            }
        }
    }
}

logger.LogInformation("Worker finished.");
