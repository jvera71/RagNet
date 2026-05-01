using Microsoft.Extensions.DependencyInjection;

namespace RagNet;

/// <summary>
/// Extension methods for setting up RagNet services in an <see cref="IServiceCollection" />.
/// </summary>
public static class RagServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RagNet services in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The action to configure the RAG system.</param>
    /// <returns>The original service collection for chaining.</returns>
    public static IServiceCollection AddAdvancedRag(
        this IServiceCollection services,
        Action<RagBuilder> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new RagBuilder(services);
        configure(builder);
        
        // Validate and finalize the service registrations
        builder.Build(); 
        
        return services;
    }
}
