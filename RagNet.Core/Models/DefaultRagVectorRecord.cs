using Microsoft.Extensions.VectorData;

namespace RagNet.Core.Models;

/// <summary>
/// Default vector record for storing RAG chunks.
/// Compatible with any IVectorStore provider via MEVD.
/// </summary>
public class DefaultRagVectorRecord
{
    [VectorStoreRecordKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string Content { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string Source { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string Section { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public string Keywords { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string Summary { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string EntitiesJson { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string MetadataJson { get; set; } = string.Empty;

    [VectorStoreRecordVector(Dimensions = 1536, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
