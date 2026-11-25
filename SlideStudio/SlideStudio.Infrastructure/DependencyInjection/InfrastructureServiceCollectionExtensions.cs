using Microsoft.Extensions.DependencyInjection;
using SlideStudio.Core.Abstractions;
using SlideStudio.Infrastructure.Persistence;

namespace SlideStudio.Infrastructure.DependencyInjection;
{
    public static class InfrastructureServiceCollectionExtensions
{
    // Enregistre les services de la couche Infrastructure.
    // À appeler depuis Program.cs :
    // services.AddInfrastructure();

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<SafeFileWriter>();
        services.AddSingleton<IPresentationRepository, JsonPresentationRepository>();

        return services;
    }
}
}
