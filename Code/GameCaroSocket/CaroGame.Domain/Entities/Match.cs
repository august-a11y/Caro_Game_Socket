using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Entities;

public sealed class Match
{
    private readonly List<Move> _moveHistory = new();

    public Board Board { get; }
    public TurnManager TurnManager { get; }
    public Guid PlayerXId { get; }
    public Guid PlayerOId { get; }
    public DateTime StartedAt { get; }
    public MatchResultType Result { get; private set; } = MatchResultType.Continue;
    public bool IsFinished => Result != MatchResultType.Continue;
    public IReadOnlyCollection<Move> MoveHistory => _moveHistory.AsReadOnly();

    public Match(
        Guid playerXId,
        Guid playerOId,
        int boardSize = 15,
        int turnDurationSec = 30,
        DateTime? startTime = null)
    {
        if (playerXId == Guid.Empty)
            throw new ArgumentException("Player X must not be empty.", nameof(playerXId));
        if (playerOId == Guid.Empty)
            throw new ArgumentException("Player O must not be empty.", nameof(playerOId));
        if (playerXId == playerOId)
            throw new ArgumentException("A match requires two different players.", nameof(playerOId));

        PlayerXId = playerXId;
        PlayerOId = playerOId;
        StartedAt = startTime ?? DateTime.UtcNow;
        Board = new Board(boardSize);
        TurnManager = new TurnManager(playerXId, turnDurationSec, StartedAt);
    }

    public Move ApplyMove(Guid playerId, Position position, DateTime? playedAt = null)
    {
        if (IsFinished)
            throw new InvalidOperationException("The match has already finished.");
        if (playerId != PlayerXId && playerId != PlayerOId)
            throw new InvalidOperationException("User is not a player in this match.");
        if (TurnManager.CurrentTurnPlayerId != playerId)
            throw new InvalidOperationException("It is not the player's turn.");
        if (TurnManager.IsPaused)
            throw new InvalidOperationException("The match is paused while a player is disconnected.");

        var timestamp = playedAt ?? DateTime.UtcNow;
        if (timestamp < TurnManager.TurnStartedAt)
            throw new InvalidOperationException("Move timestamp cannot be before the current turn started.");
        if (TurnManager.IsTimeUp(timestamp))
            throw new InvalidOperationException("The turn deadline has passed.");

        var symbol = GetPlayerSymbol(playerId);
        Board.PlaceSymbol(position, symbol);

        var move = new Move(_moveHistory.Count + 1, playerId, position, symbol, timestamp);
        _moveHistory.Add(move);

        var nextPlayerId = playerId == PlayerXId ? PlayerOId : PlayerXId;
        TurnManager.SwitchTurn(nextPlayerId, timestamp);
        return move;
    }

    public void EndMatch(MatchResultType result)
    {
        if (!System.Enum.IsDefined(result))
            throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown match result.");
        if (result == MatchResultType.Continue)
            throw new ArgumentException("A finished match must have a final result.", nameof(result));
        if (IsFinished)
            throw new InvalidOperationException("The match has already finished.");

        Result = result;
    }

    public void Pause(DateTime pausedAt)
    {
        if (IsFinished)
            return;

        TurnManager.Pause(pausedAt);
    }

    public void Resume(DateTime resumedAt)
    {
        if (IsFinished)
            return;

        TurnManager.Resume(resumedAt);
    }

    private Symbol GetPlayerSymbol(Guid playerId) =>
        playerId == PlayerXId ? Symbol.X : Symbol.O;
}
