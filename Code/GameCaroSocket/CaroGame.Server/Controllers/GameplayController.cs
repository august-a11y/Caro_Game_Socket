using CaroGame.Server.Services;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;
using CaroGame.Infrastructure.Networking.Messaging;

namespace CaroGame.Server.Controllers;

public class GameplayController(
    IMoveSubmitter submitter,
    IMatchEnder ender,
    RoomLockService roomLocks,
    LobbyLockService lobbyLock,
    RequestExecutor requests,
    IRoomRepository rooms,
    MessageService messages,
    TimeProvider time)
{
    public async Task MoveAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var session = connection.RequireSession();
        var request = requests.Deserialize<SubmitMoveRequest>(packet);
        if (request.RoomId == Guid.Empty)
            throw new ArgumentException("RoomId must not be empty.");

        Task broadcast;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            await roomLocks.LockRoomAsync(request.RoomId, cancellationToken);
            try
            {
                connection.RequireSession();
                var room = rooms.GetById(request.RoomId)
                    ?? throw new KeyNotFoundException("Room was not found.");
                if (!room.IsActivePlayer(session.PlayerId))
                    throw new UnauthorizedAccessException("Only room players can submit moves.");

                var result = submitter.SubmitMove(room.RoomId, session.PlayerId, new Position(request.Column, request.Row));
                if (result != MatchResultType.Continue)
                    ender.EndMatch(room.RoomId, result);

                var match = room.CurrentMatch!;
                var notification = new MoveNotification(
                    request.RequestId, room.RoomId, RoomMessages.Move(match.MoveHistory.Last()),
                    match.TurnManager.CurrentTurnPlayerId, match.TurnManager.TurnDeadline);
                var outgoing = new List<Packet>
                {
                    messages.CreatePacket(MessageTypes.MoveBroadcastNotification, notification)
                };
                if (result != MatchResultType.Continue)
                    outgoing.Add(messages.CreatePacket(MessageTypes.GameOverNotification, new GameOverNotification(
                        request.RequestId, RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime),
                        result == MatchResultType.Draw ? "Draw" : "FiveInRow")));

                // Enqueue complete frames in room order; network waits happen after releasing the room lock.
                broadcast = messages.BroadcastAsync(RoomMessages.Recipients(room), outgoing, cancellationToken);
            }
            finally
            {
                roomLocks.UnlockRoom(request.RoomId);
            }
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await broadcast;
    }

    public async Task SurrenderAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var session = connection.RequireSession();
        var request = requests.Deserialize<RoomRequest>(packet);
        Task broadcast;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            await roomLocks.LockRoomAsync(request.RoomId, cancellationToken);
            try
            {
                connection.RequireSession();
                var room = rooms.GetById(request.RoomId)
                    ?? throw new KeyNotFoundException("Room was not found.");
                if (!room.IsActivePlayer(session.PlayerId))
                    throw new UnauthorizedAccessException("Only room players can surrender.");
                if (room.Status != RoomStatus.Playing)
                    throw new InvalidOperationException("Room does not have an active match.");
                ender.EndMatch(room.RoomId, session.PlayerId == room.PlayerX.PlayerId
                    ? MatchResultType.PlayerOWin : MatchResultType.PlayerXWin, "Surrender");
                var notification = new GameOverNotification(request.RequestId,
                    RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime), "Surrender");
                broadcast = messages.BroadcastAsync(RoomMessages.Recipients(room),
                    MessageTypes.GameOverNotification, notification, cancellationToken);
            }
            finally
            {
                roomLocks.UnlockRoom(request.RoomId);
            }
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await broadcast;
    }

    public async Task GetBoardStateAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var session = connection.RequireSession();
        var request = requests.Deserialize<RoomRequest>(packet);
        Task send;
        await roomLocks.LockRoomAsync(request.RoomId, cancellationToken);
        try
        {
            connection.RequireSession();
            var room = rooms.GetById(request.RoomId)
                ?? throw new KeyNotFoundException("Room was not found.");
            if (!room.IsActivePlayer(session.PlayerId) && !room.Spectators.Contains(session.PlayerId))
                throw new UnauthorizedAccessException("Join this room before requesting its board.");
            send = messages.SendResponseAsync(connection, MessageTypes.BoardStateSnapshotResponse, 
                new RoomResponse(request.RequestId, RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime)), cancellationToken);
        }
        finally
        {
            roomLocks.UnlockRoom(request.RoomId);
        }
        await send;
    }
}
