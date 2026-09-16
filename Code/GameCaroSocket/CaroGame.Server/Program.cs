using System.Net;
using CaroGame.Application;
using CaroGame.Infrastructure;
using CaroGame.Server;
using CaroGame.Server.Routing;
using CaroGame.Server.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var endpoint = new IPEndPoint(IPAddress.Any, 5000);

var services = new ServiceCollection()
    .AddCaroGameApplication()
    .AddCaroGameInfrastructure()
    .AddCaroGameRouting()
    .AddCaroGameServer(endpoint);

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
});


var acceptor = serviceProvider.GetRequiredService<ClientAcceptor>();
var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CaroGame.Server");
using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

logger.LogInformation("Caro game server listening on {Endpoint}", endpoint);
var discoveryBroadcaster = serviceProvider.GetRequiredService<ServerDiscoveryBroadcaster>();
var discoveryTask = discoveryBroadcaster.StartAsync(shutdown.Token);

var listening = acceptor.StartAsync(shutdown.Token);
var timeouts = serviceProvider.GetRequiredService<TimeoutWorker>().RunAsync(shutdown.Token);
var cleanup = serviceProvider.GetRequiredService<CleanupWorker>().RunAsync(shutdown.Token);
// If any service stops/fails, stop the others and observe all tasks.
try
{
    await Task.WhenAny(listening, timeouts, cleanup, discoveryTask);
}
finally
{
    shutdown.Cancel();
    try
    {
        await Task.WhenAll(listening, timeouts, cleanup, discoveryTask);
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
