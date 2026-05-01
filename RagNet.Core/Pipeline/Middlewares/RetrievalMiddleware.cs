using System.Diagnostics;
using RagNet.Abstractions;
using RagNet.Core.Diagnostics;

namespace RagNet.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware for retrieving documents based on the transformed queries.
/// </summary>
public class RetrievalMiddleware
{
    private readonly RagPipelineDelegate _next;
    private readonly IRetriever _retriever;
    private readonly int _topK;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrievalMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="retriever">The document retriever.</param>
    /// <param name="topK">The maximum number of documents to retrieve per query.</param>
    public RetrievalMiddleware(
        RagPipelineDelegate next, IRetriever retriever, int topK = 20)
    {
        _next = next;
        _retriever = retriever;
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
            "RagNet.Retrieval.Search",
            ActivityKind.Internal);

        activity?.SetTag("ragnet.query.original", context.OriginalQuery);
        activity?.SetTag("ragnet.query.transformed_count", context.TransformedQueries.Count());

        try
        {
            var allDocs = new List<RagDocument>();

            // If no queries were transformed, fallback to original query
            var queriesToRun = context.TransformedQueries.Any() 
                ? context.TransformedQueries 
                : new[] { context.OriginalQuery };

            // Search with each query
            foreach (var query in queriesToRun)
            {
                var docs = await _retriever.RetrieveAsync(query, _topK, context.CancellationToken);
                allDocs.AddRange(docs);
            }

            // Deduplicate by Id
            context.RetrievedDocuments = allDocs
                .DistinctBy(d => d.Id)
                .ToList();

            activity?.SetTag("ragnet.retrieval.document_count", context.RetrievedDocuments.Count());
            
            var topScore = context.RetrievedDocuments.FirstOrDefault()?.Metadata.GetValueOrDefault("_score");
            if (topScore != null)
            {
                activity?.SetTag("ragnet.retrieval.top_score", topScore);
                
                // Also record metric
                if (topScore is double ts)
                {
                    RagNetMetrics.TopRelevanceScore.Record(ts);
                }
            }

            RagNetMetrics.RetrievedDocumentCount.Record(context.RetrievedDocuments.Count());
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Pass control to the next middleware
            return await _next(context);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
