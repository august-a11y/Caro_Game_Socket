using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.Match;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;

namespace CaroGame.Application.Tests.UseCases.Match;

public sealed class SpectatorJoinerTests
{
    private static readonly DateTime StartTime =
        new(2026, 9, 3, 5, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenRepositoryIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SpectatorJoiner(null!));
    }

    [Fact]
    public async Task JoinSpectator_WhenCancellationWasRequested_DoesNotLoadRoom()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var sut = new SpectatorJoiner(repository.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.JoinSpectator(
                Guid.NewGuid(),
                Guid.NewGuid(),
                cancellation.Token));
    }

    [Fact]
    public async Task JoinSpectator_WhenRoomIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var sut = new SpectatorJoiner(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.JoinSpectator(Guid.Empty, Guid.NewGuid()));

        Assert.Equal("roomId", exception.ParamName);
    }

    [Fact]
    public async Task JoinSpectator_WhenPlayerIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var sut = new SpectatorJoiner(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.JoinSpectator(Guid.NewGuid(), Guid.Empty));

        Assert.Equal("playerId", exception.ParamName);
    }

    [Fact]
    public async Task JoinSpectator_WhenRoomDoesNotExist_ThrowsKeyNotFound()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var roomId = Guid.NewGuid();
        repository.Setup(value => value.GetByIdAsync(roomId))
            .ReturnsAsync((Room?)null);
        var sut = new SpectatorJoiner(repository.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.JoinSpectator(roomId, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task JoinSpectator_WhenCallerIsActivePlayer_Throws(bool usePlayerO)
    {
        var (room, playerXId, playerOId) = CreatePlayingRoom();
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        var sut = new SpectatorJoiner(repository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.JoinSpectator(room.RoomId, usePlayerO ? playerOId : playerXId));

        Assert.Empty(room.Spectators);
        repository.Verify(value => value.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomStatus.Waiting)]
    [InlineData(RoomStatus.Finished)]
    public async Task JoinSpectator_WhenMatchIsNotPlaying_Throws(RoomStatus status)
    {
        var (room, _, _) = status == RoomStatus.Waiting
            ? CreateWaitingRoom()
            : CreateFinishedRoom();
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        var sut = new SpectatorJoiner(repository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.JoinSpectator(room.RoomId, Guid.NewGuid()));

        Assert.Empty(room.Spectators);
        repository.Verify(value => value.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task JoinSpectator_WhenEligible_AddsOnceAndPersistsSameRoom()
    {
        var (room, _, _) = CreatePlayingRoom();
        var spectatorId = Guid.NewGuid();
        var originalTurn = room.CurrentMatch!.TurnManager.CurrentTurnPlayerId;
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        repository.Setup(value => value.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var sut = new SpectatorJoiner(repository.Object);

        var returned = await sut.JoinSpectator(room.RoomId, spectatorId);

        Assert.Same(room, returned);
        Assert.Equal([spectatorId], room.Spectators);
        Assert.Equal(RoomStatus.Playing, room.Status);
        Assert.Equal(originalTurn, room.CurrentMatch.TurnManager.CurrentTurnPlayerId);
        Assert.Empty(room.CurrentMatch.MoveHistory);
        repository.Verify(value => value.UpdateAsync(room), Times.Once);
    }

    [Fact]
    public async Task JoinSpectator_WhenAlreadyJoined_IsIdempotentWithoutWrite()
    {
        var (room, _, _) = CreatePlayingRoom();
        var spectatorId = Guid.NewGuid();
        room.AddSpectator(spectatorId);
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        var sut = new SpectatorJoiner(repository.Object);

        var returned = await sut.JoinSpectator(room.RoomId, spectatorId);

        Assert.Same(room, returned);
        Assert.Equal([spectatorId], room.Spectators);
        repository.Verify(value => value.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task JoinSpectator_WhenPersistenceFails_PropagatesFailure()
    {
        var (room, _, _) = CreatePlayingRoom();
        var spectatorId = Guid.NewGuid();
        var expected = new InvalidOperationException("write failed");
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        repository.Setup(value => value.UpdateAsync(room))
            .ThrowsAsync(expected);
        var sut = new SpectatorJoiner(repository.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.JoinSpectator(room.RoomId, spectatorId));

        Assert.Same(expected, actual);
        Assert.Contains(spectatorId, room.Spectators);
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

    private static (Room Room, Guid PlayerXId, Guid PlayerOId) CreatePlayingRoom()
    {
        var (room, playerXId, playerOId) = CreateWaitingRoom();
        room.MarkReady(playerXId);
        room.MarkReady(playerOId);
        room.StartNewMatch(StartTime);
        return (room, playerXId, playerOId);
    }

    private static (Room Room, Guid PlayerXId, Guid PlayerOId) CreateFinishedRoom()
    {
        var (room, playerXId, playerOId) = CreatePlayingRoom();
        room.EndMatch(MatchResultType.Draw);
        return (room, playerXId, playerOId);
    }
}
