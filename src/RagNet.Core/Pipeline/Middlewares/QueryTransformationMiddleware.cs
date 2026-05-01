using RagNet.Abstractions;
using RagNet.Core.Security;

namespace RagNet.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware for transforming queries before retrieval.
/// </summary>
public class QueryTransformationMiddleware
{
    private readonly RagPipelineDelegate _next;
    private readonly IQueryTransformer _transformer;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTransformationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="transformer">The query transformer to apply.</param>
    public QueryTransformationMiddleware(
        RagPipelineDelegate next, IQueryTransformer transformer)
    {
        _next = next;
        _transformer = transformer;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>The pipeline response.</returns>
    public async Task<RagResponse> InvokeAsync(RagPipelineContext context)
    {
        // 1. Sanitize original query to prevent prompt injection
        context.OriginalQuery = InputSanitizer.Sanitize(context.OriginalQuery);

        // 2. Execute transformation
        context.TransformedQueries = await _transformer.TransformAsync(
            context.OriginalQuery, context.CancellationToken);

        // 2. Pass control to the next middleware
        return await _next(context);
    }
}
