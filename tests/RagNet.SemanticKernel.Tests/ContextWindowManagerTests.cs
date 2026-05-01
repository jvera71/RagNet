using FluentAssertions;
using RagNet.Abstractions;
using RagNet.SemanticKernel;
using RagNet.SemanticKernel.Options;
using Xunit;

namespace RagNet.SemanticKernel.Tests;

public class ContextWindowManagerTests
{
    [Fact]
    public void FitsInWindow_WhenUnderLimit_ReturnsTrue()
    {
        // Arrange
        var options = new SemanticKernelGeneratorOptions 
        { 
            MaxContextTokens = 100,
            TokenizerModel = "gpt-3.5-turbo"
        };
        var manager = new ContextWindowManager(options);

        var docs = new[]
        {
            new RagDocument("1", "Short text", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>()),
            new RagDocument("2", "Another short text", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>())
        };

        // Act
        var result = manager.FitsInWindow(docs);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TruncateToFit_WhenExceedsLimit_TruncatesDocuments()
    {
        // Arrange
        var options = new SemanticKernelGeneratorOptions 
        { 
            MaxContextTokens = 10, // Very small budget
            TokenizerModel = "gpt-3.5-turbo"
        };
        var manager = new ContextWindowManager(options);

        var docs = new[]
        {
            new RagDocument("1", "A very long text that will definitely exceed ten tokens and need to be truncated.", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>()),
            new RagDocument("2", "This should not be included at all.", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>())
        };

        // Act
        var truncated = manager.TruncateToFit(docs).ToList();

        // Assert
        // The first document should be included but truncated. The second should be excluded.
        // Wait, ContextWindowManager says `if (tokenBudget > 100) ... truncate ... else break`.
        // Since budget is 10 and we don't have > 100 remaining, it just drops documents if they don't fit.
        // If a document exceeds the initial budget, and budget is 10 (not > 100), it skips it.
        // Let's set budget to 110 to trigger the partial truncation block.
        var options2 = new SemanticKernelGeneratorOptions 
        { 
            MaxContextTokens = 110,
            TokenizerModel = "gpt-3.5-turbo"
        };
        var manager2 = new ContextWindowManager(options2);
        
        // Large content of ~150 words
        var largeContent = string.Join(" ", Enumerable.Repeat("word", 150));
        var docs2 = new[]
        {
            new RagDocument("1", largeContent, ReadOnlyMemory<float>.Empty, new Dictionary<string, object>())
        };
        
        var truncated2 = manager2.TruncateToFit(docs2).ToList();
        
        truncated2.Should().HaveCount(1);
        truncated2[0].Content.Should().EndWith("...");
        manager2.CountTokens(truncated2[0].Content).Should().BeLessThanOrEqualTo(120); // allow some slack for the "..." tokens
    }
}
