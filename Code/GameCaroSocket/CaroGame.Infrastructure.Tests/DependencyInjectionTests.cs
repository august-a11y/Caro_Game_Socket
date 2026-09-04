using CaroGame.Application;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Application.UseCases.Lobby;
using CaroGame.Application.UseCases.Match;
using CaroGame.Application.UseCases.MatchMaking;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Services;
using CaroGame.Infrastructure;
using CaroGame.Infrastructure.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace CaroGame.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddCaroGameApplication_WhenServicesIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CaroGame.Application.DependencyInjection.AddCaroGameApplication(null!));
    }

    [Fact]
    public void AddCaroGameInfrastructure_WhenServicesIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CaroGame.Infrastructure.DependencyInjection.AddCaroGameInfrastructure(null!));
    }

    [Fact]
    public void Registrations_BuildAndResolveEveryUseCase()
    {
        var services = new ServiceCollection()
            .AddCaroGameApplication()
            .AddCaroGameInfrastructure();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Type[] serviceTypes =
        [
            typeof(IMatchEnder),
            typeof(IMoveSubmitter),
            typeof(ITurnTimeoutHandler),
            typeof(IOnlinePlayerFinder),
            typeof(IOngoingMatchFinder),
            typeof(ISpectatorJoiner),
            typeof(ISpectatorLeaver),
            typeof(IPlayerReadyHandler),
            typeof(IChallengeSender),
            typeof(IChallengeResponder),
            typeof(IGracePeriodExpiryHandler),
            typeof(IPlayerDisconnectHandler),
            typeof(IPlayerJoiner),
            typeof(IPlayerReconnector),
            typeof(ISessionHeartbeatHandler),
            typeof(IUdpEndpointRegistrar),
            typeof(IWinConditionChecker),
            typeof(TimeProvider)
        ];

        foreach (var serviceType in serviceTypes)
            Assert.NotNull(provider.GetRequiredService(serviceType));
    }

    [Fact]
    public void Repositories_AreSingletonsWhileUseCasesAreTransient()
    {
        var services = new ServiceCollection()
            .AddCaroGameApplication()
            .AddCaroGameInfrastructure();
        using var provider = services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IPlayerRepository>(),
            provider.GetRequiredService<IPlayerRepository>());
        Assert.Same(
            provider.GetRequiredService<ISessionRepository>(),
            provider.GetRequiredService<ISessionRepository>());
        Assert.Same(
            provider.GetRequiredService<IRoomRepository>(),
            provider.GetRequiredService<IRoomRepository>());
        Assert.Same(
            provider.GetRequiredService<IChallengeRepository>(),
            provider.GetRequiredService<IChallengeRepository>());
        Assert.NotSame(
            provider.GetRequiredService<IMoveSubmitter>(),
            provider.GetRequiredService<IMoveSubmitter>());
    }

    [Fact]
    public void Registrations_PreserveCallerProvidedClockAndRepository()
    {
        var clock = new FixedTimeProvider();
        var players = new InMemoryPlayerRepository();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<IPlayerRepository>(players);

        services.AddCaroGameApplication();
        services.AddCaroGameInfrastructure();

        using var provider = services.BuildServiceProvider();
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
        Assert.Same(players, provider.GetRequiredService<IPlayerRepository>());
        Assert.Single(provider.GetServices<IPlayerRepository>());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
    }
}
