using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using RagNet.Abstractions;
using RagNet.SemanticKernel;
using RagNet.SemanticKernel.Options;
using Xunit;

namespace RagNet.SemanticKernel.Tests;

public class SemanticKernelRagGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_InvokesKernel_AndExtractsCitations()
    {
        // Arrange
        var mockChatCompletion = new Mock<IChatCompletionService>();
        
        // Mock the response from the LLM, simulating a citation to source [1]
        var responseText = "The answer is based on the provided text [1].";
        mockChatCompletion.Setup(c => c.GetChatMessageContentAsync(
                It.IsAny<ChatHistory>(), 
                It.IsAny<PromptExecutionSettings>(), 
                It.IsAny<Kernel>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessageContent(AuthorRole.Assistant, responseText));

        // Create a real Kernel but with our mocked service
        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(mockChatCompletion.Object);
        var kernel = builder.Build();

        var options = Options.Create(new SemanticKernelGeneratorOptions
        {
            SystemPromptTemplate = "System: You are an assistant.",
            UserPromptTemplate = "Context: {{$context}} Query: {{$query}}",
            MaxContextTokens = 4000,
            TokenizerModel = "gpt-3.5-turbo",
            EnableSelfRagValidation = false // Keep simple for this test
        });

        var generator = new SemanticKernelRagGenerator(kernel, options);

        var contextDocs = new[]
        {
            new RagDocument("doc-1", "This is the source context.", ReadOnlyMemory<float>.Empty, 
                new Dictionary<string, object> { ["source"] = "manual.pdf" }),
            new RagDocument("doc-2", "Irrelevant context.", ReadOnlyMemory<float>.Empty, 
                new Dictionary<string, object> { ["source"] = "other.pdf" })
        };

        // Act
        var response = await generator.GenerateAsync("What is the answer?", contextDocs);

        // Assert
        response.Answer.Should().Be(responseText);
        
        // It should have exactly 1 citation since the answer only mentions [1]
        response.Citations.Should().HaveCount(1);
        response.Citations[0].DocumentId.Should().Be("doc-1");
        
        // Ensure the chat completion service was called
        mockChatCompletion.Verify(c => c.GetChatMessageContentAsync(
            It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
