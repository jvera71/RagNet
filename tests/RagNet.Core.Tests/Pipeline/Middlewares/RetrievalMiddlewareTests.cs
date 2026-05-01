using FluentAssertions;
using Moq;
using RagNet.Abstractions;
using RagNet.Core.Pipeline.Middlewares;
using Xunit;

namespace RagNet.Core.Tests.Pipeline.Middlewares;

public class RetrievalMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RetrievesDocuments_Deduplicates_AndPassesToNext()
    {
        // Arrange
        var mockRetriever = new Mock<IRetriever>();
        
        var docsForQuery1 = new[]
        {
            new RagDocument("doc-1", "A", ReadOnlyMemory<float>.Empty, new Dictionary<string, object> { ["_score"] = 0.9 }),
            new RagDocument("doc-2", "B", ReadOnlyMemory<float>.Empty, new Dictionary<string, object> { ["_score"] = 0.8 })
        };
        
        var docsForQuery2 = new[]
        {
            new RagDocument("doc-2", "B", ReadOnlyMemory<float>.Empty, new Dictionary<string, object> { ["_score"] = 0.85 }), // duplicate ID
            new RagDocument("doc-3", "C", ReadOnlyMemory<float>.Empty, new Dictionary<string, object> { ["_score"] = 0.7 })
        };

        mockRetriever.Setup(r => r.RetrieveAsync("q1", 20, default)).ReturnsAsync(docsForQuery1);
        mockRetriever.Setup(r => r.RetrieveAsync("q2", 20, default)).ReturnsAsync(docsForQuery2);

        var context = new RagPipelineContext 
        { 
            OriginalQuery = "q",
            TransformedQueries = new[] { "q1", "q2" }
        };
        
        var expectedResponse = new RagResponse { Answer = "done" };

        bool nextInvoked = false;
        RagPipelineDelegate next = ctx =>
        {
            nextInvoked = true;
            // Should be deduplicated by ID, so 3 total documents
            ctx.RetrievedDocuments.Should().HaveCount(3);
            ctx.RetrievedDocuments.Select(d => d.Id).Should().BeEquivalentTo("doc-1", "doc-2", "doc-3");
            return Task.FromResult(expectedResponse);
        };

        var middleware = new RetrievalMiddleware(next, mockRetriever.Object, topK: 20);

        // Act
        var response = await middleware.InvokeAsync(context);

        // Assert
        response.Should().Be(expectedResponse);
        nextInvoked.Should().BeTrue();
        mockRetriever.Verify(r => r.RetrieveAsync(It.IsAny<string>(), 20, default), Times.Exactly(2));
    }

    [Fact]
    public async Task InvokeAsync_UsesOriginalQuery_WhenTransformedQueriesAreEmpty()
    {
        // Arrange
        var mockRetriever = new Mock<IRetriever>();
        mockRetriever.Setup(r => r.RetrieveAsync("original", 10, default)).ReturnsAsync(Array.Empty<RagDocument>());

        var context = new RagPipelineContext 
        { 
            OriginalQuery = "original",
            TransformedQueries = Array.Empty<string>()
        };

        RagPipelineDelegate next = ctx => Task.FromResult(new RagResponse { Answer = "done" });
        var middleware = new RetrievalMiddleware(next, mockRetriever.Object, topK: 10);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        mockRetriever.Verify(r => r.RetrieveAsync("original", 10, default), Times.Once);
    }
}
