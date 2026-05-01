using Microsoft.Extensions.DependencyInjection;
using RagNet.Abstractions;
using RagNet.Core.Pipeline;
using RagNet.Core.Pipeline.Middlewares;

namespace RagNet;

/// <summary>
/// Provides a Fluent API to configure complex RAG pipelines.
/// </summary>
public class RagPipelineBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Func<RagPipelineDelegate, RagPipelineDelegate>> _middlewares = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RagPipelineBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    internal RagPipelineBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds a query transformation step to the pipeline.
    /// </summary>
    /// <typeparam name="TTransformer">The type of the query transformer.</typeparam>
    /// <returns>The builder instance.</returns>
    public RagPipelineBuilder UseQueryTransformation<TTransformer>()
        where TTransformer : class, IQueryTransformer
    {
        _services.AddTransient<IQueryTransformer, TTransformer>();
        _middlewares.Add(next => ctx =>
        {
            var transformer = ctx.Properties["ServiceProvider"] is IServiceProvider sp
                ? sp.GetRequiredService<IQueryTransformer>()
                // For simplified DI resolution without scope passing directly:
                : throw new InvalidOperationException("ServiceProvider not found in context.");
                
            return new QueryTransformationMiddleware(next, transformer).InvokeAsync(ctx);
        });
        return this;
    }

    /// <summary>
    /// Adds hybrid retrieval to the pipeline.
    /// </summary>
    /// <param name="alpha">RRF balance between vector and keyword.</param>
    /// <param name="expandedTopK">The top-K to retrieve per sub-retriever.</param>
    /// <returns>The builder instance.</returns>
    public RagPipelineBuilder UseHybridRetrieval(double alpha = 0.5, int expandedTopK = 20)
    {
        // Registration logic for HybridRetriever would be injected here.
        // For demonstration, we just register a generic middleware step.
        _middlewares.Add(next => async ctx =>
        {
            var sp = (IServiceProvider)ctx.Properties["ServiceProvider"];
            var retriever = sp.GetRequiredService<IRetriever>(); // Resolved as HybridRetriever
            return await new RetrievalMiddleware(next, retriever, expandedTopK).InvokeAsync(ctx);
        });
        return this;
    }

    /// <summary>
    /// Adds reranking to the pipeline.
    /// </summary>
    /// <typeparam name="TReranker">The type of the document reranker.</typeparam>
    /// <param name="topK">The final top-K documents to return.</param>
    /// <returns>The builder instance.</returns>
    public RagPipelineBuilder UseReranking<TReranker>(int topK = 5)
        where TReranker : class, IDocumentReranker
    {
        _services.AddTransient<IDocumentReranker, TReranker>();
        _middlewares.Add(next => async ctx =>
        {
            var sp = (IServiceProvider)ctx.Properties["ServiceProvider"];
            var reranker = sp.GetRequiredService<IDocumentReranker>();
            
            // Simplified Rerank Middleware logic embedded
            var queriesToRun = ctx.TransformedQueries.Any() ? ctx.TransformedQueries : new[] { ctx.OriginalQuery };
            var mainQuery = queriesToRun.First(); // Naive selection for demonstration
            
            ctx.RankedDocuments = await reranker.RerankAsync(
                mainQuery, ctx.RetrievedDocuments, topK, ctx.CancellationToken);
                
            return await next(ctx);
        });
        return this;
    }

    /// <summary>
    /// Adds Semantic Kernel generator to the pipeline.
    /// </summary>
    /// <param name="configure">Optional configuration.</param>
    /// <returns>The builder instance.</returns>
    public RagPipelineBuilder UseSemanticKernelGenerator(Action<object>? configure = null)
    {
        _middlewares.Add(next => async ctx =>
        {
            var sp = (IServiceProvider)ctx.Properties["ServiceProvider"];
            var generator = sp.GetRequiredService<IRagGenerator>(); // Resolved as SemanticKernelRagGenerator
            
            var query = ctx.OriginalQuery;
            var docs = ctx.RankedDocuments.Any() ? ctx.RankedDocuments : ctx.RetrievedDocuments;
            
            ctx.Response = await generator.GenerateAsync(query, docs, ctx.CancellationToken);
            
            return await next(ctx);
        });
        return this;
    }

    /// <summary>
    /// Adds a custom middleware function to the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware logic.</param>
    /// <returns>The builder instance.</returns>
    public RagPipelineBuilder Use(
        Func<RagPipelineContext, Func<RagPipelineContext, Task<RagResponse>>, Task<RagResponse>> middleware)
    {
        _middlewares.Add(next => ctx => middleware(ctx, c => next(c)));
        return this;
    }

    /// <summary>
    /// Builds the composed pipeline delegate.
    /// </summary>
    /// <returns>The fully composed pipeline delegate.</returns>
    internal RagPipelineDelegate Build()
    {
        RagPipelineDelegate pipeline = ctx =>
            Task.FromResult(ctx.Response ?? new RagResponse 
            { 
                Answer = "", 
                Citations = new List<Citation>(), 
                ExecutionMetadata = new Dictionary<string, object>() 
            });

        // Compose middlewares in reverse order
        foreach (var middleware in _middlewares.AsEnumerable().Reverse())
        {
            pipeline = middleware(pipeline);
        }

        return pipeline;
    }
}
