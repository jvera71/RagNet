using FluentAssertions;
using RagNet.Abstractions;
using RagNet.Parsers.Pdf;
using System.Text;
using Xunit;

namespace RagNet.Parsers.Pdf.Tests;

public class PdfDocumentParserTests
{
    [Fact]
    public async Task ParseAsync_InvalidPdf_ThrowsException()
    {
        // Arrange
        var parser = new PdfDocumentParser();
        // Send a plain text string instead of a valid PDF magic number %PDF-
        var invalidPdf = Encoding.UTF8.GetBytes("Not a valid PDF file content");
        using var stream = new MemoryStream(invalidPdf);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await parser.ParseAsync(stream, "test.pdf"));
    }
}
