using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using RagNet.Abstractions;
using RagNet.SemanticKernel.Options;

namespace RagNet.SemanticKernel;

/// <summary>
/// Generator that uses Microsoft Semantic Kernel to render prompts
/// and invoke the LLM, producing both complete and streaming responses.
/// </summary>
public class SemanticKernelRagGenerator : IRagGenerator
{
    private readonly Kernel _kernel;
    private readonly SemanticKernelGeneratorOptions _options;
    private readonly ContextWindowManager _contextWindowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticKernelRagGenerator"/> class.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance.</param>
    /// <param name="options">The generator options.</param>
    public SemanticKernelRagGenerator(
        Kernel kernel,
        IOptions<SemanticKernelGeneratorOptions> options)
    {
        _kernel = kernel;
        _options = options.Value;
        _contextWindowManager = new ContextWindowManager(_options);
    }

    /// <summary>
    /// Generates a complete RAG response for the given query and context.
    /// </summary>
    /// <param name="query">The user's query.</param>
    /// <param name="context">The retrieved context documents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A fully generated RAG response.</returns>
    public async Task<RagResponse> GenerateAsync(
        string query, 
        IEnumerable<RagDocument> context,
        CancellationToken ct = default)
    {
        // 1. Validate that the context fits in the LLM window
        var fittedContext = await FitContextWindow(context, ct);

        // 2. Render the prompt with the SK template engine
        var prompt = await RenderPrompt(query, fittedContext);

        // 3. Invoke the LLM via Kernel
        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: ct);

        // 4. Extract citations from the used context
        var answer = result.GetValue<string>() ?? string.Empty;
        var citations = ExtractCitations(fittedContext, answer);

        // 5. Build RagResponse
        var response = new RagResponse
        {
            Answer = answer,
            Citations = citations,
            ExecutionMetadata = CollectMetadata(result)
        };

        // 6. Validate Self-RAG
        if (_options.EnableSelfRagValidation)
        {
            response = await ValidateSelfRag(response, fittedContext, ct);
        }

