using FluentAssertions;
using Moq;
using RagNet.Abstractions;
using RagNet.Core.Pipeline;
using RagNet.Core.Pipeline.Middlewares;
using Xunit;

namespace RagNet.Core.Tests.Pipeline.Middlewares;

public class RerankingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReranksDocuments_AndRecutsToTopK()
    {
        // Arrange
        var mockReranker = new Mock<IDocumentReranker>();
        var originalDocs = new[]
        {
            new RagDocument("doc-1", "A", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>()),
            new RagDocument("doc-2", "B", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>()),
            new RagDocument("doc-3", "C", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>())
        };

        var rerankedDocs = new[]
        {
            new RagDocument("doc-3", "C", ReadOnlyMemory<float>.Empty, new Dictionary<string, object> { ["_score"] = 0.99 }),
            new RagDocument("doc-1", "A", ReadOnlyMemory<float>.Empty, new Dictionary<string, object> { ["_score"] = 0.85 })
        };

        mockReranker.Setup(r => r.RerankAsync("query", originalDocs, 2, default))
            .ReturnsAsync(rerankedDocs);

        var context = new RagPipelineContext 
        { 
            OriginalQuery = "query",
            RetrievedDocuments = originalDocs.ToList()
        };

        bool nextInvoked = false;
        RagPipelineDelegate next = ctx =>
        {
            nextInvoked = true;
            var docs = ctx.RetrievedDocuments.ToList();
            docs.Should().HaveCount(2);
            docs[0].Id.Should().Be("doc-3");
            docs[1].Id.Should().Be("doc-1");
            return Task.FromResult(new RagResponse { Answer = "done" });
        };

        var middleware = new RerankingMiddleware(next, mockReranker.Object, topK: 2);

        // Act
        var response = await middleware.InvokeAsync(context);

        // Assert
        response.Answer.Should().Be("done");
        nextInvoked.Should().BeTrue();
        mockReranker.Verify(r => r.RerankAsync("query", originalDocs, 2, default), Times.Once);
    }
}
