using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Enum;
using CaroGame.Server.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CaroGame.Server.Background;

public sealed class CleanupWorker(
    IRoomRepository rooms, IChallengeRepository challenges,
    ISessionRepository sessions, IPlayerRepository players,
    RoomLockService roomLocks, LobbyLockService lobbyLock,
    TimeProvider time, CleanupOptions options,
    ILogger<CleanupWorker>? logger = null)
{
    private readonly ILogger<CleanupWorker> _logger = logger ?? NullLogger<CleanupWorker>.Instance;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        options.Validate();
        using var timer = new PeriodicTimer(options.CheckInterval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await CleanupAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Cleanup worker stopped unexpectedly");
            throw;
        }
    }

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        await lobbyLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var now = time.GetUtcNow().UtcDateTime;
            // Keep players while any retained room or challenge references them.
            var referencedPlayers = new HashSet<Guid>();
            foreach (var candidate in rooms.GetAll())
            {
                await roomLocks.LockRoomAsync(candidate.RoomId, cancellationToken);
                try
                {
                    var room = rooms.GetById(candidate.RoomId);
                    if (room is null) continue;
                    if (room.Status is RoomStatus.Finished or RoomStatus.Cancelled &&
                        room.ClosedAt is DateTime closedAt && !options.RetainsRoom(closedAt, now))
                    {
                        rooms.Remove(room.RoomId);
                        continue;
                    }
                    referencedPlayers.Add(room.PlayerX.PlayerId);
                    referencedPlayers.Add(room.PlayerO.PlayerId);
                    referencedPlayers.UnionWith(room.Spectators);
                }
                finally { roomLocks.UnlockRoom(candidate.RoomId); }
            }

            foreach (var challenge in challenges.GetAll())
            {
                if (challenge.Status != ChallengeStatus.Pending && challenge.ClosedAt is DateTime closedAt &&
                    now - closedAt >= options.ChallengeRetention)
                {
                    challenges.Remove(challenge.ChallengeId);
                    continue;
                }
                referencedPlayers.Add(challenge.FromPlayerId);
                referencedPlayers.Add(challenge.ToPlayerId);
            }

            foreach (var session in sessions.GetAll())
            {
                if (session.IsConnected || session.DisconnectedAt is not DateTime disconnectedAt ||
                    now - disconnectedAt < options.OfflinePlayerRetention || referencedPlayers.Contains(session.PlayerId))
                    continue;
                sessions.Remove(session.PlayerId);
                players.Remove(session.PlayerId);
            }
        }
        finally { lobbyLock.Gate.Release(); }
    }
}
