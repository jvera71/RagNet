using System.Text;
using FluentAssertions;
using RagNet.Abstractions;
using RagNet.Parsers.Markdown;
using Xunit;

namespace RagNet.Parsers.Markdown.Tests;

public class MarkdownDocumentParserTests
{
    [Fact]
    public async Task MarkdownParser_PreservesHeadingHierarchy()
    {
        // Arrange
        var markdown = """
            # Título Principal
            ## Sección 1
            Párrafo de la sección 1.
            ## Sección 2
            Párrafo de la sección 2.
            ### Subsección 2.1
            Contenido de la subsección.
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
        var parser = new MarkdownDocumentParser();

        // Act
        var root = await parser.ParseAsync(stream, "test.md");

        // Assert
        root.NodeType.Should().Be(DocumentNodeType.Document);
        
        // This specific parser groups things under headers. 
        // According to the strategy document it should have 3 children: H1 + 2 sections
        root.Children.Should().HaveCount(3);
        root.Children[1].NodeType.Should().Be(DocumentNodeType.Section);
        root.Children[2].Children.Should().Contain(
            n => n.NodeType == DocumentNodeType.Section); // Subsección 2.1
    }

    [Fact]
    public async Task MarkdownParser_EmptyDocument_ReturnsRootNode()
    {
        // Arrange
        var markdown = "";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
        var parser = new MarkdownDocumentParser();

        // Act
        var root = await parser.ParseAsync(stream, "empty.md");

        // Assert
        root.NodeType.Should().Be(DocumentNodeType.Document);
        root.Children.Should().BeEmpty();
    }
}
