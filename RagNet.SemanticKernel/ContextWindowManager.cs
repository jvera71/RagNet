using Microsoft.ML.Tokenizers;
using RagNet.Abstractions;
using RagNet.SemanticKernel.Options;

namespace RagNet.SemanticKernel;

/// <summary>
/// Manages the context window size, ensuring the retrieved documents
/// fit within the LLM's token limit.
/// </summary>
public class ContextWindowManager
{
    private readonly Tokenizer _tokenizer;
    private readonly int _maxContextTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextWindowManager"/> class.
    /// </summary>
    /// <param name="options">The generator options containing limits.</param>
    public ContextWindowManager(SemanticKernelGeneratorOptions options)
    {
        // Fallback to a basic tokenizer if Tiktoken is unavailable for the model.
        // In a real scenario, you'd handle loading correctly.
        _tokenizer = TiktokenTokenizer.CreateForModel(options.TokenizerModel);
        _maxContextTokens = options.MaxContextTokens;
    }

    /// <summary>
    /// Counts the tokens of a given text.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>The number of tokens.</returns>
    public int CountTokens(string text)
    {
        return _tokenizer.CountTokens(text);
    }

    /// <summary>
    /// Checks if the context fits within the available window.
    /// </summary>
    /// <param name="documents">The documents to check.</param>
    /// <returns>True if it fits, otherwise false.</returns>
    public bool FitsInWindow(IEnumerable<RagDocument> documents)
    {
        var totalTokens = documents.Sum(d => CountTokens(d.Content));
        return totalTokens <= _maxContextTokens;
    }

    /// <summary>
    /// Truncates the documents to fit the token budget, prioritizing the most relevant ones.
    /// </summary>
    /// <param name="documents">The documents to truncate.</param>
    /// <returns>The truncated collection of documents.</returns>
    public IEnumerable<RagDocument> TruncateToFit(IEnumerable<RagDocument> documents)
    {
        var tokenBudget = _maxContextTokens;
        var result = new List<RagDocument>();

        foreach (var doc in documents)
        {
            var docTokens = CountTokens(doc.Content);

            if (docTokens <= tokenBudget)
            {
                result.Add(doc);
                tokenBudget -= docTokens;
            }
            else if (tokenBudget > 100) // Minimum space for a partial fragment
            {
                // Truncate the document text to fit the remaining budget
                var truncatedContent = TruncateText(doc.Content, tokenBudget);
                result.Add(doc with { Content = truncatedContent + "..." });
                break;
            }
            else
            {
                break; // Out of budget
            }
        }

        return result;
    }

    private string TruncateText(string text, int maxTokens)
    {
        var tokens = _tokenizer.EncodeToIds(text);
        if (tokens.Count <= maxTokens)
            return text;

        var truncatedTokens = tokens.Take(maxTokens).ToList();
        return _tokenizer.Decode(truncatedTokens) ?? string.Empty;
    }
}
