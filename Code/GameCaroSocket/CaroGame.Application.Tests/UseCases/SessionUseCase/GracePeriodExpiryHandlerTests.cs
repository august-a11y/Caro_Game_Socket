using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.SessionUseCase;

public sealed class GracePeriodExpiryHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullRoomRepository_ThrowsArgumentNullException()
    {
        var ender = new Mock<IMatchEnder>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new GracePeriodExpiryHandler(null!, ender, new TestTimeProvider(Now)));

        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullMatchEnder_ThrowsArgumentNullException()
    {
        var rooms = new Mock<IRoomRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new GracePeriodExpiryHandler(rooms, null!, new TestTimeProvider(Now)));

        Assert.Equal("matchEnder", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var rooms = new Mock<IRoomRepository>().Object;
        var ender = new Mock<IMatchEnder>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new GracePeriodExpiryHandler(rooms, ender, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ThrowsKeyNotFound()
    {
        var roomId = Guid.NewGuid();
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetByIdAsync(roomId))
            .ReturnsAsync((Room?)null);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.HandleAsync(roomId, Guid.NewGuid(), CancellationToken.None));

        Assert.Contains(roomId.ToString(), exception.Message, StringComparison.Ordinal);
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRoomIsWaiting_ReturnsWithoutEndingMatchEvenAfterDeadline()
    {
        var room = CreateRoom(playing: false);
        room.MarkDisconnected(
            room.PlayerX.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddMinutes(-2).UtcDateTime);
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);

        var result = await handler.HandleAsync(
            room.RoomId,
            room.PlayerX.PlayerId,
            CancellationToken.None);

        Assert.Same(room, result);
        Assert.Equal(RoomStatus.Waiting, room.Status);
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRoomIsFinished_ReturnsWithoutEndingMatchAgain()
    {
        var room = CreateRoom(playing: true);
        room.MarkDisconnected(
            room.PlayerX.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddMinutes(-1).UtcDateTime);
        room.EndMatch(MatchResultType.PlayerOWin);
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);

        var result = await handler.HandleAsync(
            room.RoomId,
            room.PlayerX.PlayerId,
            CancellationToken.None);

        Assert.Same(room, result);
        Assert.Equal(RoomStatus.Finished, room.Status);
        Assert.Equal(MatchResultType.PlayerOWin, room.CurrentMatch!.Result);
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerHasNoDisconnectRecord_ReturnsWithoutEndingMatch()
    {
        var room = CreateRoom(playing: true);
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);

        var result = await handler.HandleAsync(
            room.RoomId,
            room.PlayerX.PlayerId,
            CancellationToken.None);

        Assert.Same(room, result);
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_BeforeGraceDeadline_ReturnsWithoutEndingMatch()
    {
        var room = CreateRoom(playing: true);
        room.MarkDisconnected(
            room.PlayerX.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-59).UtcDateTime);
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);

        var result = await handler.HandleAsync(
            room.RoomId,
            room.PlayerX.PlayerId,
            CancellationToken.None);

        Assert.Same(room, result);
        Assert.Equal(MatchResultType.Continue, room.CurrentMatch!.Result);
        ender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, MatchResultType.PlayerOWin)]
    [InlineData(false, MatchResultType.PlayerXWin)]
    public async Task HandleAsync_WhenOnePlayersGraceExpires_DelegatesOpponentWin(
        bool expirePlayerX,
        MatchResultType expectedResult)
    {
        var room = CreateRoom(playing: true);
        var expiredPlayerId = expirePlayerX
            ? room.PlayerX.PlayerId
            : room.PlayerO.PlayerId;
        room.MarkDisconnected(
            expiredPlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-60).UtcDateTime);
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        ender.Setup(service => service.EndMatchAsync(
                room,
                expectedResult,
                CancellationToken.None))
            .ReturnsAsync(room);
        var handler = CreateHandler(rooms, ender);

        var result = await handler.HandleAsync(
            room.RoomId,
            expiredPlayerId,
            CancellationToken.None);

        Assert.Same(room, result);
        ender.Verify(service => service.EndMatchAsync(
            room,
            expectedResult,
            CancellationToken.None), Times.Once);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenBothPlayersGracePeriodsExpired_DelegatesDraw()
    {
        var room = CreateRoom(playing: true);
        room.MarkDisconnected(
            room.PlayerX.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-70).UtcDateTime);
        room.MarkDisconnected(
            room.PlayerO.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-60).UtcDateTime);
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        ender.Setup(service => service.EndMatchAsync(
                room,
                MatchResultType.Draw,
                CancellationToken.None))
            .ReturnsAsync(room);
        var handler = CreateHandler(rooms, ender);

        var result = await handler.HandleAsync(
            room.RoomId,
            room.PlayerO.PlayerId,
            CancellationToken.None);

        Assert.Same(room, result);
        ender.Verify(service => service.EndMatchAsync(
            room,
            MatchResultType.Draw,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenOnlyOtherPlayerExpired_DoesNotEndForRequestedPlayer()
    {
        var room = CreateRoom(playing: true);
        room.MarkDisconnected(
            room.PlayerX.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-61).UtcDateTime);
        room.MarkDisconnected(
            room.PlayerO.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-10).UtcDateTime);
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);

        var result = await handler.HandleAsync(
            room.RoomId,
            room.PlayerO.PlayerId,
            CancellationToken.None);

        Assert.Same(room, result);
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithPreCancelledToken_ThrowsWithoutUsingDependencies()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), cancellation.Token));

        rooms.VerifyNoOtherCalls();
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCancelledDuringRoomLookup_DoesNotEndMatch()
    {
        var room = CreateRoom(playing: true);
        room.MarkDisconnected(
            room.PlayerX.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-60).UtcDateTime);
        var completion = new TaskCompletionSource<Room?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .Returns(completion.Task);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);
        using var cancellation = new CancellationTokenSource();

        var operation = handler.HandleAsync(
            room.RoomId,
            room.PlayerX.PlayerId,
            cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(room);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRoomLookupFails_PropagatesException()
    {
        var roomId = Guid.NewGuid();
        var expected = new InvalidOperationException("lookup failed");
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetByIdAsync(roomId))
            .ThrowsAsync(expected);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        var handler = CreateHandler(rooms, ender);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(roomId, Guid.NewGuid(), CancellationToken.None));

        Assert.Same(expected, actual);
        ender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenMatchEnderFails_PropagatesException()
    {
        var room = CreateRoom(playing: true);
        room.MarkDisconnected(
            room.PlayerX.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-60).UtcDateTime);
        var expected = new InvalidOperationException("ending failed");
        var rooms = CreateRoomRepository(room);
        var ender = new Mock<IMatchEnder>(MockBehavior.Strict);
        ender.Setup(service => service.EndMatchAsync(
                room,
                MatchResultType.PlayerOWin,
                CancellationToken.None))
            .ThrowsAsync(expected);
        var handler = CreateHandler(rooms, ender);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                room.RoomId,
                room.PlayerX.PlayerId,
                CancellationToken.None));

        Assert.Same(expected, actual);
    }

    private static GracePeriodExpiryHandler CreateHandler(
        Mock<IRoomRepository> rooms,
        Mock<IMatchEnder> ender) =>
        new(rooms.Object, ender.Object, new TestTimeProvider(Now));

    private static Mock<IRoomRepository> CreateRoomRepository(Room room)
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetByIdAsync(room.RoomId))
            .ReturnsAsync(room);
        return rooms;
    }

    private static Room CreateRoom(bool playing)
    {
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var room = new Room(
            new PlayerSlot(playerXId, Symbol.X),
            new PlayerSlot(playerOId, Symbol.O),
            createdAt: Now.AddMinutes(-3).UtcDateTime);

        if (playing)
        {
            room.MarkReady(playerXId);
            room.MarkReady(playerOId);
            room.StartNewMatch(Now.AddMinutes(-2).UtcDateTime);
        }

        return room;
    }
}
