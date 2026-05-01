using Microsoft.Extensions.AI;
using RagNet.Abstractions;

namespace RagNet.Core.Retrieval.Transformers;

/// <summary>
/// Rewrites ambiguous or poorly formulated user queries into clearer,
/// more specific versions optimized for retrieval.
/// </summary>
public class QueryRewriter : IQueryTransformer
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryRewriter"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for rewriting the query.</param>
    public QueryRewriter(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <summary>
    /// Transforms an original query into an optimized query.
    /// </summary>
    /// <param name="originalQuery">Original user query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A single rewritten query.</returns>
    public async Task<IEnumerable<string>> TransformAsync(string originalQuery, CancellationToken ct = default)
    {
        var prompt = $@"You are an expert in search query optimization.
Rewrite the following user query to be more specific and effective for searching in a technical knowledge base.
Respond ONLY with the rewritten query, without any explanations.

Original query: ""{originalQuery}""
Rewritten query:";

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: ct);
        var rewrittenQuery = response.Text?.Trim() ?? originalQuery;

        return new[] { rewrittenQuery };
    }
}
