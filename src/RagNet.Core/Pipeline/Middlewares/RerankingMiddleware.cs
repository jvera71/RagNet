using System.Diagnostics;
using OpenTelemetry.Trace;
using RagNet.Abstractions;
using RagNet.Core.Diagnostics;

namespace RagNet.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware for reranking the retrieved documents using a cross-encoder or LLM.
/// </summary>
public class RerankingMiddleware
{
    private readonly RagPipelineDelegate _next;
    private readonly IDocumentReranker _reranker;
    private readonly int _topK;

    /// <summary>
    /// Initializes a new instance of the <see cref="RerankingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="reranker">The document reranker.</param>
    /// <param name="topK">The maximum number of documents to keep after reranking.</param>
    public RerankingMiddleware(
        RagPipelineDelegate next, IDocumentReranker reranker, int topK = 5)
    {
        _next = next;
        _reranker = reranker;
        _topK = topK;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The pipeline context.</param>
    /// <returns>The pipeline response.</returns>
    public async Task<RagResponse> InvokeAsync(RagPipelineContext context)
    {
        using var activity = RagNetActivitySources.Retrieval.StartActivity(
            "RagNet.Retrieval.Rerank",
            ActivityKind.Internal);

        activity?.SetTag("ragnet.query.original", context.OriginalQuery);
        activity?.SetTag("ragnet.rerank.input_count", context.RetrievedDocuments.Count());

        try
        {
            if (context.RetrievedDocuments.Any())
            {
                var rerankedDocs = await _reranker.RerankAsync(
                    context.OriginalQuery, 
                    context.RetrievedDocuments, 
                    _topK, 
                    context.CancellationToken);

                context.RetrievedDocuments = rerankedDocs.ToList();
            }

            activity?.SetTag("ragnet.rerank.output_count", context.RetrievedDocuments.Count());
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Pass control to the next middleware
            return await _next(context);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
