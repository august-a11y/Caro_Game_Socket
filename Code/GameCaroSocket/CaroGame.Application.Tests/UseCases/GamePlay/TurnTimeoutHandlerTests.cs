using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using Moq;

namespace CaroGame.Application.Tests.UseCases.GamePlay;

public sealed class TurnTimeoutHandlerTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 9, 3, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullDependency_Throws()
    {
        var rooms = new Mock<IRoomRepository>();
        var ender = new Mock<IMatchEnder>();
        var clock = new Mock<TimeProvider>();

        Assert.Throws<ArgumentNullException>(() =>
            new TurnTimeoutHandler(null!, ender.Object, clock.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new TurnTimeoutHandler(rooms.Object, null!, clock.Object));
        Assert.Throws<ArgumentNullException>(() =>
            new TurnTimeoutHandler(rooms.Object, ender.Object, null!));
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenCancellationWasRequested_DoesNotLoadRoom()
    {
        var (sut, _, _, _) = CreateSut();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.HandleTurnTimeoutAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                cancellation.Token));
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenRoomIdentifierIsEmpty_Throws()
    {
        var (sut, _, _, _) = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.HandleTurnTimeoutAsync(
                Guid.Empty,
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Equal("roomId", exception.ParamName);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenPlayerIdentifierIsEmpty_Throws()
    {
        var (sut, _, _, _) = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.HandleTurnTimeoutAsync(
                Guid.NewGuid(),
                Guid.Empty,
                CancellationToken.None));

        Assert.Equal("playerId", exception.ParamName);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenRoomDoesNotExist_IsNoOp()
    {
        var (sut, rooms, ender, _) = CreateSut();
        var roomId = Guid.NewGuid();
        rooms.Setup(repository => repository.GetByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        await sut.HandleTurnTimeoutAsync(
            roomId,
            Guid.NewGuid(),
            CancellationToken.None);

        VerifyNeverEnded(ender);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenRoomIsWaiting_IsNoOp()
    {
        var (room, playerXId, _) = CreateWaitingRoom();
        var (sut, rooms, ender, _) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);

        await sut.HandleTurnTimeoutAsync(
            room.RoomId,
            playerXId,
            CancellationToken.None);

        VerifyNeverEnded(ender);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenRoomIsFinished_IsNoOp()
    {
        var (room, playerXId, _) = CreatePlayingRoom();
        room.EndMatch(MatchResultType.Draw);
        var (sut, rooms, ender, _) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);

        await sut.HandleTurnTimeoutAsync(
            room.RoomId,
            playerXId,
            CancellationToken.None);

        VerifyNeverEnded(ender);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_ForStalePlayerTimer_IsNoOp()
    {
        var (room, _, playerOId) = CreatePlayingRoom();
        var (sut, rooms, ender, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow())
            .Returns(StartTime.AddMinutes(1));

        await sut.HandleTurnTimeoutAsync(
            room.RoomId,
            playerOId,
            CancellationToken.None);

        VerifyNeverEnded(ender);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_BeforeDeadline_IsNoOp()
    {
        var (room, playerXId, _) = CreatePlayingRoom();
        var (sut, rooms, ender, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow())
            .Returns(StartTime.AddSeconds(29));

        await sut.HandleTurnTimeoutAsync(
            room.RoomId,
            playerXId,
            CancellationToken.None);

        VerifyNeverEnded(ender);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenTurnIsPaused_IsNoOpEvenPastOriginalDeadline()
    {
        var (room, playerXId, _) = CreatePlayingRoom();
        room.MarkDisconnected(
            playerXId,
            gracePeriodSeconds: 60,
            StartTime.UtcDateTime.AddSeconds(10));
        var (sut, rooms, ender, clock) = CreateSut();
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow())
            .Returns(StartTime.AddMinutes(2));

        await sut.HandleTurnTimeoutAsync(
            room.RoomId,
            playerXId,
            CancellationToken.None);

        Assert.True(room.CurrentMatch!.TurnManager.IsPaused);
        VerifyNeverEnded(ender);
    }

    [Theory]
    [InlineData(true, MatchResultType.PlayerOWin)]
    [InlineData(false, MatchResultType.PlayerXWin)]
    public async Task HandleTurnTimeoutAsync_WhenDeadlineIsReached_CurrentPlayerLoses(
        bool playerXTimesOut,
        MatchResultType expectedResult)
    {
        var (room, playerXId, playerOId) = CreatePlayingRoom();
        DateTimeOffset deadline;
        Guid timedOutPlayer;
        if (playerXTimesOut)
        {
            timedOutPlayer = playerXId;
            deadline = new DateTimeOffset(
                room.CurrentMatch!.TurnManager.TurnDeadline,
                TimeSpan.Zero);
        }
        else
        {
            room.ApplyMove(
                playerXId,
                new Position(0, 0),
                StartTime.UtcDateTime.AddSeconds(1));
            timedOutPlayer = playerOId;
            deadline = new DateTimeOffset(
                room.CurrentMatch!.TurnManager.TurnDeadline,
                TimeSpan.Zero);
        }

        var (sut, rooms, ender, clock) = CreateSut();
        var cancellationToken = new CancellationTokenSource().Token;
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow()).Returns(deadline);
        ender.Setup(service => service.EndMatchAsync(room, expectedResult, cancellationToken))
            .ReturnsAsync(room);

        await sut.HandleTurnTimeoutAsync(
            room.RoomId,
            timedOutPlayer,
            cancellationToken);

        ender.Verify(
            service => service.EndMatchAsync(room, expectedResult, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleTurnTimeoutAsync_WhenEnderFails_PropagatesFailure()
    {
        var (room, playerXId, _) = CreatePlayingRoom();
        var (sut, rooms, ender, clock) = CreateSut();
        var expected = new InvalidOperationException("end failed");
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        clock.Setup(provider => provider.GetUtcNow())
            .Returns(StartTime.AddSeconds(30));
        ender.Setup(service => service.EndMatchAsync(
                room,
                MatchResultType.PlayerOWin,
                CancellationToken.None))
            .ThrowsAsync(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.HandleTurnTimeoutAsync(
                room.RoomId,
                playerXId,
                CancellationToken.None));

        Assert.Same(expected, actual);
    }

    private static (
        TurnTimeoutHandler Sut,
        Mock<IRoomRepository> Rooms,
        Mock<IMatchEnder> Ender,
        Mock<TimeProvider> Clock) CreateSut()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var clock = new Mock<TimeProvider>(MockBehavior.Strict);
        return (
            new TurnTimeoutHandler(rooms.Object, ender.Object, clock.Object),
            rooms,
            ender,
            clock);
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
        room.StartNewMatch(StartTime.UtcDateTime);
        return (room, playerXId, playerOId);
    }

    private static void VerifyNeverEnded(Mock<IMatchEnder> ender) =>
        ender.Verify(
            service => service.EndMatchAsync(
                It.IsAny<Room>(),
                It.IsAny<MatchResultType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
}
