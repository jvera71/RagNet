using System.Security;

namespace RagNet.Core.Security;

/// <summary>
/// Utility class to sanitize user inputs before they reach the LLM, 
/// preventing prompt injection attacks.
/// </summary>
public static class InputSanitizer
{
    /// <summary>
    /// Sanitizes the user input to prevent prompt injection.
    /// </summary>
    /// <param name="userInput">The raw user input.</param>
    /// <returns>The sanitized user input.</returns>
    /// <exception cref="SecurityException">Thrown if an injection pattern is detected.</exception>
    public static string Sanitize(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return string.Empty;

        // 1. Limit length
        if (userInput.Length > 2000)
            userInput = userInput[..2000];

        // 2. Escape prompt delimiters
        userInput = userInput
            .Replace("```", "")        // Code blocks
            .Replace("{{", "{")        // Semantic Kernel template delimiters
            .Replace("}}", "}")
            .Replace("SYSTEM:", "")    // Role injection attempts
            .Replace("ASSISTANT:", "");

        // 3. Detect suspicious injection patterns
        if (ContainsInjectionPattern(userInput))
        {
            throw new SecurityException("Potential prompt injection detected");
        }

        return userInput;
    }

    private static bool ContainsInjectionPattern(string input)
    {
        var patterns = new[]
        {
            "ignore previous instructions",
            "ignore all instructions",
            "you are now",
            "new instructions:",
            "override system prompt",
            "forget everything"
        };
        
        return patterns.Any(p =>
            input.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
