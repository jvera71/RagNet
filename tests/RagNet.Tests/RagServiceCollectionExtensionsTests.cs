using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RagNet;
using Xunit;

namespace RagNet.Tests;

public class RagServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAdvancedRag_RegistersServicesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAdvancedRag(rag =>
        {
            // Adding a simple pipeline to verify builder executes
            rag.AddPipeline("default", pipeline => { });
        });

        // Assert
        // If it reaches here without exception, configuration action was executed
        services.Should().NotBeEmpty();
        
        // Ensure that ArgumentNullException is thrown if services or configure is null
        Action actNullServices = () => RagServiceCollectionExtensions.AddAdvancedRag(null!, r => { });
        actNullServices.Should().Throw<ArgumentNullException>();

        Action actNullConfig = () => services.AddAdvancedRag(null!);
        actNullConfig.Should().Throw<ArgumentNullException>();
    }
}
