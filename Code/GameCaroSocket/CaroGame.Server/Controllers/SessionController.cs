using CaroGame.Server.Services;
using System.Net;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Application.UseCases.Match;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Server.Background;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;

namespace CaroGame.Server.Controllers;

public class SessionController(
    IPlayerJoiner joiner,
    IPlayerReconnector reconnector,
    ISessionHeartbeatHandler heartbeat,
    IUdpEndpointRegistrar registrar,
    IPlayerDisconnectHandler disconnect,
    IWaitingRoomCanceller waitingRooms,
    IPlayerRepository players,
    ISessionRepository sessions,
    IRoomRepository rooms,
    RoomLockService roomLocks,
    LobbyLockService lobbyLock,
    CleanupOptions cleanup,
    RequestExecutor requests,
    MessageService messages,
    TimeProvider time)
{
    public async Task JoinAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<PlayerJoinRequest>(packet);
        Task reply;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            if (connection.Session is not null)
                throw new InvalidOperationException("This connection already has a session.");
            var session = joiner.Join(request.Nickname);
            connection.AttachSession(session);
            reply = messages.SendResponseAsync(connection, MessageTypes.PlayerJoinResponse,
                SessionResponse(session, request.RequestId, null), cancellationToken);
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await reply;
    }

    public async Task ReconnectAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<PlayerReconnectRequest>(packet);
        Task delivery;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            if (connection.Session is not null)
                throw new InvalidOperationException("This connection already has a session.");
            var existing = sessions.GetById(request.SessionId)
                ?? throw new UnauthorizedAccessException("Invalid player or session.");
            if (existing.PlayerId != request.PlayerId)
                throw new UnauthorizedAccessException("Invalid player or session.");
            var now = time.GetUtcNow().UtcDateTime;
            var room = rooms.GetOngoingRooms().FirstOrDefault(r => r.IsActivePlayer(request.PlayerId) && (r.Status is RoomStatus.Waiting or RoomStatus.Playing))
                ?? rooms.GetAll()
                    .Where(r => r.IsActivePlayer(request.PlayerId)
                        && (r.Status is RoomStatus.Finished or RoomStatus.Cancelled)
                        && r.ClosedAt is DateTime closedAt && cleanup.RetainsRoom(closedAt, now))
                    .OrderByDescending(r => r.ClosedAt)
                    .ThenByDescending(r => r.CreatedAt)
                    .FirstOrDefault();
                
            if (room is not null)
                await roomLocks.LockRoomAsync(room.RoomId, cancellationToken);
            try
            {
                var waitingExpired = room != null ? room.HasReadyExpired(now) : false;
                if (waitingExpired) waitingRooms.Cancel(room!.RoomId, "ReadyTimeout");
                var wasPaused = room != null && room.CurrentMatch != null ? room.CurrentMatch.TurnManager.IsPaused : false;
                var session = reconnector.ReconnectPlayer(request.PlayerId, request.SessionId);
                connection.AttachSession(session, replaceExisting: true);
                var ongoing = room?.Status is RoomStatus.Waiting or RoomStatus.Playing;
                var reply = messages.SendResponseAsync(connection, MessageTypes.PlayerReconnectResponse,
                    SessionResponse(session, request.RequestId, ongoing ? room : null, ongoing ? null : room), cancellationToken);
                var resumed = wasPaused && room?.Status == RoomStatus.Playing &&
                    room.CurrentMatch?.TurnManager.IsPaused == false
                    ? messages.BroadcastAsync(RoomMessages.Recipients(room),
                        MessageTypes.MatchResumedNotification,
                        new RoomResponse(request.RequestId, RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime)),
                        cancellationToken)
                    : Task.CompletedTask;
                var waitingUpdate = waitingExpired
                    ? messages.BroadcastAsync(RoomMessages.Recipients(room!),
                        MessageTypes.RoomCancelledNotification,
                        new RoomCancelledNotification(request.RequestId,
                            RoomMessages.Snapshot(room!, time.GetUtcNow().UtcDateTime), "ReadyTimeout"), cancellationToken)
                    : room?.Status == RoomStatus.Waiting
                        ? messages.BroadcastAsync(RoomMessages.Recipients(room),
                            MessageTypes.WaitingRoomUpdatedNotification,
                            new RoomResponse(request.RequestId, RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime)), cancellationToken)
                        : Task.CompletedTask;
                delivery = Task.WhenAll(reply, resumed, waitingUpdate);
            }
            finally
            {
                if (room is not null)
                    roomLocks.UnlockRoom(room.RoomId);
            }
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await delivery;
    }

    public async Task HeartbeatAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<RequestMessage>(packet);
        Task reply;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var session = connection.RequireSession();
            heartbeat.Handle(session.PlayerId);
            reply = messages.SendResponseAsync(connection, MessageTypes.HeartbeatResponse,
                new HeartbeatResponse(request.RequestId, time.GetUtcNow().UtcDateTime), cancellationToken);
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await reply;
    }

    public async Task RegisterUdpEndpointAsync(ClientConnection connection, Packet packet, CancellationToken cancellationToken = default)
    {
        var request = requests.Deserialize<RegisterUdpEndpointRequest>(packet);
        Task reply;
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var session = connection.RequireSession();
            var address = ((IPEndPoint)connection.RemoteEndPoint).Address.ToString();
            registrar.Register(session.PlayerId, address, request.Port);
            reply = messages.SendResponseAsync(connection, MessageTypes.RegisterUdpEndpointResponse,
                new UdpEndpointResponse(request.RequestId, address, request.Port), cancellationToken);
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await reply;
    }

    public async Task DisconnectAsync(ClientConnection connection)
    {
        var deliveries = new List<Task>();
        await lobbyLock.Gate.WaitAsync();
        try
        {
            // A transferred session has already been detached from the old connection.
            if (connection.Session is not { IsConnected: true } session)
                return;

            var allRooms = rooms.GetAll();
            var activeRoom = allRooms.FirstOrDefault(room =>
                room.Status is RoomStatus.Waiting or RoomStatus.Playing && room.IsActivePlayer(session.PlayerId));
            if (activeRoom is not null)
                await roomLocks.LockRoomAsync(activeRoom.RoomId);
            try
            {
                disconnect.Handle(session.PlayerId);
                if (activeRoom?.Status == RoomStatus.Waiting)
                {
                    var now = time.GetUtcNow().UtcDateTime;
                    var expired = activeRoom.HasReadyExpired(now);
                    if (expired) waitingRooms.Cancel(activeRoom.RoomId, "ReadyTimeout");
                    var notification = expired
                        ? messages.CreatePacket(MessageTypes.RoomCancelledNotification,
                            new RoomCancelledNotification(null, RoomMessages.Snapshot(activeRoom, now), "ReadyTimeout"))
                        : messages.CreatePacket(MessageTypes.WaitingRoomUpdatedNotification,
                            new RoomResponse(null, RoomMessages.Snapshot(activeRoom, now)));
                    deliveries.Add(messages.BroadcastAsync(RoomMessages.Recipients(activeRoom), notification));
                }
                else if (activeRoom?.Status == RoomStatus.Playing)
                {
                    var info = activeRoom.Disconnected[session.PlayerId];
                    deliveries.Add(messages.BroadcastAsync(RoomMessages.Recipients(activeRoom),
                        MessageTypes.MatchPausedNotification, new MatchPausedNotification(
                            activeRoom.RoomId, session.PlayerId, info.GracePeriodEndsAt,
                            RoomMessages.Snapshot(activeRoom, time.GetUtcNow().UtcDateTime))));
                }
            }
            finally
            {
                if (activeRoom is not null)
                    roomLocks.UnlockRoom(activeRoom.RoomId);
            }

            foreach (var room in allRooms.Where(room => !room.IsActivePlayer(session.PlayerId)))
            {
                await roomLocks.LockRoomAsync(room.RoomId);
                try
                {
                    if (!room.RemoveSpectator(session.PlayerId))
                        continue;
                    rooms.Update(room);
                    deliveries.Add(messages.BroadcastAsync(RoomMessages.Recipients(room),
                        MessageTypes.SpectatorLeftNotification,
                        new RoomPlayerNotification(null, room.RoomId, session.PlayerId, room.Spectators.Count)));
                }
                finally
                {
                    roomLocks.UnlockRoom(room.RoomId);
                }
            }
        }
        finally
        {
            lobbyLock.Gate.Release();
        }
        await Task.WhenAll(deliveries);
    }

    private SessionResponse SessionResponse(Session session, Guid? requestId, Room? room, Room? lastRoom = null)
    {
        var player = players.GetById(session.PlayerId)
            ?? throw new KeyNotFoundException("Player was not found.");
        return new SessionResponse(requestId, session.PlayerId, session.SessionId,
            new PlayerDto(player.PlayerId, player.Nickname, player.Status.ToString()),
            room is null ? null : RoomMessages.Snapshot(room, time.GetUtcNow().UtcDateTime),
            lastRoom is null ? null : RoomMessages.Snapshot(lastRoom, time.GetUtcNow().UtcDateTime));
    }

}
