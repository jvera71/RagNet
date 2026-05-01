using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using RagNet.Core.Retrieval.Transformers;
using Xunit;

namespace RagNet.Core.Tests.Retrieval.Transformers;

public class HyDETransformerTests
{
    [Fact]
    public async Task TransformAsync_GeneratesHypotheticalDocument_AndReturnsIt()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var generatedDoc = "This is a hypothetical documentation paragraph.";
        
        mockChatClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, generatedDoc)));

        var transformer = new HyDETransformer(mockChatClient.Object);
        var originalQuery = "¿Qué es RAG?";

        // Act
        var result = (await transformer.TransformAsync(originalQuery)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Should().Be(generatedDoc);
    }

    [Fact]
    public async Task TransformAsync_WhenLLMFailsOrReturnsEmpty_ReturnsOriginalQuery()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var generatedDoc = "   "; // empty or whitespace
        
        mockChatClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, generatedDoc)));

        var transformer = new HyDETransformer(mockChatClient.Object);
        var originalQuery = "¿Qué es RAG?";

        // Act
        var result = (await transformer.TransformAsync(originalQuery)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Should().Be(originalQuery); // Fallbacks to original
    }
}
