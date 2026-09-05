using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Infrastructure.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CaroGame.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCaroGameInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IPlayerRepository, InMemoryPlayerRepository>();
        services.TryAddSingleton<ISessionRepository, InMemorySessionRepository>();
        services.TryAddSingleton<IRoomRepository, InMemoryRoomRepository>();
        services.TryAddSingleton<IChallengeRepository, InMemoryChallengeRepository>();

        return services;
    }
}
