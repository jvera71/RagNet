using FluentAssertions;
using RagNet.Abstractions;
using RagNet.Parsers.Office;
using System.Text;
using Xunit;

namespace RagNet.Parsers.Office.Tests;

public class WordDocumentParserTests
{
    // Real word documents require proper OpenXML structures which is hard to mock in a simple stream string.
    // However, if the parser has some basic stream reading or empty file handling, we can test it.
    [Fact]
    public async Task ParseAsync_EmptyStream_ThrowsExceptionOrReturnsEmpty()
    {
        // Arrange
        var parser = new WordDocumentParser();
        using var stream = new MemoryStream(new byte[0]);

        // Act & Assert
        // We'll just verify it throws something like FileFormatException or similar, 
        // because an empty stream is not a valid DOCX file.
        await Assert.ThrowsAnyAsync<Exception>(async () => await parser.ParseAsync(stream, "test.docx"));
    }
}
