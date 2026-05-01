using Microsoft.Extensions.DependencyInjection;
using RagNet.Abstractions;
using RagNet.Core.Ingestion;
using RagNet.Core.Ingestion.Enrichment;
using RagNet.Core.Options;

namespace RagNet;

/// <summary>
/// Builder for configuring the ingestion pipeline fluently.
/// </summary>
public class IngestionPipelineBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public IngestionPipelineBuilder(IServiceCollection services)
    {
        _services = services;
        
        // Register default components
        _services.AddTransient<EmbeddingBatcher>(sp => new EmbeddingBatcher(_embeddingBatchSize));
        _services.AddTransient<IIngestionPipeline, IngestionPipeline>();
    }

    private int _embeddingBatchSize = 50;

    /// <summary>
    /// Registers a document parser.
    /// </summary>
    /// <typeparam name="TParser">The type of the parser.</typeparam>
    /// <returns>The builder instance.</returns>
    public IngestionPipelineBuilder AddParser<TParser>()
        where TParser : class, IDocumentParser
    {
        _services.AddTransient<IDocumentParser, TParser>();
        return this;
    }

    /// <summary>
    /// Configures the semantic chunker.
    /// </summary>
    /// <typeparam name="TChunker">The type of the chunker.</typeparam>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder instance.</returns>
    public IngestionPipelineBuilder UseSemanticChunker<TChunker>(Action<object>? configure = null)
        where TChunker : class, ISemanticChunker
    {
        _services.AddTransient<ISemanticChunker, TChunker>();
        if (configure != null)
        {
            // Simplified configuration registration for demo purposes.
            // In a real framework, this would dynamically determine the options type and register it.
        }
        return this;
    }

    /// <summary>
    /// Enables LLM-based metadata enrichment.
    /// </summary>
    /// <param name="extractEntities">Whether to extract named entities.</param>
    /// <param name="extractKeywords">Whether to extract keywords.</param>
    /// <param name="generateSummary">Whether to generate a summary.</param>
    /// <returns>The builder instance.</returns>
    public IngestionPipelineBuilder UseLLMMetadataEnrichment(
        bool extractEntities = true,
        bool extractKeywords = true,
        bool generateSummary = true)
    {
        _services.AddTransient<IMetadataEnricher, LLMMetadataEnricher>();
        _services.Configure<LLMMetadataEnricherOptions>(options =>
        {
            options.ExtractEntities = extractEntities;
            options.ExtractKeywords = extractKeywords;
            options.GenerateSummary = generateSummary;
        });
        return this;
    }

    /// <summary>
    /// Configures the embedding batch size.
    /// </summary>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The builder instance.</returns>
    public IngestionPipelineBuilder WithEmbeddingBatchSize(int batchSize)
    {
        _embeddingBatchSize = batchSize;
        // Re-register with new size
        _services.AddTransient<EmbeddingBatcher>(sp => new EmbeddingBatcher(_embeddingBatchSize));
        return this;
    }

    /// <summary>
    /// Configures the target vector collection name.
    /// </summary>
    /// <param name="collectionName">The collection name.</param>
    /// <returns>The builder instance.</returns>
    public IngestionPipelineBuilder UseCollection(string collectionName)
    {
        // Registration logic for the specific MEVD collection
        // This is typically handled by resolving IVectorStore and requesting the collection.
        // For brevity, assuming it configures a factory or provider.
        return this;
    }

    /// <summary>
    /// Validates and finalizes ingestion configuration.
    /// </summary>
    internal void Build()
    {
        // Optional validation or delayed registrations can occur here.
    }
}
