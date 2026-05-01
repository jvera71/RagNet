using FluentAssertions;
using Moq;
using RagNet.Abstractions;
using RagNet.Core.Retrieval.Transformers;
using Xunit;

namespace RagNet.Core.Tests.Retrieval.Transformers;

public class CompositeQueryTransformerTests
{
    [Fact]
    public async Task TransformAsync_ReturnsUnionOfAllQueries()
    {
        // Arrange
        var mockT1 = new Mock<IQueryTransformer>();
        mockT1.Setup(t => t.TransformAsync("query", default)).ReturnsAsync(new[] { "q1", "query" });

        var mockT2 = new Mock<IQueryTransformer>();
        mockT2.Setup(t => t.TransformAsync("query", default)).ReturnsAsync(new[] { "q2", "q1" });

        var composite = new CompositeQueryTransformer(new[] { mockT1.Object, mockT2.Object });

        // Act
        var result = (await composite.TransformAsync("query")).ToList();

        // Assert
        // Should contain original query, plus q1 and q2, properly deduplicated
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo("query", "q1", "q2");
    }
}
