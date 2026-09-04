using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Tests.Entities;

public sealed class MatchTests
{
    private static readonly DateTime StartTime =
        new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_CreatesActiveMatchWithPlayerXTurn()
    {
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();

        var match = new Match(playerXId, playerOId, boardSize: 10, turnDurationSec: 20, StartTime);

        Assert.Equal(playerXId, match.PlayerXId);
        Assert.Equal(playerOId, match.PlayerOId);
        Assert.Equal(10, match.Board.Size);
        Assert.Equal(playerXId, match.TurnManager.CurrentTurnPlayerId);
        Assert.Equal(StartTime.AddSeconds(20), match.TurnManager.TurnDeadline);
        Assert.Equal(MatchResultType.Continue, match.Result);
        Assert.False(match.IsFinished);
        Assert.Empty(match.MoveHistory);
    }

    [Fact]
    public void Constructor_WhenPlayersAreTheSame_Throws()
    {
        var playerId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new Match(playerId, playerId));
    }

    [Fact]
    public void Constructor_WhenPlayerIdentifierIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Match(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new Match(Guid.NewGuid(), Guid.Empty));
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(15, 0)]
    public void Constructor_WhenConfigurationIsInvalid_Throws(int boardSize, int turnDurationSec)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Match(Guid.NewGuid(), Guid.NewGuid(), boardSize, turnDurationSec));
    }

    [Fact]
    public void ApplyMove_WhenMoveIsValid_RecordsMoveAndSwitchesTurn()
    {
        var (match, playerXId, playerOId) = CreateMatch();
        var playedAt = StartTime.AddSeconds(5);
        var position = new Position(3, 8);

        var move = match.ApplyMove(playerXId, position, playedAt);

        Assert.Equal(1, move.MoveNumber);
        Assert.Equal(playerXId, move.PlayerId);
        Assert.Equal(Symbol.X, move.Symbol);
        Assert.Equal(position, move.Position);
        Assert.Equal(playedAt, move.Timestamp);
        Assert.Equal(Symbol.X, match.Board.GetSymbol(position));
        Assert.Single(match.MoveHistory);
        Assert.Equal(playerOId, match.TurnManager.CurrentTurnPlayerId);
        Assert.Equal(playedAt.AddSeconds(30), match.TurnManager.TurnDeadline);
    }

    [Fact]
    public void ApplyMove_WhenItIsNotPlayersTurn_RejectsWithoutMutation()
    {
        var (match, _, playerOId) = CreateMatch();

        Assert.Throws<InvalidOperationException>(
            () => match.ApplyMove(playerOId, new Position(0, 0), StartTime.AddSeconds(1)));

        Assert.Empty(match.MoveHistory);
        Assert.Equal(0, match.Board.PlacedCount);
    }

    [Fact]
    public void ApplyMove_WhenPlayerDoesNotBelongToMatch_RejectsWithoutMutation()
    {
        var (match, _, _) = CreateMatch();

        Assert.Throws<InvalidOperationException>(
            () => match.ApplyMove(Guid.NewGuid(), new Position(0, 0), StartTime.AddSeconds(1)));

        Assert.Empty(match.MoveHistory);
    }

    [Fact]
    public void ApplyMove_WhenPositionIsOccupied_DoesNotRecordMoveOrSwitchTurn()
    {
        var (match, playerXId, playerOId) = CreateMatch();
        var occupied = new Position(4, 4);
        match.ApplyMove(playerXId, occupied, StartTime.AddSeconds(1));
        match.ApplyMove(playerOId, new Position(5, 4), StartTime.AddSeconds(2));

        Assert.Throws<InvalidOperationException>(
            () => match.ApplyMove(playerXId, occupied, StartTime.AddSeconds(3)));

        Assert.Equal(2, match.MoveHistory.Count);
        Assert.Equal(2, match.Board.PlacedCount);
        Assert.Equal(playerXId, match.TurnManager.CurrentTurnPlayerId);
    }

    [Fact]
    public void ApplyMove_WhenDeadlineHasPassed_RejectsWithoutMutation()
    {
        var (match, playerXId, _) = CreateMatch();

        Assert.Throws<InvalidOperationException>(
            () => match.ApplyMove(playerXId, new Position(1, 1), StartTime.AddSeconds(30)));

        Assert.Empty(match.MoveHistory);
        Assert.Equal(0, match.Board.PlacedCount);
    }

    [Fact]
    public void ApplyMove_WhenTimestampIsBeforeTurnStart_RejectsWithoutMutation()
    {
        var (match, playerXId, _) = CreateMatch();

        Assert.Throws<InvalidOperationException>(
            () => match.ApplyMove(playerXId, new Position(1, 1), StartTime.AddTicks(-1)));

        Assert.Empty(match.MoveHistory);
        Assert.Equal(0, match.Board.PlacedCount);
    }

    [Fact]
    public void ApplyMove_WhenPositionIsOutsideBoard_DoesNotChangeTurnOrHistory()
    {
        var (match, playerXId, _) = CreateMatch();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => match.ApplyMove(playerXId, new Position(-1, 0), StartTime.AddSeconds(1)));

        Assert.Empty(match.MoveHistory);
        Assert.Equal(playerXId, match.TurnManager.CurrentTurnPlayerId);
    }

    [Fact]
    public void EndMatch_WithFinalResult_MarksMatchAsFinished()
    {
        var (match, _, _) = CreateMatch();

        match.EndMatch(MatchResultType.PlayerXWin);

        Assert.True(match.IsFinished);
        Assert.Equal(MatchResultType.PlayerXWin, match.Result);
    }

    [Fact]
    public void EndMatch_WithContinueResult_Throws()
    {
        var (match, _, _) = CreateMatch();

        Assert.Throws<ArgumentException>(() => match.EndMatch(MatchResultType.Continue));
        Assert.False(match.IsFinished);
    }

    [Fact]
    public void EndMatch_WithUndefinedResult_Throws()
    {
        var (match, _, _) = CreateMatch();

        Assert.Throws<ArgumentOutOfRangeException>(() => match.EndMatch((MatchResultType)999));
        Assert.False(match.IsFinished);
    }

    [Fact]
    public void EndMatch_WhenAlreadyFinished_PreservesFirstResult()
    {
        var (match, _, _) = CreateMatch();
        match.EndMatch(MatchResultType.PlayerXWin);

        Assert.Throws<InvalidOperationException>(() => match.EndMatch(MatchResultType.PlayerOWin));
        Assert.Equal(MatchResultType.PlayerXWin, match.Result);
    }

    [Fact]
    public void MoveHistory_CannotBeMutatedThroughRuntimeCast()
    {
        var (match, _, _) = CreateMatch();

        Assert.False(match.MoveHistory is ICollection<Move> { IsReadOnly: false });
    }

    [Fact]
    public void ApplyMove_WhenMatchHasFinished_Throws()
    {
        var (match, playerXId, _) = CreateMatch();
        match.EndMatch(MatchResultType.PlayerOWin);

        Assert.Throws<InvalidOperationException>(
            () => match.ApplyMove(playerXId, new Position(0, 0), StartTime.AddSeconds(1)));
    }

    private static (Match Match, Guid PlayerXId, Guid PlayerOId) CreateMatch()
    {
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        return (new Match(playerXId, playerOId, startTime: StartTime), playerXId, playerOId);
    }
}
