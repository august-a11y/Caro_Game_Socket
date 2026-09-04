using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Tests.Entities;

public sealed class RoomTests
{
    private static readonly DateTime StartTime =
        new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_CreatesWaitingRoomWithoutMatch()
    {
        var beforeCreation = DateTime.UtcNow;
        var (room, _, _) = CreateRoom();
        var afterCreation = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, room.RoomId);
        Assert.InRange(room.CreatedAt, beforeCreation, afterCreation);
        Assert.Equal(RoomStatus.Waiting, room.Status);
        Assert.Null(room.CurrentMatch);
        Assert.Empty(room.Spectators);
        Assert.Empty(room.ReadyPlayers);
        Assert.False(room.ArePlayersReady);
        Assert.Empty(room.Disconnected);
    }

    [Fact]
    public void Constructor_WhenPlayerSymbolDoesNotMatchSlot_Throws()
    {
        var playerX = new PlayerSlot(Guid.NewGuid(), Symbol.O);
        var playerO = new PlayerSlot(Guid.NewGuid(), Symbol.O);

        Assert.Throws<ArgumentException>(() => new Room(playerX, playerO));
    }

    [Fact]
    public void Constructor_WhenPlayerOSymbolDoesNotMatchSlot_Throws()
    {
        var playerX = new PlayerSlot(Guid.NewGuid(), Symbol.X);
        var playerO = new PlayerSlot(Guid.NewGuid(), Symbol.X);

        Assert.Throws<ArgumentException>(() => new Room(playerX, playerO));
    }

    [Fact]
    public void Constructor_WhenPlayerSlotIsNull_Throws()
    {
        var playerX = new PlayerSlot(Guid.NewGuid(), Symbol.X);
        var playerO = new PlayerSlot(Guid.NewGuid(), Symbol.O);

        Assert.Throws<ArgumentNullException>(() => new Room(null!, playerO));
        Assert.Throws<ArgumentNullException>(() => new Room(playerX, null!));
    }

    [Fact]
    public void Constructor_WhenPlayerIdentifierIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Room(
            new PlayerSlot(Guid.Empty, Symbol.X),
            new PlayerSlot(Guid.NewGuid(), Symbol.O)));

        Assert.Throws<ArgumentException>(() => new Room(
            new PlayerSlot(Guid.NewGuid(), Symbol.X),
            new PlayerSlot(Guid.Empty, Symbol.O)));
    }

    [Fact]
    public void Constructor_WhenPlayersHaveSameIdentifier_Throws()
    {
        var playerId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new Room(
            new PlayerSlot(playerId, Symbol.X),
            new PlayerSlot(playerId, Symbol.O)));
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(15, 0)]
    public void Constructor_WhenConfigurationIsInvalid_Throws(int boardSize, int turnDurationSec)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Room(
            new PlayerSlot(Guid.NewGuid(), Symbol.X),
            new PlayerSlot(Guid.NewGuid(), Symbol.O),
            boardSize,
            turnDurationSec));
    }

    [Fact]
    public void StartNewMatch_TransitionsRoomToPlayingAndUsesConfiguration()
    {
        var (room, playerXId, _) = CreateRoom(boardSize: 9, turnDurationSec: 12);

        StartMatch(room, StartTime);

        var match = Assert.IsType<Match>(room.CurrentMatch);
        Assert.Equal(RoomStatus.Playing, room.Status);
        Assert.Equal(9, match.Board.Size);
        Assert.Equal(playerXId, match.TurnManager.CurrentTurnPlayerId);
        Assert.Equal(StartTime.AddSeconds(12), match.TurnManager.TurnDeadline);
        Assert.Equal(StartTime, match.StartedAt);
        Assert.Empty(room.ReadyPlayers);
    }

    [Fact]
    public void MarkReady_IsIdempotentAndBothPlayersAreRequiredToStart()
    {
        var (room, playerXId, playerOId) = CreateRoom();

        Assert.True(room.MarkReady(playerXId));
        Assert.False(room.MarkReady(playerXId));
        Assert.False(room.ArePlayersReady);
        Assert.Throws<InvalidOperationException>(() => room.StartNewMatch(StartTime));

        Assert.True(room.MarkReady(playerOId));
        Assert.True(room.ArePlayersReady);
    }

    [Fact]
    public void MarkReady_WhenPlayerIsNotActiveOrRoomIsPlaying_Throws()
    {
        var (room, _, _) = CreateRoom();

        Assert.Throws<InvalidOperationException>(() => room.MarkReady(Guid.NewGuid()));

        StartMatch(room, StartTime);

        Assert.Throws<InvalidOperationException>(() => room.MarkReady(room.PlayerX.PlayerId));
    }

    [Fact]
    public void DisconnectWhileWaiting_ClearsReadyAndPlayerMustReconnectBeforeReadyingAgain()
    {
        var (room, playerXId, _) = CreateRoom();
        room.MarkReady(playerXId);

        room.MarkDisconnected(playerXId, 60, StartTime);

        Assert.DoesNotContain(playerXId, room.ReadyPlayers);
        Assert.Throws<InvalidOperationException>(() => room.MarkReady(playerXId));

        room.MarkReconnected(playerXId, StartTime.AddSeconds(1));

        Assert.True(room.MarkReady(playerXId));
    }

    [Fact]
    public void StartNewMatch_WhenRoomIsAlreadyPlaying_Throws()
    {
        var (room, _, _) = CreateRoom();
        StartMatch(room, StartTime);

        Assert.Throws<InvalidOperationException>(() => room.StartNewMatch(StartTime.AddMinutes(1)));
    }

    [Fact]
    public void PrepareRematch_WhenRoomIsNotFinished_Throws()
    {
        var (room, _, _) = CreateRoom();

        Assert.Throws<InvalidOperationException>(room.PrepareRematch);
    }

    [Fact]
    public void ApplyMove_DelegatesToCurrentMatch()
    {
        var (room, playerXId, playerOId) = CreateRoom();
        StartMatch(room, StartTime);
        var position = new Position(7, 2);

        var move = room.ApplyMove(playerXId, position, StartTime.AddSeconds(1));

        Assert.Equal(Symbol.X, move.Symbol);
        Assert.Equal(Symbol.X, room.CurrentMatch!.Board.GetSymbol(position));
        Assert.Equal(playerOId, room.CurrentMatch.TurnManager.CurrentTurnPlayerId);
    }

    [Fact]
    public void ApplyMove_WhenRoomIsWaiting_Throws()
    {
        var (room, playerXId, _) = CreateRoom();

        Assert.Throws<InvalidOperationException>(
            () => room.ApplyMove(playerXId, new Position(0, 0), StartTime));
    }

    [Fact]
    public void EndMatch_WhenRoomIsPlaying_TransitionsRoomToFinished()
    {
        var (room, _, _) = CreateRoom();
        StartMatch(room, StartTime);

        room.EndMatch(MatchResultType.Draw);

        Assert.Equal(RoomStatus.Finished, room.Status);
        Assert.Equal(MatchResultType.Draw, room.CurrentMatch!.Result);
    }

    [Fact]
    public void EndMatch_WhenNoMatchIsPlaying_Throws()
    {
        var (room, _, _) = CreateRoom();

        Assert.Throws<InvalidOperationException>(() => room.EndMatch(MatchResultType.Draw));
    }

    [Fact]
    public void EndMatch_WithContinueResult_LeavesRoomPlaying()
    {
        var (room, _, _) = CreateRoom();
        StartMatch(room, StartTime);

        Assert.Throws<ArgumentException>(() => room.EndMatch(MatchResultType.Continue));
        Assert.Equal(RoomStatus.Playing, room.Status);
        Assert.Equal(MatchResultType.Continue, room.CurrentMatch!.Result);
    }

    [Fact]
    public void StartNewMatch_AfterFinishedMatch_CreatesFreshMatchAndClearsDisconnects()
    {
        var (room, playerXId, _) = CreateRoom();
        StartMatch(room, StartTime);
        room.MarkDisconnected(playerXId, 60, StartTime.AddSeconds(5));
        var firstMatch = room.CurrentMatch;
        room.EndMatch(MatchResultType.PlayerOWin);

        room.PrepareRematch();
        StartMatch(room, StartTime.AddMinutes(1));

        Assert.Equal(RoomStatus.Playing, room.Status);
        Assert.NotSame(firstMatch, room.CurrentMatch);
        Assert.Empty(room.CurrentMatch!.MoveHistory);
        Assert.Empty(room.Disconnected);
    }

    [Fact]
    public void AddSpectator_AddsOnlyOnceAndRemoveSpectatorRemovesIt()
    {
        var (room, _, _) = CreateRoom();
        var spectatorId = Guid.NewGuid();
        StartMatch(room, StartTime);

        room.AddSpectator(spectatorId);
        room.AddSpectator(spectatorId);

        Assert.Single(room.Spectators);
        room.RemoveSpectator(spectatorId);
        Assert.Empty(room.Spectators);
    }

    [Fact]
    public void AddSpectator_WhenPlayerIsActive_Throws()
    {
        var (room, playerXId, _) = CreateRoom();
        StartMatch(room, StartTime);

        Assert.Throws<InvalidOperationException>(() => room.AddSpectator(playerXId));
    }

    [Fact]
    public void AddSpectator_WhenIdentifierIsEmpty_Throws()
    {
        var (room, _, _) = CreateRoom();
        StartMatch(room, StartTime);

        Assert.Throws<ArgumentException>(() => room.AddSpectator(Guid.Empty));
    }

    [Fact]
    public void AddSpectator_WhenRoomIsNotPlaying_Throws()
    {
        var (room, _, _) = CreateRoom();

        Assert.Throws<InvalidOperationException>(() => room.AddSpectator(Guid.NewGuid()));
    }

    [Fact]
    public void Spectators_CannotBeMutatedThroughRuntimeCast()
    {
        var (room, _, _) = CreateRoom();
        StartMatch(room, StartTime);

        Assert.False(room.Spectators is ICollection<Guid> { IsReadOnly: false });
    }

    [Fact]
    public void MarkDisconnected_ForActivePlayer_StoresGracePeriod()
    {
        var (room, playerXId, _) = CreateRoom();
        var disconnectedAt = StartTime.AddSeconds(10);

        room.MarkDisconnected(playerXId, 60, disconnectedAt);

        var info = Assert.Contains(playerXId, room.Disconnected);
        Assert.Equal(disconnectedAt, info.DisconnectedAt);
        Assert.Equal(disconnectedAt.AddSeconds(60), info.GracePeriodEndsAt);
    }

    [Fact]
    public void MarkDisconnected_ForNonPlayer_Throws()
    {
        var (room, _, _) = CreateRoom();

        Assert.Throws<InvalidOperationException>(
            () => room.MarkDisconnected(Guid.NewGuid(), 60, StartTime));
    }

    [Fact]
    public void MarkDisconnected_WhenGracePeriodIsNotPositive_Throws()
    {
        var (room, playerXId, _) = CreateRoom();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => room.MarkDisconnected(playerXId, 0, StartTime));
    }

    [Fact]
    public void MarkDisconnected_WhenCalledAgain_PreservesOriginalGracePeriod()
    {
        var (room, playerXId, _) = CreateRoom();
        room.MarkDisconnected(playerXId, 30, StartTime);

        room.MarkDisconnected(playerXId, 60, StartTime.AddSeconds(10));

        Assert.Equal(StartTime.AddSeconds(30), room.Disconnected[playerXId].GracePeriodEndsAt);
    }

    [Fact]
    public void MarkReconnected_RemovesDisconnectInformation()
    {
        var (room, playerXId, _) = CreateRoom();
        room.MarkDisconnected(playerXId, 60, StartTime);

        room.MarkReconnected(playerXId);

        Assert.DoesNotContain(playerXId, room.Disconnected.Keys);
    }

    [Fact]
    public void DisconnectAndReconnect_PausesAndResumesTurnWithRemainingTime()
    {
        var (room, playerXId, _) = CreateRoom(turnDurationSec: 30);
        StartMatch(room, StartTime);

        room.MarkDisconnected(playerXId, 60, StartTime.AddSeconds(10));

        Assert.True(room.CurrentMatch!.TurnManager.IsPaused);
        Assert.False(room.CurrentMatch.TurnManager.IsTimeUp(StartTime.AddHours(1)));

        room.MarkReconnected(playerXId, StartTime.AddSeconds(25));

        Assert.False(room.CurrentMatch.TurnManager.IsPaused);
        Assert.Equal(StartTime.AddSeconds(45), room.CurrentMatch.TurnManager.TurnDeadline);
    }

    [Fact]
    public void Reconnect_WhenOtherPlayerRemainsDisconnected_KeepsTurnPaused()
    {
        var (room, playerXId, playerOId) = CreateRoom();
        StartMatch(room, StartTime);
        room.MarkDisconnected(playerXId, 60, StartTime.AddSeconds(5));
        room.MarkDisconnected(playerOId, 60, StartTime.AddSeconds(6));

        room.MarkReconnected(playerXId, StartTime.AddSeconds(20));

        Assert.True(room.CurrentMatch!.TurnManager.IsPaused);
        Assert.Contains(playerOId, room.Disconnected.Keys);
    }

    [Fact]
    public void MarkReconnected_ForNonPlayer_Throws()
    {
        var (room, _, _) = CreateRoom();

        Assert.Throws<InvalidOperationException>(() => room.MarkReconnected(Guid.NewGuid(), StartTime));
    }

    [Fact]
    public void DisconnectedCollection_CannotBeMutatedThroughRuntimeCast()
    {
        var (room, playerXId, _) = CreateRoom();
        room.MarkDisconnected(playerXId, 60, StartTime);

        Assert.False(room.Disconnected is IDictionary<Guid, DisconnectInfo> { IsReadOnly: false });
    }

    [Fact]
    public void IsActivePlayer_ReturnsTrueOnlyForPlayersInRoom()
    {
        var (room, playerXId, playerOId) = CreateRoom();

        Assert.True(room.IsActivePlayer(playerXId));
        Assert.True(room.IsActivePlayer(playerOId));
        Assert.False(room.IsActivePlayer(Guid.NewGuid()));
    }

    private static (Room Room, Guid PlayerXId, Guid PlayerOId) CreateRoom(
        int boardSize = 15,
        int turnDurationSec = 30)
    {
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var room = new Room(
            new PlayerSlot(playerXId, Symbol.X),
            new PlayerSlot(playerOId, Symbol.O),
            boardSize,
            turnDurationSec);

        return (room, playerXId, playerOId);
    }

    private static void StartMatch(Room room, DateTime startTime)
    {
        room.MarkReady(room.PlayerX.PlayerId);
        room.MarkReady(room.PlayerO.PlayerId);
        room.StartNewMatch(startTime);
    }
}
