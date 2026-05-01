using Microsoft.Extensions.AI;
using RagNet.Abstractions;

namespace RagNet.Core.Retrieval.Transformers;

/// <summary>
/// Generates a more abstract or general version of a specific query
/// to capture the underlying concept or principle.
/// </summary>
public class StepBackTransformer : IQueryTransformer
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepBackTransformer"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for abstracting the query.</param>
    public StepBackTransformer(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <summary>
    /// Transforms the query by providing the original query along with a generalized version.
    /// </summary>
    /// <param name="originalQuery">Original user query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection containing both the original and the generalized query.</returns>
    public async Task<IEnumerable<string>> TransformAsync(string originalQuery, CancellationToken ct = default)
    {
        var prompt = $@"Given the following specific question, formulate a more general question
that captures the underlying principle or concept.
Respond ONLY with the generalized question, without any explanations.

Specific question: ""{originalQuery}""
Generalized question:";

        var response = await _chatClient.CompleteAsync(prompt, cancellationToken: ct);
        var generalizedQuery = response.Message.Text?.Trim();

        var queries = new List<string> { originalQuery };

        if (!string.IsNullOrWhiteSpace(generalizedQuery) && generalizedQuery != originalQuery)
        {
            queries.Add(generalizedQuery);
        }

        return queries;
    }
}
