using FluentAssertions;
using RagNet.Abstractions;
using Xunit;

namespace RagNet.Abstractions.Tests.Models;

public class StreamingRagResponseTests
{
    [Fact]
    public void StreamingRagResponse_IncompleteFragment_SetsPropertiesCorrectly()
    {
        // Act
        var response = new StreamingRagResponse
        {
            ContentFragment = "partial",
            IsComplete = false
        };

        // Assert
        response.ContentFragment.Should().Be("partial");
        response.IsComplete.Should().BeFalse();
        response.Citations.Should().BeNull();
    }

    [Fact]
    public void StreamingRagResponse_CompleteFragment_WithCitations()
    {
        // Arrange
        var citations = new[] { new Citation("doc-1", "src", 0.9, new Dictionary<string, object>()) };

        // Act
        var response = new StreamingRagResponse
        {
            ContentFragment = ".",
            IsComplete = true,
            Citations = citations
        };

        // Assert
        response.ContentFragment.Should().Be(".");
        response.IsComplete.Should().BeTrue();
        response.Citations.Should().NotBeNull();
        response.Citations.Should().HaveCount(1);
    }
}
