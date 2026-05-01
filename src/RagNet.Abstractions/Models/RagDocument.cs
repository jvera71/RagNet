namespace RagNet.Abstractions;

/// <summary>
/// Unified representation of a document fragment in the RAG system.
/// Contains textual content, its vector representation, and enriched metadata.
/// </summary>
public record RagDocument(
    string Id,
    string Content,
    ReadOnlyMemory<float> Vector,
    Dictionary<string, object> Metadata);
