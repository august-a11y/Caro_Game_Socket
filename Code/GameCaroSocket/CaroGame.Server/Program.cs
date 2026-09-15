using System.Net;
using CaroGame.Application;
using CaroGame.Infrastructure;
using CaroGame.Server;
using CaroGame.Server.Routing;
using CaroGame.Server.Background;
using Microsoft.Extensions.DependencyInjection;

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
using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

Console.WriteLine($"Caro game server listening on {endpoint}.");
var listening = acceptor.StartAsync(shutdown.Token);
var timeouts = serviceProvider.GetRequiredService<TimeoutWorker>().RunAsync(shutdown.Token);
var cleanup = serviceProvider.GetRequiredService<CleanupWorker>().RunAsync(shutdown.Token);
// If any service stops/fails, stop the others and observe all tasks.
try
{
    await Task.WhenAny(listening, timeouts, cleanup);
}
finally
{
    shutdown.Cancel();
    await Task.WhenAll(listening, timeouts, cleanup);
}
