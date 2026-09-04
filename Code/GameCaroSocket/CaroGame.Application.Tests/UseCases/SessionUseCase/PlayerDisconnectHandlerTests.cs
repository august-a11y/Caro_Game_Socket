using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.SessionUseCase;

public sealed class PlayerDisconnectHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullSessionRepository_ThrowsArgumentNullException()
    {
        var rooms = new Mock<IRoomRepository>().Object;
        var players = new Mock<IPlayerRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerDisconnectHandler(null!, rooms, players, new TestTimeProvider(Now)));

        Assert.Equal("sessionRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRoomRepository_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;
        var players = new Mock<IPlayerRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerDisconnectHandler(sessions, null!, players, new TestTimeProvider(Now)));

        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullPlayerRepository_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerDisconnectHandler(sessions, rooms, null!, new TestTimeProvider(Now)));

        Assert.Equal("playerRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;
        var players = new Mock<IPlayerRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerDisconnectHandler(sessions, rooms, players, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionDoesNotExist_IsSilentNoOp()
    {
        var playerId = Guid.NewGuid();
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync((Session?)null);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var handler = CreateHandler(sessions, rooms, players);

        await handler.HandleAsync(playerId, CancellationToken.None);

        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
        players.VerifyNoOtherCalls();
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerDoesNotExist_ThrowsWithoutChangingSession()
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid(), Now.AddMinutes(-1).UtcDateTime);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(playerId))
            .ReturnsAsync((Player?)null);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var handler = CreateHandler(sessions, rooms, players);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.HandleAsync(playerId, CancellationToken.None));

        Assert.Contains(playerId.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.True(session.IsConnected);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerIsNotInRoom_DisconnectsSessionAndMarksPlayerOffline()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Free };
        var session = new Session(player.PlayerId, Guid.NewGuid(), Now.AddMinutes(-1).UtcDateTime);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(player.PlayerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .Returns(Task.CompletedTask);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        players.Setup(repository => repository.UpdateAsync(player))
            .Returns(Task.CompletedTask);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync(Array.Empty<Room>());
        var handler = CreateHandler(sessions, rooms, players);

        await handler.HandleAsync(player.PlayerId, CancellationToken.None);

        Assert.False(session.IsConnected);
        Assert.Equal(Now.UtcDateTime, session.DisconnectedAt);
        Assert.Equal(PlayerStatus.Offline, player.Status);
        sessions.Verify(repository => repository.RemoveAsync(It.IsAny<Guid>()), Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_WhenPlayerIsInPlayingRoom_StartsGracePeriodAndPausesMatch(
        bool disconnectPlayerX)
    {
        var playerX = new Player("Alice") { Status = PlayerStatus.InMatch };
        var playerO = new Player("Bob") { Status = PlayerStatus.InMatch };
        var disconnectedPlayer = disconnectPlayerX ? playerX : playerO;
        var room = CreateRoom(playerX.PlayerId, playerO.PlayerId, playing: true);
        var session = new Session(disconnectedPlayer.PlayerId, Guid.NewGuid());
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(disconnectedPlayer.PlayerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .Returns(Task.CompletedTask);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(disconnectedPlayer.PlayerId))
            .ReturnsAsync(disconnectedPlayer);
        players.Setup(repository => repository.UpdateAsync(disconnectedPlayer))
            .Returns(Task.CompletedTask);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync([room]);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(sessions, rooms, players);

        await handler.HandleAsync(disconnectedPlayer.PlayerId, CancellationToken.None);

        var info = Assert.Single(room.Disconnected).Value;
        Assert.Equal(disconnectedPlayer.PlayerId, info.PlayerId);
        Assert.Equal(Now.UtcDateTime, info.DisconnectedAt);
        Assert.Equal(Now.AddSeconds(60).UtcDateTime, info.GracePeriodEndsAt);
        Assert.True(room.CurrentMatch!.TurnManager.IsPaused);
        Assert.False(session.IsConnected);
        Assert.Equal(Now.UtcDateTime, session.DisconnectedAt);
        Assert.Equal(PlayerStatus.Offline, disconnectedPlayer.Status);
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenPlayerIsInWaitingRoom_RecordsGraceWithoutMatch()
    {
        var player = new Player("Alice") { Status = PlayerStatus.InMatch };
        var opponentId = Guid.NewGuid();
        var room = CreateRoom(player.PlayerId, opponentId, playing: false);
        var session = new Session(player.PlayerId, Guid.NewGuid());
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(player.PlayerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .Returns(Task.CompletedTask);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        players.Setup(repository => repository.UpdateAsync(player))
            .Returns(Task.CompletedTask);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync([room]);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(sessions, rooms, players);

        await handler.HandleAsync(player.PlayerId, CancellationToken.None);

        var info = Assert.Single(room.Disconnected).Value;
        Assert.Equal(Now.UtcDateTime, info.DisconnectedAt);
        Assert.Equal(Now.AddSeconds(60).UtcDateTime, info.GracePeriodEndsAt);
        Assert.Null(room.CurrentMatch);
    }

    [Fact]
    public async Task HandleAsync_WhenDisconnectIsRepeated_PreservesOriginalGraceDeadline()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var opponentId = Guid.NewGuid();
        var room = CreateRoom(player.PlayerId, opponentId, playing: true);
        var originalDisconnect = Now.AddSeconds(-10);
        room.MarkDisconnected(player.PlayerId, 60, originalDisconnect.UtcDateTime);
        var session = new Session(player.PlayerId, Guid.NewGuid());
        session.MarkDisconnected(originalDisconnect.UtcDateTime);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(player.PlayerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .Returns(Task.CompletedTask);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        players.Setup(repository => repository.UpdateAsync(player))
            .Returns(Task.CompletedTask);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync([room]);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(sessions, rooms, players);

        await handler.HandleAsync(player.PlayerId, CancellationToken.None);

        var info = Assert.Single(room.Disconnected).Value;
        Assert.Equal(originalDisconnect.UtcDateTime, info.DisconnectedAt);
        Assert.Equal(originalDisconnect.AddSeconds(60).UtcDateTime, info.GracePeriodEndsAt);
        Assert.Equal(originalDisconnect.UtcDateTime, session.DisconnectedAt);
    }

    [Fact]
    public async Task HandleAsync_WithPreCancelledToken_ThrowsWithoutUsingRepositories()
    {
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var handler = CreateHandler(sessions, rooms, players);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(Guid.NewGuid(), cancellation.Token));

        sessions.VerifyNoOtherCalls();
        players.VerifyNoOtherCalls();
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCancelledDuringRoomLookup_DoesNotMutateEntities()
    {
        var player = new Player("Alice") { Status = PlayerStatus.InMatch };
        var session = new Session(player.PlayerId, Guid.NewGuid());
        var roomCompletion = new TaskCompletionSource<IReadOnlyList<Room>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(player.PlayerId))
            .ReturnsAsync(session);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .Returns(roomCompletion.Task);
        var handler = CreateHandler(sessions, rooms, players);
        using var cancellation = new CancellationTokenSource();

        var operation = handler.HandleAsync(player.PlayerId, cancellation.Token);
        cancellation.Cancel();
        roomCompletion.SetResult(Array.Empty<Room>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.True(session.IsConnected);
        Assert.Equal(PlayerStatus.InMatch, player.Status);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
        players.Verify(repository => repository.UpdateAsync(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionLookupFails_PropagatesException()
    {
        var playerId = Guid.NewGuid();
        var expected = new InvalidOperationException("lookup failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ThrowsAsync(expected);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var handler = CreateHandler(sessions, rooms, players);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(playerId, CancellationToken.None));

        Assert.Same(expected, actual);
        rooms.VerifyNoOtherCalls();
        players.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenSessionUpdateFails_PropagatesWithoutCallingLaterUpdates()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Free };
        var session = new Session(player.PlayerId, Guid.NewGuid());
        var expected = new InvalidOperationException("session update failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(player.PlayerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .ThrowsAsync(expected);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync(Array.Empty<Room>());
        var handler = CreateHandler(sessions, rooms, players);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(player.PlayerId, CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.False(session.IsConnected);
        Assert.Equal(PlayerStatus.Offline, player.Status);
        players.Verify(repository => repository.UpdateAsync(It.IsAny<Player>()), Times.Never);
    }

    private static PlayerDisconnectHandler CreateHandler(
        Mock<ISessionRepository> sessions,
        Mock<IRoomRepository> rooms,
        Mock<IPlayerRepository> players) =>
        new(sessions.Object, rooms.Object, players.Object, new TestTimeProvider(Now));

    private static Room CreateRoom(Guid playerXId, Guid playerOId, bool playing)
    {
        var room = new Room(
            new PlayerSlot(playerXId, Symbol.X),
            new PlayerSlot(playerOId, Symbol.O));

        if (playing)
        {
            room.MarkReady(playerXId);
            room.MarkReady(playerOId);
            room.StartNewMatch(Now.AddSeconds(-10).UtcDateTime);
        }

        return room;
    }
}
