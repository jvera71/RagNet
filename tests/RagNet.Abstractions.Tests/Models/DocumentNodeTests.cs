using FluentAssertions;
using RagNet.Abstractions;
using Xunit;

namespace RagNet.Abstractions.Tests.Models;

public class DocumentNodeTests
{
    [Fact]
    public void DocumentNode_CreatesHierarchyCorrectly()
    {
        // Arrange
        var childNode = new DocumentNode
        {
            NodeType = DocumentNodeType.Section,
            Content = "Child content",
            Level = 2
        };

        var parentNode = new DocumentNode
        {
            NodeType = DocumentNodeType.Document,
            Content = "Parent content",
            Level = 1,
            Children = new[] { childNode }
        };

        // Act & Assert
        parentNode.NodeType.Should().Be(DocumentNodeType.Document);
        parentNode.Content.Should().Be("Parent content");
        parentNode.Level.Should().Be(1);
        parentNode.Children.Should().HaveCount(1);
        
        var child = parentNode.Children[0];
        child.NodeType.Should().Be(DocumentNodeType.Section);
        child.Content.Should().Be("Child content");
        child.Level.Should().Be(2);
    }

    [Fact]
    public void DocumentNode_DefaultProperties_AreSetCorrectly()
    {
        // Act
        var node = new DocumentNode
        {
            NodeType = DocumentNodeType.Document,
            Content = "Test content"
        };

        // Assert
        node.Metadata.Should().NotBeNull();
        node.Metadata.Should().BeEmpty();
        node.Children.Should().NotBeNull();
        node.Children.Should().BeEmpty();
        node.Level.Should().Be(0);
    }
}
