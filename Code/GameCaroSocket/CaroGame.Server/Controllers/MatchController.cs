using CaroGame.Server.Services;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.Match;
using CaroGame.Domain.Enum;
using CaroGame.Infrastructure.Networking.Messaging;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace CaroGame.Server.Controllers;

public class MatchController(
    IPlayerReadyHandler ready,
    ISpectatorJoiner joiner,
    ISpectatorLeaver leaver,
    IRoomRepository rooms,
    RoomLockService roomLocks,
    LobbyLockService lobbyLock,
    IWaitingRoomCanceller waitingRooms,
    RequestExecutor requests,
    MessageService messages,
    TimeProvider time)
{
    public async Task ReadyAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
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
                var existing = rooms.GetById(request.RoomId) ?? throw new KeyNotFoundException("Room was not found.");
                if (!existing.IsActivePlayer(session.PlayerId))
                    throw new UnauthorizedAccessException("Only room players can ready.");
                if (existing.HasReadyExpired(time.GetUtcNow().UtcDateTime))
                    broadcast = CancelWaiting(request, "ReadyTimeout", cancellationToken);
                else
                {
                    var room = ready.Handle(request.RoomId, session.PlayerId);
                    var outgoing = new List<Packet>
                    {
                        messages.CreatePacket(MessageTypes.PlayerReadyNotification, new RoomPlayerNotification(request.RequestId, room.RoomId,
                            session.PlayerId, room.Spectators.Count))
                    };
                    if (room.Status == RoomStatus.Playing)
                        outgoing.Add(messages.CreatePacket(MessageTypes.MatchStartedNotification, new RoomResponse(request.RequestId,
                            RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime))));
                    broadcast = messages.BroadcastAsync(RoomMessages.Recipients(room), outgoing, cancellationToken);
                }
            }
            finally { roomLocks.UnlockRoom(request.RoomId); }
        }
        finally { lobbyLock.Gate.Release(); }
        await broadcast;
    }

    public async Task LeaveWaitingRoomAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<RoomRequest>(packet);
        Task broadcast;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            await roomLocks.LockRoomAsync(request.RoomId, cancellationToken);
            try
            {
                var session = connection.RequireSession();
                var room = rooms.GetById(request.RoomId) ?? throw new KeyNotFoundException("Room was not found.");
                if (!room.IsActivePlayer(session.PlayerId))
                    throw new UnauthorizedAccessException("Only room players can cancel a waiting room.");
                if (room.Status != RoomStatus.Waiting)
                    throw new InvalidOperationException("Room is no longer waiting.");
                broadcast = CancelWaiting(request,
                    room.HasReadyExpired(time.GetUtcNow().UtcDateTime) ? "ReadyTimeout" : "PlayerLeft", cancellationToken);
            }
            finally { roomLocks.UnlockRoom(request.RoomId); }
        }
        finally { lobbyLock.Gate.Release(); }
        await broadcast;
    }

    private Task CancelWaiting(RoomRequest request, string reason, CancellationToken cancellationToken)
    {
        var room = waitingRooms.Cancel(request.RoomId, reason);
        return messages.BroadcastAsync(RoomMessages.Recipients(room),
            MessageTypes.RoomCancelledNotification, new RoomCancelledNotification(request.RequestId,
                RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime), reason),
            cancellationToken);
    }

    public async Task JoinAsSpectatorAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var session = connection.RequireSession();
        var request = requests.Deserialize<RoomRequest>(packet);
        Task delivery;
        await roomLocks.LockRoomAsync(request.RoomId, cancellationToken);
        try
        {
            connection.RequireSession();
            var room = joiner.JoinSpectator(request.RoomId, session.PlayerId);
            var reply = messages.SendResponseAsync(connection, MessageTypes.JoinRoomAsSpectatorResponse, new RoomResponse(
                request.RequestId, RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime)), cancellationToken);
            var broadcast = messages.BroadcastAsync(RoomMessages.Recipients(room),
                MessageTypes.SpectatorJoinedNotification, new RoomPlayerNotification(request.RequestId, room.RoomId,
                    session.PlayerId, room.Spectators.Count),
                cancellationToken);
            delivery = Task.WhenAll(reply, broadcast);
        }
        finally
        {
            roomLocks.UnlockRoom(request.RoomId);
        }
        await delivery;
    }

    public async Task LeaveAsSpectatorAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var session = connection.RequireSession();
        var request = requests.Deserialize<RoomRequest>(packet);
        Task broadcast;
        await roomLocks.LockRoomAsync(request.RoomId, cancellationToken);
        try
        {
            connection.RequireSession();
            var existing = rooms.GetById(request.RoomId)
                ?? throw new KeyNotFoundException("Room was not found.");
            if (!existing.Spectators.Contains(session.PlayerId))
                throw new UnauthorizedAccessException("Only a joined spectator can leave this room.");
            var room = leaver.LeaveSpectator(request.RoomId, session.PlayerId);
            broadcast = messages.BroadcastAsync(RoomMessages.Recipients(room).Append(session.PlayerId),
                MessageTypes.SpectatorLeftNotification, new RoomPlayerNotification(request.RequestId, room.RoomId,
                    session.PlayerId, room.Spectators.Count),
                cancellationToken);
        }
        finally
        {
            roomLocks.UnlockRoom(request.RoomId);
        }
        await broadcast;
    }
}
