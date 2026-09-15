using System.Net;
using System.Collections.Concurrent;
using CaroGame.Server.Services;
using CaroGame.Infrastructure.Networking.Messaging;
using CaroGame.Server.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CaroGame.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddCaroGameServer(
        this IServiceCollection services,
        IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endPoint);

        services.TryAddSingleton<IPacketFramer, PacketFramer>();
        services.TryAddSingleton<IMessageSerializer, MessageSerializer>();
        services.TryAddSingleton<ConcurrentDictionary<Guid, ClientConnection>>();
        services.TryAddSingleton<MessageService>();
        services.TryAddSingleton<RequestExecutor>();
        services.TryAddSingleton<LobbyLockService>();
        services.TryAddSingleton<ClientConnectionHandler>();
        services.TryAddSingleton(endPoint);
        services.TryAddSingleton<ClientAcceptor>();
        services.TryAddSingleton<TimeoutOptions>();
        services.TryAddSingleton<TimeoutWorker>();
        services.TryAddSingleton<CleanupOptions>();
        services.TryAddSingleton<CleanupWorker>();

        return services;
    }
}
