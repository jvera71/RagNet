using FluentAssertions;
using RagNet.Abstractions;
using System.Text.Json;
using Xunit;

namespace RagNet.Abstractions.Tests.Models;

public class RagDocumentTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = "doc-1";
        var content = "This is a test content";
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var metadata = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var document = new RagDocument(id, content, vector, metadata);

        // Assert
        document.Id.Should().Be(id);
        document.Content.Should().Be(content);
        document.Vector.ToArray().Should().BeEquivalentTo(vector);
        document.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void Record_ProvidesStructuralEquality()
    {
        // Arrange
        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        
        var doc1 = new RagDocument("doc-1", "content", vector, metadata);
        var doc2 = new RagDocument("doc-1", "content", vector, metadata);

        // Act & Assert
        doc1.Should().BeEquivalentTo(doc2);
    }
}
