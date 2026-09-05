using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Application.UseCases.Lobby;
using CaroGame.Application.UseCases.Match;
using CaroGame.Application.UseCases.MatchMaking;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CaroGame.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCaroGameApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IWinConditionChecker, WinConditionChecker>();

        services.AddTransient<IMatchEnder, EndMatchUseCase>();
        services.AddTransient<IMoveSubmitter, MoveSubmitter>();
        services.AddTransient<ITurnTimeoutHandler, TurnTimeoutHandler>();
        services.AddTransient<IOnlinePlayerFinder, OnlinePlayerFinder>();
        services.AddTransient<IOngoingMatchFinder, OngoingMatchFinder>();
        services.AddTransient<ISpectatorJoiner, SpectatorJoiner>();
        services.AddTransient<ISpectatorLeaver, SpectatorLeaver>();
        services.AddTransient<IPlayerReadyHandler, PlayerReadyHandler>();
        services.AddTransient<IChallengeSender, ChallengeSender>();
        services.AddTransient<IChallengeResponder, ChallengeResponder>();
        services.AddTransient<IGracePeriodExpiryHandler, GracePeriodExpiryHandler>();
        services.AddTransient<IPlayerDisconnectHandler, PlayerDisconnectHandler>();
        services.AddTransient<IPlayerJoiner, PlayerJoiner>();
        services.AddTransient<IPlayerReconnector, PlayerReconnector>();
        services.AddTransient<ISessionHeartbeatHandler, SessionHeartbeatHandler>();
        services.AddTransient<IUdpEndpointRegistrar, UdpEndpointRegistrar>();

        return services;
    }
}
