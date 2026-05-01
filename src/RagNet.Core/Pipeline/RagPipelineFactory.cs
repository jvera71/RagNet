using Microsoft.Extensions.DependencyInjection;
using RagNet.Abstractions;

namespace RagNet.Core.Pipeline;

/// <summary>
/// Resolves named RAG pipelines at runtime.
/// </summary>
public class RagPipelineFactory : IRagPipelineFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Func<IServiceProvider, IRagPipeline>> _registrations;

    /// <summary>
    /// Initializes a new instance of the <see cref="RagPipelineFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The dependency injection container.</param>
    /// <param name="registrations">The dictionary of registered pipelines.</param>
    public RagPipelineFactory(
        IServiceProvider serviceProvider,
        Dictionary<string, Func<IServiceProvider, IRagPipeline>> registrations)
    {
        _serviceProvider = serviceProvider;
        _registrations = registrations;
    }

    /// <summary>
    /// Creates and resolves a pipeline by its registered name.
    /// </summary>
    /// <param name="pipelineName">The name of the pipeline (e.g., "fast", "precise").</param>
    /// <returns>The configured RAG pipeline.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the pipeline name is not registered.</exception>
    public IRagPipeline Create(string pipelineName)
    {
        if (!_registrations.TryGetValue(pipelineName, out var factory))
        {
            throw new InvalidOperationException(
                $"Pipeline '{pipelineName}' not registered. " +
                $"Available: {string.Join(", ", _registrations.Keys)}");
        }

        var pipeline = factory(_serviceProvider);
        return pipeline;
    }
}