        return response;
    }

    /// <summary>
    /// Generates a streaming RAG response for the given query and context.
    /// </summary>
    /// <param name="query">The user's query.</param>
    /// <param name="context">The retrieved context documents.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An asynchronous stream of response fragments.</returns>
    public async IAsyncEnumerable<StreamingRagResponse> GenerateStreamingAsync(
        string query, 
        IEnumerable<RagDocument> context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var fittedContext = await FitContextWindow(context, ct);
        var prompt = await RenderPrompt(query, fittedContext);

        // Streaming via SK
        var streamingResult = _kernel.InvokePromptStreamingAsync(prompt, cancellationToken: ct);
        
        await foreach (var chunk in streamingResult)
        {
            yield return new StreamingRagResponse
            {
                ContentFragment = chunk.ToString(),
                IsComplete = false
            };
        }

        // Final fragment with citations
        yield return new StreamingRagResponse
        {
            ContentFragment = string.Empty,
            IsComplete = true,
            Citations = ExtractCitations(fittedContext)
        };
    }

    private Task<IEnumerable<RagDocument>> FitContextWindow(IEnumerable<RagDocument> context, CancellationToken ct)
    {
        if (_contextWindowManager.FitsInWindow(context))
        {
            return Task.FromResult(context);
        }
        
        // Truncate to fit
        return Task.FromResult(_contextWindowManager.TruncateToFit(context));
    }

    private async Task<string> RenderPrompt(string query, IEnumerable<RagDocument> context)
    {
        // Build numbered context block
        var contextBuilder = new StringBuilder();
        int index = 1;
        foreach (var doc in context)
        {
            contextBuilder.AppendLine($"[{index}] Source: {doc.Metadata.GetValueOrDefault("source", "Unknown")}");
            contextBuilder.AppendLine($"    Section: {doc.Metadata.GetValueOrDefault("section", "")}");
            contextBuilder.AppendLine($"    Content: {doc.Content}");
            contextBuilder.AppendLine();
            index++;
        }

        // Add System Prompt Template at the beginning or as part of settings if using Chat History
        var fullTemplate = $"{_options.SystemPromptTemplate}\n\n{_options.UserPromptTemplate}";

        // Render with SK template engine
        var templateConfig = new PromptTemplateConfig(fullTemplate);
        var template = new KernelPromptTemplateFactory().Create(templateConfig);

        return await template.RenderAsync(_kernel, new KernelArguments
        {
            ["context"] = contextBuilder.ToString(),
            ["query"] = query
        }, cancellationToken: default);
    }

    private IReadOnlyList<Citation> ExtractCitations(IEnumerable<RagDocument> context, string answer = "")
    {
        var allCitations = PrepareCitations(context);

        if (string.IsNullOrWhiteSpace(answer))
        {
            return allCitations;
        }

        // Detect which citation numbers appear in the answer (e.g. [1], [2])
        var citedIndices = Regex.Matches(answer, @"\[(\d+)\]")
            .Select(m => int.Parse(m.Groups[1].Value) - 1)
            .Where(i => i >= 0 && i < allCitations.Count)
            .Distinct()
            .ToHashSet();

        // Return only effectively used citations
        return allCitations
            .Where((_, index) => citedIndices.Contains(index))
            .ToList();
    }

    private IReadOnlyList<Citation> PrepareCitations(IEnumerable<RagDocument> context)
    {
        return context.Select((doc, index) => new Citation(
            DocumentId: doc.Id,
            SourceContent: doc.Content.Length > 200 ? doc.Content[..200] + "..." : doc.Content,
            RelevanceScore: (double)(doc.Metadata.GetValueOrDefault("_score", 0.0) ?? 0.0),
            Metadata: new Dictionary<string, object>(doc.Metadata)
            {
                ["source"] = doc.Metadata.GetValueOrDefault("source", "Unknown")?.ToString() ?? "Unknown",
                ["referenceNumber"] = index + 1
            }
        )).ToList();
    }

    private async Task<RagResponse> ValidateSelfRag(
        RagResponse response, IEnumerable<RagDocument> context, CancellationToken ct)
    {
        var validationPrompt = $$"""
            Analyze the following answer and verify that each claim is supported by the provided context.

            CONTEXT:
            {{FormatContext(context)}}

            ANSWER TO VALIDATE:
            {{response.Answer}}

            For each claim in the answer, indicate:
            - "SUPPORTED" if it is supported by the context
            - "NOT_SUPPORTED" if it is NOT supported
            - "PARTIAL" if it is partially supported

            Respond ONLY in JSON format like this:
            [{"claim": "...", "status": "SUPPORTED|NOT_SUPPORTED|PARTIAL", "evidence": "fragment from context"}]
            """;

        try
        {
            var validationResult = await _kernel.InvokePromptAsync(validationPrompt, cancellationToken: ct);
            var json = validationResult.GetValue<string>();
            
            // In a complete implementation, this JSON would be parsed and action taken based on the HallucinationStrategy.
            // For now, we append the validation output to metadata.
            var newMetadata = new Dictionary<string, object>(response.ExecutionMetadata)
            {
                ["selfRagValidation"] = json ?? "No validation result"
            };

            return response with { ExecutionMetadata = newMetadata };
        }
        catch
        {
            return response;
        }
    }

    private string FormatContext(IEnumerable<RagDocument> context)
    {
        var contextBuilder = new StringBuilder();
        foreach (var doc in context)
        {
            contextBuilder.AppendLine(doc.Content);
        }
        return contextBuilder.ToString();
    }

    private Dictionary<string, object> CollectMetadata(FunctionResult result)
    {
        // Extract metadata from the result if available (like token usage)
        var metadata = new Dictionary<string, object>();
        if (result.Metadata != null)
        {
            foreach (var kvp in result.Metadata)
            {
                if (kvp.Value != null)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }
        }
        return metadata;
    }
}
