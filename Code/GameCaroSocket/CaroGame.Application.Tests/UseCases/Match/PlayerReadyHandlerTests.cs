using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.Match;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;

namespace CaroGame.Application.Tests.UseCases.Match;

public sealed class PlayerReadyHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullDependency_Throws()
    {
        var rooms = new Mock<IRoomRepository>();
        var clock = new Mock<TimeProvider>();

        Assert.Throws<ArgumentNullException>(() =>
            new PlayerReadyHandler(null!, clock.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new PlayerReadyHandler(rooms.Object, null!));
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationWasRequested_DoesNotLoadRoom()
    {
        var (sut, _, _) = CreateSut();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.HandleAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                cancellation.Token));
    }

    [Fact]
    public async Task HandleAsync_WhenRoomIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var (sut, _, _) = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.HandleAsync(Guid.Empty, Guid.NewGuid()));

        Assert.Equal("roomId", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var (sut, _, _) = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.HandleAsync(Guid.NewGuid(), Guid.Empty));

        Assert.Equal("playerId", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsKeyNotFound()
    {
        var (sut, rooms, _) = CreateSut();
        var roomId = Guid.NewGuid();
        rooms.Setup(repository => repository.GetByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.HandleAsync(roomId, Guid.NewGuid()));
    }

    [Fact]
    public async Task HandleAsync_WhenCallerIsNotAnActivePlayer_ThrowsWithoutWrite()
    {
        var (room, _, _) = CreateWaitingRoom();
        var (sut, rooms, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(room.RoomId, Guid.NewGuid()));

        Assert.Empty(room.ReadyPlayers);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
        clock.Verify(provider => provider.GetUtcNow(), Times.Never);
    }

    [Theory]
    [InlineData(RoomStatus.Playing)]
    [InlineData(RoomStatus.Finished)]
    public async Task HandleAsync_WhenRoomIsNotWaiting_ThrowsWithoutWrite(RoomStatus status)
    {
        var (room, playerXId, playerOId) = CreateWaitingRoom();
        room.MarkReady(playerXId);
        room.MarkReady(playerOId);
        room.StartNewMatch(Now.UtcDateTime);
        if (status == RoomStatus.Finished)
            room.EndMatch(MatchResultType.Draw);
        var (sut, rooms, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(room.RoomId, playerXId));

        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
        clock.Verify(provider => provider.GetUtcNow(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ForFirstReadyPlayer_StoresReadinessWithoutStartingMatch()
    {
        var (room, playerXId, _) = CreateWaitingRoom();
        var (sut, rooms, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);

        var returned = await sut.HandleAsync(room.RoomId, playerXId);

        Assert.Same(room, returned);
        Assert.Equal(RoomStatus.Waiting, room.Status);
        Assert.Contains(playerXId, room.ReadyPlayers);
        Assert.False(room.ArePlayersReady);
        Assert.Null(room.CurrentMatch);
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
        clock.Verify(provider => provider.GetUtcNow(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerIsAlreadyReady_IsIdempotentWithoutWrite()
    {
        var (room, playerXId, _) = CreateWaitingRoom();
        room.MarkReady(playerXId);
        var (sut, rooms, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);

        var returned = await sut.HandleAsync(room.RoomId, playerXId);

        Assert.Same(room, returned);
        Assert.Single(room.ReadyPlayers);
        Assert.Equal(RoomStatus.Waiting, room.Status);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
        clock.Verify(provider => provider.GetUtcNow(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ForSecondReadyPlayer_StartsMatchAtClockTimeAndPersists()
    {
        var (room, playerXId, playerOId) = CreateWaitingRoom();
        room.MarkReady(playerXId);
        var (sut, rooms, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        clock.Setup(provider => provider.GetUtcNow()).Returns(Now);

        var returned = await sut.HandleAsync(room.RoomId, playerOId);

        Assert.Same(room, returned);
        Assert.Equal(RoomStatus.Playing, room.Status);
        Assert.True(room.ArePlayersReady is false);
        Assert.Empty(room.ReadyPlayers);
        Assert.NotNull(room.CurrentMatch);
        Assert.Equal(Now.UtcDateTime, room.CurrentMatch.StartedAt);
        Assert.Equal(playerXId, room.CurrentMatch.TurnManager.CurrentTurnPlayerId);
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
        clock.Verify(provider => provider.GetUtcNow(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerIsDisconnected_RejectsReadinessWithoutWrite()
    {
        var (room, playerXId, _) = CreateWaitingRoom();
        room.MarkDisconnected(
            playerXId,
            gracePeriodSeconds: 60,
            Now.UtcDateTime);
        var (sut, rooms, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(room.RoomId, playerXId));

        Assert.Empty(room.ReadyPlayers);
        Assert.Contains(playerXId, room.Disconnected.Keys);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
        clock.Verify(provider => provider.GetUtcNow(), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPersistenceFails_PropagatesFailure()
    {
        var (room, playerXId, _) = CreateWaitingRoom();
        var expected = new InvalidOperationException("write failed");
        var (sut, rooms, _) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleAsync(room.RoomId, playerXId));

        Assert.Same(expected, actual);
        Assert.Contains(playerXId, room.ReadyPlayers);
    }

    private static (
        PlayerReadyHandler Sut,
        Mock<IRoomRepository> Rooms,
        Mock<TimeProvider> Clock) CreateSut()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var clock = new Mock<TimeProvider>(MockBehavior.Strict);
        return (new PlayerReadyHandler(rooms.Object, clock.Object), rooms, clock);
    }

    private static (Room Room, Guid PlayerXId, Guid PlayerOId) CreateWaitingRoom()
    {
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var room = new Room(
            new PlayerSlot(playerXId, Symbol.X),
            new PlayerSlot(playerOId, Symbol.O));
        return (room, playerXId, playerOId);
    }
}
