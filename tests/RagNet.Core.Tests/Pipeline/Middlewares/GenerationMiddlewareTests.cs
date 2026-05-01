using FluentAssertions;
using Moq;
using RagNet.Abstractions;
using RagNet.Core.Pipeline.Middlewares;
using Xunit;

namespace RagNet.Core.Tests.Pipeline.Middlewares;

public class GenerationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_GeneratesResponse_AndDoesNotCallNext()
    {
        // Arrange
        var mockGenerator = new Mock<IRagGenerator>();
        var contextDocs = new[]
        {
            new RagDocument("doc-1", "A", ReadOnlyMemory<float>.Empty, new Dictionary<string, object>())
        };

        var expectedResponse = new RagResponse 
        { 
            Answer = "Generated Answer",
            Citations = new[] { new Citation("doc-1", "source", 1.0, new Dictionary<string, object>()) }
        };

        mockGenerator.Setup(g => g.GenerateAsync("query", contextDocs, default))
            .ReturnsAsync(expectedResponse);

        var context = new RagPipelineContext 
        { 
            OriginalQuery = "query",
            RetrievedDocuments = contextDocs.ToList()
        };

        bool nextInvoked = false;
        RagPipelineDelegate next = ctx =>
        {
            nextInvoked = true;
            return Task.FromResult(new RagResponse { Answer = "next" });
        };

        var middleware = new GenerationMiddleware(next, mockGenerator.Object);

        // Act
        var response = await middleware.InvokeAsync(context);

        // Assert
        response.Should().Be(expectedResponse);
        nextInvoked.Should().BeFalse(); // Because Generation is terminal
        mockGenerator.Verify(g => g.GenerateAsync("query", contextDocs, default), Times.Once);
    }
}
