using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.Match;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;

namespace CaroGame.Application.Tests.UseCases.Match;

public sealed class SpectatorLeaverTests
{
    private static readonly DateTime StartTime =
        new(2026, 9, 3, 5, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenRepositoryIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SpectatorLeaver(null!));
    }

    [Fact]
    public async Task LeaveSpectator_WhenCancellationWasRequested_DoesNotLoadRoom()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var sut = new SpectatorLeaver(repository.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.LeaveSpectator(
                Guid.NewGuid(),
                Guid.NewGuid(),
                cancellation.Token));
    }

    [Fact]
    public async Task LeaveSpectator_WhenRoomIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var sut = new SpectatorLeaver(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.LeaveSpectator(Guid.Empty, Guid.NewGuid()));

        Assert.Equal("roomId", exception.ParamName);
    }

    [Fact]
    public async Task LeaveSpectator_WhenPlayerIdentifierIsEmpty_ThrowsBeforeRepositoryCall()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var sut = new SpectatorLeaver(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.LeaveSpectator(Guid.NewGuid(), Guid.Empty));

        Assert.Equal("playerId", exception.ParamName);
    }

    [Fact]
    public async Task LeaveSpectator_WhenRoomDoesNotExist_ThrowsKeyNotFound()
    {
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        var roomId = Guid.NewGuid();
        repository.Setup(value => value.GetByIdAsync(roomId))
            .ReturnsAsync((Room?)null);
        var sut = new SpectatorLeaver(repository.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.LeaveSpectator(roomId, Guid.NewGuid()));
    }

    [Fact]
    public async Task LeaveSpectator_WhenPlayerIsNotSpectating_IsIdempotentWithoutWrite()
    {
        var (room, _, _) = CreatePlayingRoom();
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        var sut = new SpectatorLeaver(repository.Object);

        var returned = await sut.LeaveSpectator(room.RoomId, Guid.NewGuid());

        Assert.Same(room, returned);
        Assert.Empty(room.Spectators);
        repository.Verify(value => value.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomStatus.Waiting)]
    [InlineData(RoomStatus.Playing)]
    [InlineData(RoomStatus.Finished)]
    public async Task LeaveSpectator_InAnyRoomState_RemovesAndPersists(RoomStatus status)
    {
        var (room, spectatorId) = CreateRoomWithSpectator(status);
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        repository.Setup(value => value.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var sut = new SpectatorLeaver(repository.Object);

        var returned = await sut.LeaveSpectator(room.RoomId, spectatorId);

        Assert.Same(room, returned);
        Assert.Empty(room.Spectators);
        Assert.Equal(status, room.Status);
        repository.Verify(value => value.UpdateAsync(room), Times.Once);
    }

    [Fact]
    public async Task LeaveSpectator_WhenCalledTwice_PerformsOnlyOneWrite()
    {
        var (room, spectatorId) = CreateRoomWithSpectator(RoomStatus.Playing);
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        repository.Setup(value => value.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var sut = new SpectatorLeaver(repository.Object);

        await sut.LeaveSpectator(room.RoomId, spectatorId);
        var returned = await sut.LeaveSpectator(room.RoomId, spectatorId);

        Assert.Same(room, returned);
        Assert.Empty(room.Spectators);
        repository.Verify(value => value.UpdateAsync(room), Times.Once);
    }

    [Fact]
    public async Task LeaveSpectator_WhenPersistenceFails_PropagatesFailure()
    {
        var (room, spectatorId) = CreateRoomWithSpectator(RoomStatus.Playing);
        var expected = new InvalidOperationException("write failed");
        var repository = new Mock<IRoomRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        repository.Setup(value => value.UpdateAsync(room))
            .ThrowsAsync(expected);
        var sut = new SpectatorLeaver(repository.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.LeaveSpectator(room.RoomId, spectatorId));

        Assert.Same(expected, actual);
        Assert.Empty(room.Spectators);
    }

    private static (Room Room, Guid SpectatorId) CreateRoomWithSpectator(RoomStatus status)
    {
        var (room, _, _) = CreatePlayingRoom();
        var spectatorId = Guid.NewGuid();
        room.AddSpectator(spectatorId);

        if (status != RoomStatus.Playing)
        {
            room.EndMatch(MatchResultType.Draw);
            if (status == RoomStatus.Waiting)
                room.PrepareRematch();
        }

        return (room, spectatorId);
    }

    private static (Room Room, Guid PlayerXId, Guid PlayerOId) CreatePlayingRoom()
    {
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var room = new Room(
            new PlayerSlot(playerXId, Symbol.X),
            new PlayerSlot(playerOId, Symbol.O));
        room.MarkReady(playerXId);
        room.MarkReady(playerOId);
        room.StartNewMatch(StartTime);
        return (room, playerXId, playerOId);
    }
}
