using CaroGame.Server.Services;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Application.UseCases.Match;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Enum;
using CaroGame.Server.Controllers;
using CaroGame.Shared.Networking.Messaging;
using CaroGame.Shared.Protocol.Contracts;
using CaroGame.Infrastructure.Networking.Messaging;

namespace CaroGame.Server.Background;

public sealed class TimeoutWorker(
    IRoomRepository rooms,
    IChallengeRepository challenges,
    RoomLockService roomLocks,
    LobbyLockService lobbyLock,
    ITurnTimeoutHandler turnTimeout,
    IGracePeriodExpiryHandler graceTimeout,
    IWaitingRoomCanceller waitingRooms,
    SessionController sessions,
    ClientAcceptor connections,
    MessageService messages,
    TimeProvider time,
    TimeoutOptions options)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        options.Validate();
        using var timer = new PeriodicTimer(options.CheckInterval, time);
        try
        {
            // Await each scan so ticks never overlap within this worker.
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await CheckTimeoutsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public async Task CheckTimeoutsAsync(CancellationToken cancellationToken = default)
    {
        var deliveries = new List<Task>();
        var staleConnections = new List<ClientConnection>();
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var now = time.GetUtcNow().UtcDateTime;
            foreach (var connection in connections.Connections)
            {
                if (connection.Session is { IsConnected: true } session &&
                    now - session.LastHeartbeatAt >= options.HeartbeatTimeout)
                {
                    // The normal receive-loop cleanup performs DisconnectAsync and
                    // broadcasts MatchPaused. Do not acquire the lobby lock again here.
                    await connection.DisposeAsync();
                    staleConnections.Add(connection);
                }
            }

            foreach (var challenge in challenges.GetPending())
            {
                if (!challenge.IsExpired(now)) continue;
                challenge.Expire(now);
                challenges.Update(challenge);
                deliveries.Add(messages.BroadcastAsync(
                    [challenge.FromPlayerId, challenge.ToPlayerId],
                    MessageTypes.ChallengeExpiredNotification,
                    new ChallengeResponse(null, new ChallengeDto(challenge.ChallengeId,
                        challenge.FromPlayerId, challenge.ToPlayerId, challenge.ExpiresAt,
                        challenge.Status.ToString())), cancellationToken));
            }
        }
        finally { lobbyLock.Gate.Release(); }

        // Complete the normal disconnect transition before checking room deadlines.
        // Receive-loop cleanup can also run; DisconnectAsync is idempotent under its lock.
        foreach (var connection in staleConnections)
            await sessions.DisconnectAsync(connection);

        // Matchmaking and session lifecycle use lobby -> room; keep the same order.
        foreach (var candidate in rooms.GetOngoingRooms())
        {
            await lobbyLock.Gate.WaitAsync(cancellationToken);
            try
            {
                await roomLocks.LockRoomAsync(candidate.RoomId, cancellationToken);
                try
                {
                    var room = rooms.GetById(candidate.RoomId);
                    var now = time.GetUtcNow().UtcDateTime;
                    if (room?.HasReadyExpired(now) == true)
                    {
                        waitingRooms.Cancel(room.RoomId, "ReadyTimeout");
                        deliveries.Add(messages.BroadcastAsync(RoomMessages.Recipients(room),
                            MessageTypes.RoomCancelledNotification,
                            new RoomCancelledNotification(null, RoomMessages.Snapshot(room, now), "ReadyTimeout"),
                            cancellationToken));
                        continue;
                    }
                    if (room?.Status != RoomStatus.Playing || room.CurrentMatch is null) continue;
                    var expiredDisconnect = room.Disconnected.Values
                        .FirstOrDefault(info => now >= info.GracePeriodEndsAt);
                    var packets = new List<Packet>();
                    string reason;
                    if (expiredDisconnect is not null)
                    {
                        graceTimeout.Handle(room.RoomId, expiredDisconnect.PlayerId);
                        reason = "OpponentDisconnectTimeout";
                    }
                    else
                    {
                        var turn = room.CurrentMatch.TurnManager;
                        if (turn.IsPaused || !turn.IsTimeUp(now)) continue;
                        var playerId = turn.CurrentTurnPlayerId;
                        turnTimeout.HandleTurnTimeout(room.RoomId, playerId);
                        reason = "Timeout";
                        packets.Add(messages.CreatePacket(MessageTypes.TurnTimeoutNotification,
                            new TurnTimeoutNotification(room.RoomId, playerId)));
                    }

                    if (room.Status != RoomStatus.Finished) continue;
                    packets.Add(messages.CreatePacket(MessageTypes.GameOverNotification,
                        new GameOverNotification(null, RoomMessages.Snapshot(room, now), reason)));
                    // Queue the batch under the room lock, like the gameplay controller.
                    // Await network delivery after releasing locks.
                    deliveries.Add(messages.BroadcastAsync(RoomMessages.Recipients(room), packets, cancellationToken));
                }
                finally { roomLocks.UnlockRoom(candidate.RoomId); }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Console.Error.WriteLine($"Timeout check failed for room {candidate.RoomId}: {exception}");
            }
            finally { lobbyLock.Gate.Release(); }
        }
        await Task.WhenAll(deliveries);
    }

}
