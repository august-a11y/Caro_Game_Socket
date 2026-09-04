using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.Services;
using CaroGame.Domain.ValueObjects;
using Moq;

namespace CaroGame.Application.Tests.UseCases.GamePlay;

public sealed class MoveSubmitterTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 5, 0, 5, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullDependency_Throws()
    {
        var rooms = new Mock<IRoomRepository>();
        var checker = new Mock<IWinConditionChecker>();
        var ender = new Mock<IMatchEnder>();
        var clock = new Mock<TimeProvider>();

        Assert.Throws<ArgumentNullException>(() =>
            new MoveSubmitter(null!, checker.Object, ender.Object, clock.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new MoveSubmitter(rooms.Object, null!, ender.Object, clock.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new MoveSubmitter(rooms.Object, checker.Object, null!, clock.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new MoveSubmitter(rooms.Object, checker.Object, ender.Object, null!));
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenCancellationWasRequested_DoesNotLoadRoom()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var checker = new Mock<IWinConditionChecker>(MockBehavior.Strict);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var clock = new Mock<TimeProvider>(MockBehavior.Strict);
        var sut = new MoveSubmitter(rooms.Object, checker.Object, ender.Object, clock.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.SubmitMoveAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Position(0, 0),
                cancellation.Token));
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenRoomIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var (sut, _, _, _, _) = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitMoveAsync(
                Guid.Empty,
                Guid.NewGuid(),
                new Position(0, 0),
                CancellationToken.None));

        Assert.Equal("roomId", exception.ParamName);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenPlayerIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var (sut, _, _, _, _) = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SubmitMoveAsync(
                Guid.NewGuid(),
                Guid.Empty,
                new Position(0, 0),
                CancellationToken.None));

        Assert.Equal("playerId", exception.ParamName);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenRoomDoesNotExist_ThrowsKeyNotFound()
    {
        var (sut, rooms, _, _, _) = CreateSut();
        var roomId = Guid.NewGuid();
        rooms.Setup(repository => repository.GetByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.SubmitMoveAsync(
                roomId,
                Guid.NewGuid(),
                new Position(0, 0),
                CancellationToken.None));
    }

    [Fact]
    public async Task SubmitMoveAsync_ForOngoingMove_AppliesChecksAndPersistsSameRoom()
    {
        var (room, playerXId, playerOId) = CreatePlayingRoom();
        var (sut, rooms, checker, ender, clock) = CreateSut();
        Move? checkedMove = null;
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);
        checker.Setup(service => service.Check(room.CurrentMatch!.Board, It.IsAny<Move>()))
            .Callback<Board, Move>((_, move) => checkedMove = move)
            .Returns(MatchResultType.Continue);

        var returned = await sut.SubmitMoveAsync(
            room.RoomId,
            playerXId,
            new Position(4, 7),
            CancellationToken.None);

        Assert.Same(room, returned);
        Assert.NotNull(checkedMove);
        Assert.Equal(1, checkedMove.MoveNumber);
        Assert.Equal(playerXId, checkedMove.PlayerId);
        Assert.Equal(Symbol.X, checkedMove.Symbol);
        Assert.Equal(new Position(4, 7), checkedMove.Position);
        Assert.Equal(Now.UtcDateTime, checkedMove.Timestamp);
        Assert.Equal(playerOId, room.CurrentMatch!.TurnManager.CurrentTurnPlayerId);
        Assert.Equal(Symbol.X, room.CurrentMatch.Board.GetSymbol(new Position(4, 7)));
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
        ender.Verify(
            service => service.EndMatchAsync(
                It.IsAny<Room>(),
                It.IsAny<MatchResultType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(MatchResultType.PlayerXWin)]
    [InlineData(MatchResultType.PlayerOWin)]
    [InlineData(MatchResultType.Draw)]
    public async Task SubmitMoveAsync_ForFinalResult_DelegatesAlreadyLoadedRoom(
        MatchResultType finalResult)
    {
        var (room, playerXId, _) = CreatePlayingRoom();
        var (sut, rooms, checker, ender, clock) = CreateSut();
        var cancellationToken = new CancellationTokenSource().Token;
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);
        checker.Setup(service => service.Check(room.CurrentMatch!.Board, It.IsAny<Move>()))
            .Returns(finalResult);
        ender.Setup(service => service.EndMatchAsync(room, finalResult, cancellationToken))
            .ReturnsAsync(room);

        var returned = await sut.SubmitMoveAsync(
            room.RoomId,
            playerXId,
            new Position(3, 3),
            cancellationToken);

        Assert.Same(room, returned);
        Assert.Single(room.CurrentMatch!.MoveHistory);
        Assert.Equal(Symbol.X, room.CurrentMatch.Board.GetSymbol(new Position(3, 3)));
        ender.Verify(
            service => service.EndMatchAsync(room, finalResult, cancellationToken),
            Times.Once);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenRoomIsWaiting_RejectsMoveWithoutCheckingOrSaving()
    {
        var playerXId = Guid.NewGuid();
        var room = new Room(
            new PlayerSlot(playerXId, Symbol.X),
            new PlayerSlot(Guid.NewGuid(), Symbol.O));
        var (sut, rooms, checker, ender, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                playerXId,
                new Position(0, 0),
                CancellationToken.None));

        Assert.Empty(room.ReadyPlayers);
        checker.Verify(
            service => service.Check(It.IsAny<Board>(), It.IsAny<Move>()),
            Times.Never);
        ender.Verify(
            service => service.EndMatchAsync(
                It.IsAny<Room>(),
                It.IsAny<MatchResultType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenItIsNotPlayersTurn_DoesNotMutateOrPersist()
    {
        var (room, _, playerOId) = CreatePlayingRoom();
        var (sut, rooms, checker, _, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                playerOId,
                new Position(0, 0),
                CancellationToken.None));

        Assert.Empty(room.CurrentMatch!.MoveHistory);
        Assert.Null(room.CurrentMatch.Board.GetSymbol(new Position(0, 0)));
        checker.Verify(
            service => service.Check(It.IsAny<Board>(), It.IsAny<Move>()),
            Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenPlayerIsNotInRoom_DoesNotMutateOrPersist()
    {
        var (room, _, _) = CreatePlayingRoom();
        var (sut, rooms, checker, _, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                Guid.NewGuid(),
                new Position(0, 0),
                CancellationToken.None));

        Assert.Empty(room.CurrentMatch!.MoveHistory);
        checker.Verify(
            service => service.Check(It.IsAny<Board>(), It.IsAny<Move>()),
            Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenPositionIsOutsideBoard_DoesNotAdvanceTurn()
    {
        var (room, playerXId, _) = CreatePlayingRoom(boardSize: 5);
        var originalTurn = room.CurrentMatch!.TurnManager.CurrentTurnPlayerId;
        var (sut, rooms, checker, _, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                playerXId,
                new Position(5, 0),
                CancellationToken.None));

        Assert.Equal(originalTurn, room.CurrentMatch.TurnManager.CurrentTurnPlayerId);
        Assert.Empty(room.CurrentMatch.MoveHistory);
        checker.Verify(
            service => service.Check(It.IsAny<Board>(), It.IsAny<Move>()),
            Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenCellIsOccupied_DoesNotAddSecondMove()
    {
        var (room, playerXId, playerOId) = CreatePlayingRoom();
        room.ApplyMove(playerXId, new Position(2, 2), Now.UtcDateTime.AddSeconds(-1));
        var (sut, rooms, checker, _, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                playerOId,
                new Position(2, 2),
                CancellationToken.None));

        Assert.Single(room.CurrentMatch!.MoveHistory);
        Assert.Equal(playerOId, room.CurrentMatch.TurnManager.CurrentTurnPlayerId);
        checker.Verify(
            service => service.Check(It.IsAny<Board>(), It.IsAny<Move>()),
            Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenTurnDeadlineHasPassed_DoesNotApplyMove()
    {
        var (room, playerXId, _) = CreatePlayingRoom(turnDurationSec: 4);
        var afterDeadline = new DateTimeOffset(
            room.CurrentMatch!.TurnManager.TurnDeadline.AddSeconds(1),
            TimeSpan.Zero);
        var (sut, rooms, checker, _, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(afterDeadline);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                playerXId,
                new Position(1, 1),
                CancellationToken.None));

        Assert.Empty(room.CurrentMatch.MoveHistory);
        checker.Verify(
            service => service.Check(It.IsAny<Board>(), It.IsAny<Move>()),
            Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenMatchIsPaused_DoesNotApplyMove()
    {
        var (room, playerXId, _) = CreatePlayingRoom();
        room.MarkDisconnected(playerXId, 60, Now.UtcDateTime.AddSeconds(-1));
        var (sut, rooms, checker, _, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                playerXId,
                new Position(1, 1),
                CancellationToken.None));

        Assert.Empty(room.CurrentMatch!.MoveHistory);
        Assert.True(room.CurrentMatch.TurnManager.IsPaused);
        checker.Verify(
            service => service.Check(It.IsAny<Board>(), It.IsAny<Move>()),
            Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task SubmitMoveAsync_WhenUpdateFails_PropagatesFailure()
    {
        var (room, playerXId, _) = CreatePlayingRoom();
        var (sut, rooms, checker, _, clock) = CreateSut();
        var expected = new InvalidOperationException("write failed");
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .ThrowsAsync(expected);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);
        checker.Setup(service => service.Check(room.CurrentMatch!.Board, It.IsAny<Move>()))
            .Returns(MatchResultType.Continue);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitMoveAsync(
                room.RoomId,
                playerXId,
                new Position(1, 1),
                CancellationToken.None));

        Assert.Same(expected, actual);
    }

    private static (
        MoveSubmitter Sut,
        Mock<IRoomRepository> Rooms,
        Mock<IWinConditionChecker> Checker,
        Mock<IMatchEnder> Ender,
        Mock<TimeProvider> Clock) CreateSut()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var checker = new Mock<IWinConditionChecker>(MockBehavior.Strict);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var clock = new Mock<TimeProvider>(MockBehavior.Strict);
        return (
            new MoveSubmitter(rooms.Object, checker.Object, ender.Object, clock.Object),
            rooms,
            checker,
            ender,
            clock);
    }

    private static (Room Room, Guid PlayerXId, Guid PlayerOId) CreatePlayingRoom(
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
        room.MarkReady(playerXId);
        room.MarkReady(playerOId);
        room.StartNewMatch(Now.UtcDateTime.AddSeconds(-2));
        return (room, playerXId, playerOId);
    }
}
