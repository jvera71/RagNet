namespace RagNet.Abstractions;

/// <summary>
/// Defines a contract for mapping between internal <see cref="RagDocument"/>
/// and provider-specific vector store records.
/// </summary>
/// <typeparam name="TRecord">The concrete type of the vector store record.</typeparam>
public interface IRagDocumentMapper<TRecord>
{
    /// <summary>
    /// Converts a <see cref="RagDocument"/> to the specific record type.
    /// </summary>
    /// <param name="document">The abstract RAG document.</param>
    /// <returns>The concrete record.</returns>
    TRecord ToRecord(RagDocument document);

    /// <summary>
    /// Converts a specific record type back to a <see cref="RagDocument"/>.
    /// </summary>
    /// <param name="record">The concrete record.</param>
    /// <returns>The abstract RAG document.</returns>
    RagDocument FromRecord(TRecord record);
}
