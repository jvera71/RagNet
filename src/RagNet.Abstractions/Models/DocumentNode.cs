namespace RagNet.Abstractions;

/// <summary>
/// Node in the hierarchical tree of a parsed document.
/// Preserves the original structure of the source document.
/// </summary>
public record DocumentNode
{
    /// <summary>Structural node type.</summary>
    public required DocumentNodeType NodeType { get; init; }

    /// <summary>Text content of the node.</summary>
    public required string Content { get; init; }

    /// <summary>Hierarchical level (0 = root, 1 = H1, 2 = H2, etc.).</summary>
    public int Level { get; init; }

    /// <summary>Node metadata (source, page, position).</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>Child nodes in the hierarchy.</summary>
    public IReadOnlyList<DocumentNode> Children { get; init; } = Array.Empty<DocumentNode>();
}
