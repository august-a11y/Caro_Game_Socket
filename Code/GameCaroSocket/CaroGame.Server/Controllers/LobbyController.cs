using CaroGame.Server.Services;
using CaroGame.Application.UseCases.Lobby;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace CaroGame.Server.Controllers;

public class LobbyController(
    IOnlinePlayerFinder onlinePlayers,
    IOngoingMatchFinder ongoingMatches,
    IRoomRepository rooms,
    RoomLockService roomLocks,
    LobbyLockService lobbyLock,
    RequestExecutor requests,
    MessageService messages)
{
    public async Task GetOnlinePlayersAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<RequestMessage>(packet);
        Task reply;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            connection.RequireSession();
            var players = onlinePlayers.FindOnlinePlayers().Select(player =>
                new PlayerDto(player.UserId, player.Nickname, player.Status.ToString())).ToArray();
            reply = messages.SendResponseAsync(connection, MessageTypes.OnlinePlayersListResponse, 
                new PlayersResponse(request.RequestId, players), cancellationToken);
        }
        finally { lobbyLock.Gate.Release(); }
        await reply;
    }

    public async Task GetActiveMatchesAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<RequestMessage>(packet);
        Task reply;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            connection.RequireSession();
            var matches = new List<MatchSummaryDto>();
            foreach (var room in rooms.GetOngoingRooms())
            {
                // Spectator updates use the room lock, so copy each summary under it.
                await roomLocks.LockRoomAsync(room.RoomId, cancellationToken);
                try
                {
                    matches.AddRange(ongoingMatches.FindOngoingMatches(room.RoomId).Select(summary => new MatchSummaryDto(
                        summary.RoomId, summary.PlayerAName, summary.PlayerBName, summary.SpectatorCount,
                        summary.Status.ToString(), summary.StartedAt)));
                }
                finally { roomLocks.UnlockRoom(room.RoomId); }
            }
            reply = messages.SendResponseAsync(connection, MessageTypes.ActiveMatchesListResponse, 
                new MatchesResponse(request.RequestId, matches), cancellationToken);
        }
        finally { lobbyLock.Gate.Release(); }
        await reply;
    }
}
