using Microsoft.Extensions.AI;
using RagNet.Abstractions;

namespace RagNet.Core.Retrieval.Transformers;

/// <summary>
/// A query transformer that translates multi-lingual queries to English
/// to improve vector retrieval effectiveness.
/// </summary>
public class TranslationTransformer : IQueryTransformer
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationTransformer"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used for translation.</param>
    public TranslationTransformer(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    /// <summary>
    /// Translates the original query to English.
    /// </summary>
    /// <param name="originalQuery">The original user query.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Both the original query and the translated query.</returns>
    public async Task<IEnumerable<string>> TransformAsync(
        string originalQuery, CancellationToken ct = default)
    {
        var prompt = $"Translate the following to English. Reply with ONLY the translation:\n\"{originalQuery}\"";
        
        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: ct);
        
        var translated = response.Text?.Trim() ?? originalQuery;

        // Return both: original (useful for keyword search) + translated (useful for vector search)
        return new[] { originalQuery, translated }.Distinct();
    }
}
