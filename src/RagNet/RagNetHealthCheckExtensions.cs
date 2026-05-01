using Microsoft.Extensions.DependencyInjection;
using RagNet.Core.Diagnostics;

namespace RagNet;

/// <summary>
/// Extension methods for configuring RagNet health checks.
/// </summary>
public static class RagNetHealthCheckExtensions
{
    /// <summary>
    /// Registers all RagNet health checks.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <returns>The builder instance for chaining.</returns>
    public static IHealthChecksBuilder AddRagNetHealthChecks(
        this IHealthChecksBuilder builder)
    {
        return builder
            .AddCheck<VectorStoreHealthCheck>("ragnet-vectorstore", tags: new[] { "ragnet" })
            .AddCheck<LlmProviderHealthCheck>("ragnet-llm", tags: new[] { "ragnet" });
    }
}
