using System.Diagnostics;
using OpenTelemetry.Trace;
using RagNet.Abstractions;
using RagNet.Core.Diagnostics;

namespace RagNet.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware for generating the final response using the retrieved documents as context.
/// </summary>
public class GenerationMiddleware
{
    private readonly RagPipelineDelegate _next;
    private readonly IRagGenerator _generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline (often terminal).</param>
    /// <param name="generator">The generator used to synthesize the response.</param>
    public GenerationMiddleware(RagPipelineDelegate next, IRagGenerator generator)
    {
        _next = next;
        _generator = generator;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>The generated RAG response.</returns>
    public async Task<RagResponse> InvokeAsync(RagPipelineContext context)
    {
        using var activity = RagNetActivitySources.Generation.StartActivity(
            "RagNet.Generation.Execute",
            ActivityKind.Internal);

        activity?.SetTag("ragnet.query.original", context.OriginalQuery);
        activity?.SetTag("ragnet.generation.context_count", context.RetrievedDocuments.Count());

        try
        {
            var response = await _generator.GenerateAsync(
                context.OriginalQuery, 
                context.RetrievedDocuments, 
                context.CancellationToken);

            activity?.SetTag("ragnet.generation.citation_count", response.Citations.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
