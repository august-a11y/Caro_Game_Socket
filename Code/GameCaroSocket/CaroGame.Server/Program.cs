using CaroGame.Application;
using CaroGame.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddCaroGameApplication()
    .AddCaroGameInfrastructure();

using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
});

Console.WriteLine("Caro game server dependencies initialized.");
