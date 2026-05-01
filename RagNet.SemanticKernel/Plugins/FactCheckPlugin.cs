using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace RagNet.SemanticKernel.Plugins;

/// <summary>
/// Validation plugin - verifies claims against the context.
/// </summary>
public class FactCheckPlugin
{
    /// <summary>
    /// Verifies if a claim is supported by the context.
    /// </summary>
    /// <param name="claim">The claim to verify.</param>
    /// <param name="context">The context to check against.</param>
    /// <returns>"VERIFIED" if the context supports the claim, otherwise "UNVERIFIED".</returns>
    [KernelFunction("verify_claim")]
    [Description("Verifies if a claim is supported by the context")]
    public string VerifyClaim(string claim, string context)
    {
        if (string.IsNullOrWhiteSpace(claim) || string.IsNullOrWhiteSpace(context))
        {
            return "UNVERIFIED";
        }

        // Extremely simplified fuzzy/substring search for demonstration.
        // In a real scenario, this could use semantic similarity or another LLM call.
        return context.Contains(claim, StringComparison.OrdinalIgnoreCase)
            ? "VERIFIED" 
            : "UNVERIFIED";
    }
}
