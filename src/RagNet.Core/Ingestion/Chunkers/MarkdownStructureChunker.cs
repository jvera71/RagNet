using System.Text;
using Microsoft.Extensions.Options;
using RagNet.Abstractions;
using RagNet.Core.Options;

namespace RagNet.Core.Ingestion.Chunkers;

/// <summary>
/// Splits a parsed document based on its structural hierarchy (e.g., headings).
/// Each section defined by a heading is a candidate chunk.
/// </summary>
public class MarkdownStructureChunker : ISemanticChunker
{
    private readonly MarkdownStructureChunkerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownStructureChunker"/> class.
    /// </summary>
    /// <param name="options">The chunker options.</param>
    public MarkdownStructureChunker(IOptions<MarkdownStructureChunkerOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Splits a parsed document into semantic fragments based on structure.
    /// </summary>
    /// <param name="rootNode">Root node of the parsed document.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of RAG documents (chunks) without assigned vectors.</returns>
    public Task<IEnumerable<RagDocument>> ChunkAsync(DocumentNode rootNode, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var chunks = new List<RagDocument>();
            var breadcrumb = new List<string>();

            ProcessNode(rootNode, breadcrumb, chunks, ct);

            // Merge small chunks if needed (simplified post-processing)
            var mergedChunks = MergeSmallChunks(chunks);

            return (IEnumerable<RagDocument>)mergedChunks;
        }, ct);
    }

    private void ProcessNode(DocumentNode node, List<string> currentBreadcrumb, List<RagDocument> chunks, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (node.NodeType == DocumentNodeType.Section || node.NodeType == DocumentNodeType.Heading)
        {
            var newBreadcrumb = new List<string>(currentBreadcrumb);
            if (!string.IsNullOrWhiteSpace(node.Content))
            {
                newBreadcrumb.Add(node.Content);
            }

            if (node.Level == _options.ChunkAtHeadingLevel)
            {
                // We reached the target split level. Collect content.
                var content = CollectContent(node);
                
                if (!string.IsNullOrWhiteSpace(content))
                {
                    string prefix = string.Empty;
                    if (_options.IncludeBreadcrumb && newBreadcrumb.Count > 0)
                    {
                        prefix = $"[{string.Join(" > ", newBreadcrumb)}] ";
                    }

                    chunks.Add(new RagDocument(
                        Id: Guid.NewGuid().ToString(),
                        Content: $"{prefix}{content}",
                        Vector: ReadOnlyMemory<float>.Empty,
                        Metadata: new Dictionary<string, object>
                        {
                            { "chunkType", "markdownStructure" },
                            { "breadcrumb", string.Join(" > ", newBreadcrumb) }
                        }
                    ));
                }
            }
            else if (node.Level < _options.ChunkAtHeadingLevel)
            {
                // Go deeper
                foreach (var child in node.Children)
                {
                    ProcessNode(child, newBreadcrumb, chunks, ct);
                }
            }
            else
            {
                // If it's deeper than target level, it will be collected by its parent Section
            }
        }
        else if (node.Level == 0 || currentBreadcrumb.Count == 0) // Root level content not in sections
        {
            foreach (var child in node.Children)
            {
                ProcessNode(child, currentBreadcrumb, chunks, ct);
            }
        }
    }

    private string CollectContent(DocumentNode node)
    {
        var sb = new StringBuilder();
        if (node.NodeType != DocumentNodeType.Section)
        {
            sb.AppendLine(node.Content);
        }

        foreach (var child in node.Children)
        {
            sb.AppendLine(CollectContent(child));
        }

        return sb.ToString().Trim();
    }

    private List<RagDocument> MergeSmallChunks(List<RagDocument> chunks)
    {
        if (chunks.Count <= 1) return chunks;

        var merged = new List<RagDocument>();
        RagDocument? current = chunks[0];

        for (int i = 1; i < chunks.Count; i++)
        {
            var next = chunks[i];
            
            if (current!.Content.Length < _options.MinChunkSize)
            {
                // Merge current with next
                current = current with { Content = current.Content + "\n" + next.Content };
            }
            else if (current.Content.Length > _options.MaxChunkSize)
            {
                // Subdivide if too large (simplified - just passing through for now, 
                // in a full implementation this would break it down further)
                merged.Add(current);
                current = next;
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }

        if (current != null)
        {
            merged.Add(current);
        }

        return merged;
    }
}
