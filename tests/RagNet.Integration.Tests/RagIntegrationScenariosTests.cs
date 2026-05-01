using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RagNet.Abstractions;
using Xunit;

namespace RagNet.Integration.Tests;

public class RagIntegrationScenariosTests
{
    [Fact]
    public void DependencyInjection_CanResolveRagPipelineFactory()
    {
        // Integration test scenario to ensure DI setup constructs the factory
        
        // Arrange
        var services = new ServiceCollection();
        services.AddAdvancedRag(rag => 
        {
            rag.AddPipeline("default", p => { });
        });
        
        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetService<IRagPipelineFactory>();

        // Assert
        factory.Should().NotBeNull();
        
        // Depending on the implementation, fetching a pipeline by name:
        // var pipeline = factory.CreatePipeline("default");
        // pipeline.Should().NotBeNull();
    }
}
