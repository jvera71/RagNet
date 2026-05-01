using Microsoft.Extensions.DependencyInjection;
using RagNet.Abstractions;
using RagNet.Core.Pipeline;

namespace RagNet;

/// <summary>
/// Root configuration builder for the RagNet ecosystem.
/// Provides methods to configure ingestion and named RAG pipelines.
/// </summary>
public class RagBuilder
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<string, RagPipelineBuilder> _pipelines = new();
    private IngestionPipelineBuilder? _ingestion;

    /// <summary>
    /// Initializes a new instance of the <see cref="RagBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    internal RagBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Configures the document ingestion pipeline.
    /// </summary>
    /// <param name="configure">The action to configure the ingestion pipeline.</param>
    /// <returns>The current builder instance.</returns>
    public RagBuilder AddIngestion(Action<IngestionPipelineBuilder> configure)
    {
        _ingestion = new IngestionPipelineBuilder(_services);
        configure(_ingestion);
        return this;
    }

    /// <summary>
    /// Registers a named RAG pipeline.
    /// </summary>
    /// <param name="name">The name of the pipeline (e.g. "default", "fast").</param>
    /// <param name="configure">The action to configure the named pipeline.</param>
    /// <returns>The current builder instance.</returns>
    public RagBuilder AddPipeline(string name, Action<RagPipelineBuilder> configure)
    {
        var pipelineBuilder = new RagPipelineBuilder(_services);
        configure(pipelineBuilder);
        _pipelines[name] = pipelineBuilder;
        return this;
    }

    /// <summary>
    /// Validates the configuration and registers the necessary services into the container.
    /// </summary>
    internal void Build()
    {
        // Register IRagPipelineFactory with all the configured pipelines
        _services.AddSingleton<IRagPipelineFactory>(sp =>
        {
            var builtPipelines = new Dictionary<string, Func<IServiceProvider, IRagPipeline>>();
            foreach (var kvp in _pipelines)
            {
                var builder = kvp.Value;
                var delegatePipeline = builder.Build();
                builtPipelines[kvp.Key] = provider => 
                    new DefaultRagPipeline(
                        delegatePipeline, 
                        provider.GetService<Microsoft.Extensions.Logging.ILogger<DefaultRagPipeline>>(), 
                        kvp.Key);
            }

            return new RagPipelineFactory(sp, builtPipelines);
        });

        // If there is only one pipeline registered, register it as a direct IRagPipeline service
        if (_pipelines.Count == 1)
        {
            _services.AddTransient<IRagPipeline>(sp =>
                sp.GetRequiredService<IRagPipelineFactory>().Create(_pipelines.Keys.First()));
        }

        // Build and register the ingestion pipeline if it was configured
        _ingestion?.Build();

        // Run final validations
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (_pipelines.Count == 0 && _ingestion == null)
        {
            throw new InvalidOperationException(
                "RagNet: You must configure at least one RAG pipeline or the ingestion pipeline.");
        }
    }
}
