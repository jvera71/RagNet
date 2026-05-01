using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RagNet.Core.Diagnostics;

/// <summary>
/// Health check that verifies connectivity with the LLM provider.
/// </summary>
public class LlmProviderHealthCheck : IHealthCheck
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmProviderHealthCheck"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client to check.</param>
    public LlmProviderHealthCheck(IChatClient chatClient)
    {
        _chatClient = chatClient;
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
            var response = await _chatClient.GetResponseAsync("Reply with OK", cancellationToken: ct);

            return response.Text is not null
                ? HealthCheckResult.Healthy("LLM provider is responsive")
                : HealthCheckResult.Degraded("LLM responded with empty message");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("LLM provider is unreachable", ex);
        }
    }
}
