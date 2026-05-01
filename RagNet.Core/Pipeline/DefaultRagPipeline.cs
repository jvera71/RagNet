using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using RagNet.Abstractions;
using RagNet.Core.Diagnostics;

namespace RagNet.Core.Pipeline;

/// <summary>
/// Default implementation of the RAG Pipeline executing a chain of middlewares.
/// </summary>
public class DefaultRagPipeline : IRagPipeline
{
    private readonly RagPipelineDelegate _pipeline;
    private readonly ILogger<DefaultRagPipeline>? _logger;
    private readonly string _pipelineName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRagPipeline"/> class.
    /// </summary>
    /// <param name="pipeline">The composed pipeline delegate.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <param name="pipelineName">The name of the pipeline.</param>
    public DefaultRagPipeline(
        RagPipelineDelegate pipeline, 
        ILogger<DefaultRagPipeline>? logger = null,
        string pipelineName = "default")
    {
        _pipeline = pipeline;
        _logger = logger;
        _pipelineName = pipelineName;
    }

    /// <summary>
    /// Executes the pipeline sequentially.
    /// </summary>
    /// <param name="query">User query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated response.</returns>
    public async Task<RagResponse> ExecuteAsync(string query, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        using var activity = RagNetActivitySources.Pipeline.StartActivity(
            "RagNet.Pipeline.Execute",
            ActivityKind.Server);

        activity?.SetTag("ragnet.query.original", query);
        activity?.SetTag("ragnet.query.length", query.Length);

        _logger?.LogInformation(
            "RagNet pipeline '{PipelineName}' started for query: {Query}",
            _pipelineName, query);

        try
        {
            var context = new RagPipelineContext
            {
                OriginalQuery = query,
                CancellationToken = ct
            };

            var response = await _pipeline(context);

            activity?.SetStatus(ActivityStatusCode.Ok);
            
            _logger?.LogInformation(
                "RagNet pipeline '{PipelineName}' completed in {ElapsedMs}ms. " +
                "Documents: {DocCount}, Citations: {CitationCount}",
                _pipelineName, sw.ElapsedMilliseconds, context.RetrievedDocuments.Count(), response.Citations.Count);

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            
            _logger?.LogError(ex, 
                "RagNet pipeline '{PipelineName}' failed: {ErrorMessage}", 
                _pipelineName, ex.Message);
                
            throw;
        }
        finally
        {
            sw.Stop();
            RagNetMetrics.QueriesProcessed.Add(1, new KeyValuePair<string, object?>("pipeline", _pipelineName));
            RagNetMetrics.QueryLatency.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("pipeline", _pipelineName));
        }
    }

    /// <summary>
    /// Executes the pipeline and returns a streaming response.
    /// </summary>
    /// <param name="query">User query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An asynchronous stream of response fragments.</returns>
    public async IAsyncEnumerable<StreamingRagResponse> ExecuteStreamingAsync(
        string query, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // For streaming, the pipeline executes up to the generator,
        // and the generator returns the IAsyncEnumerable directly.
        // A complete implementation would adapt the pipeline to handle streaming correctly,
        // perhaps by storing an IAsyncEnumerable in the context or returning it from a specialized delegate.
        // For this demo structure, we yield a placeholder or invoke a streaming path.
        
        yield return new StreamingRagResponse
        {
            ContentFragment = "Streaming is managed by the Generator inside the pipeline.",
            IsComplete = true
        };
        
        await Task.CompletedTask;
    }
}
