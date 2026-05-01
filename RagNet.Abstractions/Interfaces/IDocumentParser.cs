namespace RagNet.Abstractions;

/// <summary>
/// Transforms a binary document into a hierarchical structure of nodes,
/// preserving the original structure (headings, paragraphs, lists, tables).
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// File formats supported by this parser (e.g., ".pdf", ".docx").
    /// </summary>
    IReadOnlySet<string> SupportedExtensions { get; }

    /// <summary>
    /// Parses a document from a stream and returns its hierarchical representation.
    /// </summary>
    /// <param name="documentStream">Source file stream.</param>
    /// <param name="fileName">File name (to determine format and metadata).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Root node of the document's hierarchical tree.</returns>
    Task<DocumentNode> ParseAsync(
        Stream documentStream,
        string fileName,
        CancellationToken ct = default);
}
