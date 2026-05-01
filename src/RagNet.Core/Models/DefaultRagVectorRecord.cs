using System;
using Microsoft.Extensions.VectorData;

namespace RagNet.Core.Models;

/// <summary>
/// Default strongly-typed record for vector stores mapped to MEVD attributes.
/// </summary>
public class DefaultRagVectorRecord
{
    /// <summary>Unique identifier of the record.</summary>
    [VectorStoreKey]
    public string Id { get; set; } = string.Empty;

    /// <summary>Main text content.</summary>
    [VectorStoreData(IsIndexed = true)]
    public string Content { get; set; } = string.Empty;

    /// <summary>Source of the document.</summary>
    [VectorStoreData(IsIndexed = true)]
    public string Source { get; set; } = string.Empty;

    /// <summary>Extracted keywords for full-text search.</summary>
    [VectorStoreData(IsFullTextIndexed = true)]
    public string Keywords { get; set; } = string.Empty;

    /// <summary>Generated summary of the content.</summary>
    [VectorStoreData]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Section of the document.</summary>
    [VectorStoreData(IsIndexed = true)]
    public string Section { get; set; } = string.Empty;

    /// <summary>Serialized JSON containing entities.</summary>
    [VectorStoreData]
    public string EntitiesJson { get; set; } = string.Empty;

    /// <summary>Serialized JSON containing additional dynamic metadata.</summary>
    [VectorStoreData]
    public string MetadataJson { get; set; } = string.Empty;

    /// <summary>
    /// Vector embedding of the content.
    /// Default dimensions are 1536 (typical for text-embedding-ada-002 or text-embedding-3-small),
    /// but users should define custom records if using different models.
    /// </summary>
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
