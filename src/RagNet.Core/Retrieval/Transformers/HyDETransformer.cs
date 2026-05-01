using Microsoft.Extensions.AI;
using RagNet.Abstractions;

namespace RagNet.Core.Retrieval.Transformers;

/// <summary>
/// Hypothetical Document Embeddings (HyDE) Transformer.
/// Generates a hypothetical response/document using an LLM to be used
/// as the search query, improving recall by matching document structure/vocabulary.
/// </summary>
public class HyDETransformer : IQueryTransformer
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="HyDETransformer"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for generating the hypothetical document.</param>
    public HyDETransformer(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <summary>
    /// Transforms the query by generating a hypothetical document.
    /// </summary>
    /// <param name="originalQuery">Original user query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The hypothetical document to be used as a query.</returns>
    public async Task<IEnumerable<string>> TransformAsync(string originalQuery, CancellationToken ct = default)
    {
        var prompt = $@"Write a paragraph of technical documentation that would directly answer the following question.
The paragraph should be informative and detailed, as if it were a fragment of a real technical manual.
Do not include any introductory phrases, just the documentation text.

Question: ""{originalQuery}""
Documentation paragraph:";

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: ct);
        var hypotheticalDocument = response.Text?.Trim();

        // If the LLM fails to generate something meaningful, fallback to the original query
        if (string.IsNullOrWhiteSpace(hypotheticalDocument))
        {
            return new[] { originalQuery };
        }

        return new[] { hypotheticalDocument };
    }
}
