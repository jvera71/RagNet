using FluentAssertions;
using Microsoft.Extensions.Options;
using RagNet.Abstractions;
using RagNet.Core.Ingestion.Chunkers;
using RagNet.Core.Options;
using Xunit;

namespace RagNet.Core.Tests.Ingestion.Chunkers;

public class NLPBoundaryChunkerTests
{
    [Fact]
    public async Task ChunkAsync_SplitsOnSemanticBoundary_ReturnsMultipleChunks()
    {
        // Arrange
        var options = Options.Create(new NLPBoundaryChunkerOptions
        {
            MaxChunkSize = 30, // Small chunk size to force splitting
            MinChunkSize = 10,
            OverlapSentences = 0,
            IncludeSectionTitle = true
        });

        var chunker = new NLPBoundaryChunker(options);

        var paragraph = new DocumentNode
        {
            NodeType = DocumentNodeType.Paragraph,
            Content = "First sentence. Second sentence! Third sentence?"
        };

        var root = new DocumentNode
        {
            NodeType = DocumentNodeType.Section,
            Content = "Test Section",
            Children = new[] { paragraph }
        };

        // Act
        var results = (await chunker.ChunkAsync(root)).ToList();

        // Assert
        results.Should().HaveCountGreaterThan(1);
        results[0].Content.Should().Contain("[Test Section]");
        results[0].Content.Should().Contain("First sentence.");
    }
    
    [Fact]
    public async Task ChunkAsync_RespectsOverlap()
    {
        // Arrange
        var options = Options.Create(new NLPBoundaryChunkerOptions
        {
            MaxChunkSize = 30, // Force split
            MinChunkSize = 10,
            OverlapSentences = 1,
            IncludeSectionTitle = false
        });

        var chunker = new NLPBoundaryChunker(options);

        var paragraph = new DocumentNode
        {
            NodeType = DocumentNodeType.Paragraph,
            Content = "S1. S2. S3. S4."
        };

        var root = new DocumentNode
        {
            NodeType = DocumentNodeType.Document,
            Content = "Root",
            Children = new[] { paragraph }
        };

        // Act
        var results = (await chunker.ChunkAsync(root)).ToList();

        // Assert
        // With overlap 1, the end of chunk N should appear at start of chunk N+1.
        results.Should().HaveCountGreaterThan(1);
        
        // Ensure "S2." or similar overlaps
        bool hasOverlap = false;
        for(int i = 0; i < results.Count - 1; i++)
        {
            var current = results[i].Content;
            var next = results[i+1].Content;
            
            // Checking if any word from current chunk exists in the next chunk
            var lastWord = current.Split(' ').Last(s => !string.IsNullOrWhiteSpace(s));
            if(next.Contains(lastWord))
            {
                hasOverlap = true;
                break;
            }
        }
        
        hasOverlap.Should().BeTrue();
    }
}
