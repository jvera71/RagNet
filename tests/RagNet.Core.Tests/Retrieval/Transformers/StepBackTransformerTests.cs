using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using RagNet.Core.Retrieval.Transformers;
using Xunit;

namespace RagNet.Core.Tests.Retrieval.Transformers;

public class StepBackTransformerTests
{
    [Fact]
    public async Task TransformAsync_ReturnsOriginal_And_GeneralizedQuery()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var generalizedQuery = "Conceptos generales de RAG";
        
        mockChatClient.Setup(c => c.CompleteAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletion(new ChatMessage(ChatRole.Assistant, generalizedQuery)));

        var transformer = new StepBackTransformer(mockChatClient.Object);
        var originalQuery = "¿Cómo se configura el chunker en RAGNet?";

        // Act
        var result = (await transformer.TransformAsync(originalQuery)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be(originalQuery);
        result[1].Should().Be(generalizedQuery);
    }

    [Fact]
    public async Task TransformAsync_WhenLLMReturnsEmpty_ReturnsOnlyOriginalQuery()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        
        mockChatClient.Setup(c => c.CompleteAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletion(new ChatMessage(ChatRole.Assistant, "")));

        var transformer = new StepBackTransformer(mockChatClient.Object);
        var originalQuery = "¿Cómo se configura el chunker en RAGNet?";

        // Act
        var result = (await transformer.TransformAsync(originalQuery)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(originalQuery);
    }
}
