using Microsoft.Extensions.AI;
using RagNet.Abstractions;

namespace RagNet.Core.Ingestion;

/// <summary>
/// Helper class to divide documents into batches and generate embeddings
/// respecting the limits of the embedding provider.
/// </summary>
public class EmbeddingBatcher
{
    private readonly int _maxBatchSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingBatcher"/> class.
    /// </summary>
    /// <param name="maxBatchSize">Maximum number of documents per batch.</param>
    public EmbeddingBatcher(int maxBatchSize = 50)
    {
        _maxBatchSize = maxBatchSize;
    }

    /// <summary>
    /// Generates embeddings for documents in batches.
    /// </summary>
    /// <param name="documents">The documents to embed.</param>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Documents with their generated vectors assigned.</returns>
    public async Task<IEnumerable<RagDocument>> EmbedInBatchesAsync(
        IEnumerable<RagDocument> documents,
        IEmbeddingGenerator<string, Embedding<float>> generator,
        CancellationToken ct)
    {
        var results = new List<RagDocument>();
        var batches = documents.Chunk(_maxBatchSize);

        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();
            var texts = batch.Select(d => d.Content).ToList();
            
            // Generate embeddings for the current batch
            var embeddings = await generator.GenerateAsync(texts, cancellationToken: ct);
            
            // Assign vectors back to documents
            results.AddRange(batch.Zip(embeddings, (doc, emb) =>
                doc with { Vector = emb.Vector }));
        }

        return results;
    }
}
