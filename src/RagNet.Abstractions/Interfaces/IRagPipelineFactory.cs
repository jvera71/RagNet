namespace RagNet.Abstractions;

/// <summary>
/// Factory to resolve RAG pipelines registered by name.
/// </summary>
public interface IRagPipelineFactory
{
    /// <summary>
    /// Creates or resolves a RAG pipeline by its registered name.
    /// </summary>
    /// <param name="pipelineName">Name of the configured pipeline.</param>
    /// <returns>Instance of the configured pipeline.</returns>
    IRagPipeline Create(string pipelineName);
}
