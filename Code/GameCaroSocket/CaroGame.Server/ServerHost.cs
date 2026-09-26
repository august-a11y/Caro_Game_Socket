using System.Net;
using CaroGame.Application;
using CaroGame.Infrastructure;
using CaroGame.Server.Background;
using CaroGame.Server.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CaroGame.Server;

public static class ServerHost
{
    public static async Task RunAsync(
        CancellationToken cancellationToken,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        var endpoint = new IPEndPoint(IPAddress.Any, 5000);
        var services = new ServiceCollection()
            .AddCaroGameApplication()
            .AddCaroGameInfrastructure()
            .AddCaroGameRouting()
            .AddCaroGameServer(endpoint, configureLogging);

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var acceptor = serviceProvider.GetRequiredService<ClientAcceptor>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CaroGame.Server");
        var discoveryBroadcaster = serviceProvider.GetRequiredService<ServerDiscoveryBroadcaster>();

        logger.LogInformation("Caro game server listening on {Endpoint}", endpoint);
        logger.LogInformation("Server log files are written to {LogDirectory}",
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "logs")));

        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var listening = acceptor.StartAsync(shutdown.Token);
        var timeouts = serviceProvider.GetRequiredService<TimeoutWorker>().RunAsync(shutdown.Token);
        var cleanup = serviceProvider.GetRequiredService<CleanupWorker>().RunAsync(shutdown.Token);
        var tasks = new List<Task> { listening, timeouts, cleanup };
        if (!string.Equals(Environment.GetEnvironmentVariable("CARO_DISCOVERY_ENABLED"), "false",
                StringComparison.OrdinalIgnoreCase))
            tasks.Add(discoveryBroadcaster.RunAsync(shutdown.Token));

        try
        {
            await Task.WhenAny(tasks);
        }
        finally
        {
            shutdown.Cancel();
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "A server background task failed");
                throw;
            }
            finally
            {
                logger.LogInformation("Caro game server stopped");
            }
        }
    }
}
