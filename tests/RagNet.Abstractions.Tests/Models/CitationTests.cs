using FluentAssertions;
using RagNet.Abstractions;
using Xunit;

namespace RagNet.Abstractions.Tests.Models;

public class CitationTests
{
    [Fact]
    public void Citation_PropertiesAreSetCorrectly()
    {
        // Arrange
        var id = "doc-1";
        var sourceContent = "This is a citation content.";
        var score = 0.95;
        var metadata = new Dictionary<string, object> { { "page", 5 } };

        // Act
        var citation = new Citation(id, sourceContent, score, metadata);

        // Assert
        citation.DocumentId.Should().Be(id);
        citation.SourceContent.Should().Be(sourceContent);
        citation.RelevanceScore.Should().Be(score);
        citation.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void Citation_ProvidesStructuralEquality()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "page", 5 } };
        
        var citation1 = new Citation("doc-1", "source", 0.9, metadata);
        var citation2 = new Citation("doc-1", "source", 0.9, metadata);

        // Act & Assert
        citation1.Should().BeEquivalentTo(citation2);
    }
}
