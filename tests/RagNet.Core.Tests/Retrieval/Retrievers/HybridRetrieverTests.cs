using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RagNet.Abstractions;
using RagNet.Core.Options;
using RagNet.Core.Retrieval.Retrievers;
using Xunit;

namespace RagNet.Core.Tests.Retrieval.Retrievers;

public class HybridRetrieverTests
{
    [Fact]
    public async Task RetrieveAsync_FusesResults_WithRRF()
    {
        // Arrange
        var mockVector = new Mock<IRetriever>();
        mockVector.Setup(r => r.RetrieveAsync("test", 10, default))
            .ReturnsAsync(new[]
            {
                new RagDocument("doc-1", "contenido A", ReadOnlyMemory<float>.Empty,
                    new Dictionary<string, object> { ["_score"] = 0.95 }),
                new RagDocument("doc-2", "contenido B", ReadOnlyMemory<float>.Empty,
                    new Dictionary<string, object> { ["_score"] = 0.80 })
            });

        var mockKeyword = new Mock<IRetriever>();
        mockKeyword.Setup(r => r.RetrieveAsync("test", 10, default))
            .ReturnsAsync(new[]
            {
                new RagDocument("doc-2", "contenido B", ReadOnlyMemory<float>.Empty,
                    new Dictionary<string, object> { ["_score"] = 0.90 }),
                new RagDocument("doc-3", "contenido C", ReadOnlyMemory<float>.Empty,
                    new Dictionary<string, object> { ["_score"] = 0.70 })
            });

        var options = Microsoft.Extensions.Options.Options.Create(new HybridRetrieverOptions 
        { 
            Alpha = 0.5,
            ExpandedTopK = 10,
            RrfK = 60
        });
        
        var hybrid = new HybridRetriever(mockVector.Object, mockKeyword.Object, options);

        // Act
        var results = (await hybrid.RetrieveAsync("test", 10)).ToList();

        // Assert
        results.Should().HaveCount(3); // doc-1, doc-2 (fusionado), doc-3
        results[0].Id.Should().Be("doc-2"); // Aparece en ambos -> mayor RRF score
        results.Should().BeInDescendingOrder(d => (double)d.Metadata["_score"]);
    }
}
