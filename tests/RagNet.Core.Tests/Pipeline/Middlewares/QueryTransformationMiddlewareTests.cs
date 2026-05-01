using FluentAssertions;
using Moq;
using RagNet.Abstractions;
using RagNet.Core.Pipeline.Middlewares;
using Xunit;

namespace RagNet.Core.Tests.Pipeline.Middlewares;

public class QueryTransformationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_TransformsQuery_AndPassesToNextMiddleware()
    {
        // Arrange
        var mockTransformer = new Mock<IQueryTransformer>();
        var transformedQueries = new[] { "query1", "query2" };
        mockTransformer.Setup(t => t.TransformAsync(It.IsAny<string>(), default))
            .ReturnsAsync(transformedQueries);

        var context = new RagPipelineContext { OriginalQuery = "original query" };
        var expectedResponse = new RagResponse { Answer = "done" };

        bool nextInvoked = false;
        RagPipelineDelegate next = ctx =>
        {
            nextInvoked = true;
            ctx.TransformedQueries.Should().BeEquivalentTo(transformedQueries);
            return Task.FromResult(expectedResponse);
        };

        var middleware = new QueryTransformationMiddleware(next, mockTransformer.Object);

        // Act
        var response = await middleware.InvokeAsync(context);

        // Assert
        response.Should().Be(expectedResponse);
        nextInvoked.Should().BeTrue();
        mockTransformer.Verify(t => t.TransformAsync(It.IsAny<string>(), default), Times.Once);
    }
}
