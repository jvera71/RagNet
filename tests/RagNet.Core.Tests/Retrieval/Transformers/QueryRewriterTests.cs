using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using RagNet.Core.Retrieval.Transformers;
using Xunit;

namespace RagNet.Core.Tests.Retrieval.Transformers;

public class QueryRewriterTests
{
    [Fact]
    public async Task TransformAsync_ReturnsRewrittenQuery()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var rewrittenQuery = "Configuración avanzada del particionado semántico en C#";
        
        mockChatClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, rewrittenQuery)));

        var transformer = new QueryRewriter(mockChatClient.Object);
        var originalQuery = "como configuro el chunker?";

        // Act
        var result = (await transformer.TransformAsync(originalQuery)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Should().Be(rewrittenQuery);
    }

    [Fact]
    public async Task TransformAsync_WhenLLMFails_ReturnsOriginalQuery()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        
        mockChatClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "   ")));

        var transformer = new QueryRewriter(mockChatClient.Object);
        var originalQuery = "como configuro el chunker?";

        // Act
        var result = (await transformer.TransformAsync(originalQuery)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Should().Be(originalQuery);
    }
}
