using System.Net;
using System.Collections.Concurrent;
using CaroGame.Server.Services;
using CaroGame.Server.Background;
using CaroGame.Server.Networking;
using CaroGame.Server.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using CaroGame.Shared.Networking.Messaging;

namespace CaroGame.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddCaroGameServer(
        this IServiceCollection services,
        IPEndPoint endPoint,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endPoint);

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";
                options.SingleLine = true;
                options.IncludeScopes = true;
            });
            logging.AddProvider(new DailyFileLoggerProvider(
                Path.Combine(Environment.CurrentDirectory, "logs")));
            logging.SetMinimumLevel(LogLevel.Information);
            configureLogging?.Invoke(logging);
        });

        services.TryAddSingleton<IPacketFramer, PacketFramer>();
        services.TryAddSingleton<IMessageSerializer, MessageSerializer>();
        services.TryAddSingleton<ConcurrentDictionary<Guid, ClientConnection>>();
        services.TryAddSingleton<NetworkMessageLogger>();
        services.TryAddSingleton<MessageService>();
        services.TryAddSingleton<RequestExecutor>();
        services.TryAddSingleton<RoomLockService>();
        services.TryAddSingleton<LobbyLockService>();
        services.TryAddSingleton<ServerDiscoveryBroadcaster>();
        services.TryAddSingleton<ServerDiscoveryBroadcastOptions>();
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
