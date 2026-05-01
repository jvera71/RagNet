namespace RagNet.Abstractions;

/// <summary>
/// Reference to a source document used in generating the response.
/// </summary>
public record Citation(
    string DocumentId,
    string SourceContent,
    double RelevanceScore,
    Dictionary<string, object> Metadata);
