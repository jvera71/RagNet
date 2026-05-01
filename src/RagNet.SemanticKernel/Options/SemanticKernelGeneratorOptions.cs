namespace RagNet.SemanticKernel.Options;

/// <summary>
/// Configuration options for SemanticKernelRagGenerator.
/// </summary>
public class SemanticKernelGeneratorOptions
{
    /// <summary>System prompt template.</summary>
    public string SystemPromptTemplate { get; set; } =
        "You are an expert assistant. Respond ONLY based on the provided context.";

    /// <summary>User prompt template with context.</summary>
    public string UserPromptTemplate { get; set; } =
        """
        CONTEXT:
        {{context}}

        QUESTION:
        {{query}}

        INSTRUCTIONS:
        - Respond exclusively based on the provided context.
        - If the context does not contain the information, state that you do not have enough data.
        - Cite the sources using [1], [2], etc.
        """;

    /// <summary>Maximum context tokens before truncating/summarizing.</summary>
    public int MaxContextTokens { get; set; } = 6000;

    /// <summary>Tokenization model for counting tokens.</summary>
    public string TokenizerModel { get; set; } = "gpt-4";

    /// <summary>Enable Self-RAG validation (anti-hallucination).</summary>
    public bool EnableSelfRagValidation { get; set; } = false;
}
