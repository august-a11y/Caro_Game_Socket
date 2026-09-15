using CaroGame.Server.Controllers;
using CaroGame.Shared.Networking.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CaroGame.Server.Routing;

public static class DependencyInjection
{
    public static IServiceCollection AddCaroGameRouting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

      
        services.TryAddTransient<SessionController>();
        services.TryAddTransient<LobbyController>();
        services.TryAddTransient<MatchMakingController>();
        services.TryAddTransient<MatchController>();
        services.TryAddTransient<GameplayController>();

        services.TryAddTransient<MessageDispatcher>(provider =>
        {
            var session = provider.GetRequiredService<SessionController>();
            var lobby = provider.GetRequiredService<LobbyController>();
            var matchmaking = provider.GetRequiredService<MatchMakingController>();
            var match = provider.GetRequiredService<MatchController>();
            var gameplay = provider.GetRequiredService<GameplayController>();

            var routes = new Dictionary<MessageTypes, RequestDelegate>
            {
                { MessageTypes.PlayerJoinRequest, session.JoinAsync },
                { MessageTypes.PlayerReconnectRequest, session.ReconnectAsync },
                { MessageTypes.Heartbeat, session.HeartbeatAsync },
                { MessageTypes.RegisterUdpEndpointRequest, session.RegisterUdpEndpointAsync },
                { MessageTypes.OnlinePlayersListRequest, lobby.GetOnlinePlayersAsync },
                { MessageTypes.ActiveMatchesListRequest, lobby.GetActiveMatchesAsync },
                { MessageTypes.ChallengeRequest, matchmaking.SendChallengeAsync },
                { MessageTypes.ChallengeRespondRequest, matchmaking.RespondToChallengeAsync },
                { MessageTypes.ChallengeCancelRequest, matchmaking.CancelChallengeAsync },
                { MessageTypes.PlayerReadyRequest, match.ReadyAsync },
                { MessageTypes.LeaveWaitingRoomRequest, match.LeaveWaitingRoomAsync },
                { MessageTypes.JoinRoomAsSpectatorRequest, match.JoinAsSpectatorAsync },
                { MessageTypes.LeaveRoomAsSpectatorRequest, match.LeaveAsSpectatorAsync },
                { MessageTypes.MoveRequest, gameplay.MoveAsync },
                { MessageTypes.SurrenderRequest, gameplay.SurrenderAsync },
                { MessageTypes.BoardStateSnapshotRequest, gameplay.GetBoardStateAsync }
            };

            return new MessageDispatcher(routes);
        });
        return services;
    }
}
