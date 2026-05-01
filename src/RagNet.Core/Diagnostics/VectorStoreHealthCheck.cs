using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.VectorData;
using RagNet.Core.Models;

namespace RagNet.Core.Diagnostics;

/// <summary>
/// Health check that verifies connectivity with the VectorStore.
/// </summary>
public class VectorStoreHealthCheck : IHealthCheck
{
    private readonly VectorStore _vectorStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreHealthCheck"/> class.
    /// </summary>
    /// <param name="vectorStore">The vector store to check.</param>
    public VectorStoreHealthCheck(VectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    /// <summary>
    /// Executes the health check logic.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            // Attempt to list collections as a connectivity test
            // Note: Since ListCollectionNamesAsync returns IAsyncEnumerable, we read the first one 
            // or simply iterate to ensure the connection is active.
            await foreach (var collectionName in _vectorStore.ListCollectionNamesAsync(ct).WithCancellation(ct))
            {
                break;
            }
            return HealthCheckResult.Healthy("VectorStore is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("VectorStore is unreachable", ex);
        }
    }
}
