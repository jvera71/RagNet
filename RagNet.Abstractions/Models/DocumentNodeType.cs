namespace RagNet.Abstractions;

/// <summary>
/// Structural node types recognized by parsers.
/// </summary>
public enum DocumentNodeType
{
    /// <summary>Document root</summary>
    Document,
    /// <summary>Section delimited by a header</summary>
    Section,
    /// <summary>Heading (H1-H6)</summary>
    Heading,
    /// <summary>Paragraph of text</summary>
    Paragraph,
    /// <summary>List (ordered or unordered)</summary>
    List,
    /// <summary>List item</summary>
    ListItem,
    /// <summary>Complete table</summary>
    Table,
    /// <summary>Table row</summary>
    TableRow,
    /// <summary>Code block</summary>
    CodeBlock,
    /// <summary>Quote or blockquote</summary>
    Quote,
    /// <summary>Image reference</summary>
    Image
}
