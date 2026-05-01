using OpenTelemetry.Trace;
using RagNet.Core.Diagnostics;

namespace RagNet;

/// <summary>
/// Extension methods for configuring RagNet telemetry.
/// </summary>
public static class RagNetTelemetryExtensions
{
    /// <summary>
    /// Registers all RagNet activity sources into OpenTelemetry.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <returns>The builder instance for chaining.</returns>
    public static TracerProviderBuilder AddRagNetInstrumentation(
        this TracerProviderBuilder builder)
    {
        foreach (var source in RagNetActivitySources.AllSourceNames)
        {
            builder.AddSource(source);
        }
        return builder;
    }
}
