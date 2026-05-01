using FluentAssertions;
using RagNet.Abstractions;
using Xunit;

namespace RagNet.Abstractions.Tests.Models;

public class RagResponseTests
{
    [Fact]
    public void RagResponse_Initialization_SetsPropertiesCorrectly()
    {
        // Arrange
        var citation = new Citation("doc-1", "source", 0.9, new Dictionary<string, object>());
        var citations = new[] { citation };
        var metadata = new Dictionary<string, object> { { "duration_ms", 150 } };

        // Act
        var response = new RagResponse
        {
            Answer = "Generated Answer",
            Citations = citations,
            ExecutionMetadata = metadata
        };

        // Assert
        response.Answer.Should().Be("Generated Answer");
        response.Citations.Should().HaveCount(1);
        response.Citations[0].DocumentId.Should().Be("doc-1");
        response.ExecutionMetadata.Should().ContainKey("duration_ms").WhoseValue.Should().Be(150);
    }

    [Fact]
    public void RagResponse_DefaultProperties_AreEmptyLists()
    {
        // Act
        var response = new RagResponse
        {
            Answer = "Test Answer"
        };

        // Assert
        response.Citations.Should().NotBeNull();
        response.Citations.Should().BeEmpty();
        response.ExecutionMetadata.Should().NotBeNull();
        response.ExecutionMetadata.Should().BeEmpty();
    }
}
