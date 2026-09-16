using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace CaroGame.Domain.Entities;

public sealed class Room
{
    private readonly HashSet<Guid> _spectators = new();
    private readonly HashSet<Guid> _readyPlayers = new();
    private readonly Dictionary<Guid, DisconnectInfo> _disconnected = new();
    private readonly HashSet<Guid> _rematchAccepted = new();
    private readonly ReadOnlyDictionary<Guid, DisconnectInfo> _disconnectedView;
    private readonly int _boardSize;
    private readonly int _turnDurationSec;
    private readonly int _readyTimeoutSeconds;

    public Guid RoomId { get; }
    public RoomStatus Status { get; private set; }
    public PlayerSlot PlayerX { get; }
    public PlayerSlot PlayerO { get; }
    public Match? CurrentMatch { get; private set; }
    public IReadOnlyCollection<Guid> Spectators => Array.AsReadOnly(_spectators.ToArray());
    public IReadOnlyCollection<Guid> ReadyPlayers => Array.AsReadOnly(_readyPlayers.ToArray());
    public IReadOnlyCollection<Guid> RematchAccepted => Array.AsReadOnly(_rematchAccepted.ToArray());
    public IReadOnlyDictionary<Guid, DisconnectInfo> Disconnected => _disconnectedView;
    public bool ArePlayersReady => _readyPlayers.Count == 2;
    public bool AreBothPlayersReadyForRematch =>
        _rematchAccepted.Contains(PlayerX.PlayerId) && _rematchAccepted.Contains(PlayerO.PlayerId);
    public DateTime CreatedAt { get; }
    public DateTime? ReadyDeadline { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? ClosingReason { get; private set; }

    public Room(
        PlayerSlot playerX,
        PlayerSlot playerO,
        int boardSize = 15,
        int turnDurationSec = 30,
        DateTime? createdAt = null,
        int readyTimeoutSeconds = 60)
    {
        ArgumentNullException.ThrowIfNull(playerX);
        ArgumentNullException.ThrowIfNull(playerO);

        if (playerX.PlayerId == Guid.Empty || playerO.PlayerId == Guid.Empty)
            throw new ArgumentException("Room players must have valid identifiers.");
        if (playerX.PlayerId == playerO.PlayerId)
            throw new ArgumentException("A room requires two different players.", nameof(playerO));
        if (playerX.Symbol != Symbol.X)
            throw new ArgumentException("Player X must use the X symbol.", nameof(playerX));
        if (playerO.Symbol != Symbol.O)
            throw new ArgumentException("Player O must use the O symbol.", nameof(playerO));
        if (boardSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be greater than zero.");
        if (turnDurationSec <= 0)
            throw new ArgumentOutOfRangeException(nameof(turnDurationSec), "Turn duration must be greater than zero.");
        if (readyTimeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(readyTimeoutSeconds));

        RoomId = Guid.NewGuid();
        _disconnectedView = new ReadOnlyDictionary<Guid, DisconnectInfo>(_disconnected);
        PlayerX = playerX;
        PlayerO = playerO;
        _boardSize = boardSize;
        _turnDurationSec = turnDurationSec;
        Status = RoomStatus.Waiting;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        _readyTimeoutSeconds = readyTimeoutSeconds;
        ReadyDeadline = CreatedAt.AddSeconds(readyTimeoutSeconds);
    }

    public bool MarkReady(Guid playerId)
    {
        if (!IsActivePlayer(playerId))
            throw new InvalidOperationException("Only an active player can become ready.");
        if (Status != RoomStatus.Waiting)
            throw new InvalidOperationException("Players can become ready only while the room is waiting.");
        if (_disconnected.ContainsKey(playerId))
            throw new InvalidOperationException("A disconnected player cannot become ready.");

        return _readyPlayers.Add(playerId);
    }

    public void StartNewMatch(DateTime startTime)
    {
        if (Status != RoomStatus.Waiting)
            throw new InvalidOperationException("Room must be waiting before a match can start.");
        if (!ArePlayersReady)
            throw new InvalidOperationException("Both players must be ready before a match can start.");
        if (HasReadyExpired(startTime))
            throw new InvalidOperationException("The ready deadline has expired.");
        if (_disconnected.Count != 0)
            throw new InvalidOperationException("Both players must be connected before starting.");

        CurrentMatch = new Match(
            PlayerX.PlayerId,
            PlayerO.PlayerId,
            _boardSize,
            _turnDurationSec,
            startTime);
        _readyPlayers.Clear();
        _rematchAccepted.Clear();
        _disconnected.Clear();
        Status = RoomStatus.Playing;
        ReadyDeadline = null;
    }

    public void PrepareRematch(DateTime? preparedAt = null)
    {
        if (Status != RoomStatus.Finished)
            throw new InvalidOperationException("Only a finished room can prepare a rematch.");

        _readyPlayers.Clear();
        _disconnected.Clear();
        Status = RoomStatus.Waiting;
        ReadyDeadline = (preparedAt ?? DateTime.UtcNow).AddSeconds(_readyTimeoutSeconds);
        ClosedAt = null;
        ClosingReason = null;
    }

    public bool RespondToRematch(Guid playerId, bool accept)
    {
        if (Status != RoomStatus.Finished)
            throw new InvalidOperationException("Rematch can only be requested after a finished match.");
        if (!IsActivePlayer(playerId))
            throw new UnauthorizedAccessException("Only room players can respond to a rematch.");

        if (accept)
            return _rematchAccepted.Add(playerId);

        return _rematchAccepted.Remove(playerId);
    }

    public bool HasReadyExpired(DateTime now) =>
        Status == RoomStatus.Waiting && ReadyDeadline is DateTime deadline && now >= deadline;

    public void CancelWaiting(DateTime? cancelledAt = null, string reason = "PlayerLeft")
    {
        if (Status != RoomStatus.Waiting)
            throw new InvalidOperationException("Only a waiting room can be cancelled.");
        Status = RoomStatus.Cancelled;
        ClosedAt = cancelledAt ?? DateTime.UtcNow;
        ClosingReason = reason;
        ReadyDeadline = null;
        _readyPlayers.Clear();
        _disconnected.Clear();
    }

    public Move ApplyMove(Guid playerId, Position position, DateTime? playedAt = null)
    {
        EnsureMatchIsPlaying();
        return CurrentMatch!.ApplyMove(playerId, position, playedAt);
    }

    public void EndMatch(MatchResultType result, DateTime? endedAt = null, string? reason = null)
    {
        EnsureMatchIsPlaying();
        CurrentMatch!.EndMatch(result);
        Status = RoomStatus.Finished;
        ClosedAt = endedAt ?? DateTime.UtcNow;
        ClosingReason = reason ?? (result == MatchResultType.Draw ? "Draw" : "FiveInRow");
    }

    public bool AddSpectator(Guid playerId)
    {
        if (playerId == Guid.Empty)
            throw new ArgumentException("Spectator must have a valid identifier.", nameof(playerId));
        if (IsActivePlayer(playerId))
            throw new InvalidOperationException("An active player cannot join as a spectator.");
        if (Status != RoomStatus.Playing)
            throw new InvalidOperationException("Spectators can join only while a match is playing.");

        return _spectators.Add(playerId);
    }

    public bool RemoveSpectator(Guid playerId) => _spectators.Remove(playerId);

    public void MarkDisconnected(Guid playerId, int gracePeriodSeconds, DateTime? disconnectedAt = null)
    {
        if (!IsActivePlayer(playerId))
            throw new InvalidOperationException("Only an active player can be marked as disconnected.");
        if (gracePeriodSeconds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(gracePeriodSeconds),
                "Grace period must be greater than zero.");

        if (_disconnected.ContainsKey(playerId))
            return;

        var timestamp = disconnectedAt ?? DateTime.UtcNow;
        _disconnected.Add(playerId, new DisconnectInfo(
            playerId,
            timestamp,
            Status == RoomStatus.Waiting ? ReadyDeadline!.Value : timestamp.AddSeconds(gracePeriodSeconds)));

        if (Status == RoomStatus.Waiting)
            _readyPlayers.Remove(playerId);
        else if (Status == RoomStatus.Playing)
            CurrentMatch!.Pause(timestamp);
    }

    public void MarkReconnected(Guid playerId, DateTime? reconnectedAt = null)
    {
        if (!IsActivePlayer(playerId))
            throw new InvalidOperationException("Only an active player can be marked as reconnected.");

        var wasDisconnected = _disconnected.Remove(playerId);
        if (wasDisconnected && _disconnected.Count == 0 && Status == RoomStatus.Playing)
            CurrentMatch!.Resume(reconnectedAt ?? DateTime.UtcNow);
    }

    public bool IsActivePlayer(Guid playerId) =>
        PlayerX.PlayerId == playerId || PlayerO.PlayerId == playerId;

    private void EnsureMatchIsPlaying()
    {
        if (Status != RoomStatus.Playing || CurrentMatch is null)
            throw new InvalidOperationException("Room does not have an active match.");
    }
}
