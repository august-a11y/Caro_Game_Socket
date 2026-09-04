using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.SessionUseCase;

public sealed class PlayerReconnectorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullSessionRepository_ThrowsArgumentNullException()
    {
        var players = new Mock<IPlayerRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerReconnector(null!, players, rooms, new TestTimeProvider(Now)));

        Assert.Equal("sessionRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullPlayerRepository_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerReconnector(sessions, null!, rooms, new TestTimeProvider(Now)));

        Assert.Equal("playerRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRoomRepository_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;
        var players = new Mock<IPlayerRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerReconnector(sessions, players, null!, new TestTimeProvider(Now)));

        Assert.Equal("roomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;
        var players = new Mock<IPlayerRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerReconnector(sessions, players, rooms, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenSessionTokenDoesNotExist_ThrowsKeyNotFound()
    {
        var playerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(sessionId))
            .ReturnsAsync((Session?)null);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var reconnector = CreateReconnector(sessions, players, rooms);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            reconnector.ReconnectPlayerAsync(playerId, sessionId, CancellationToken.None));

        Assert.Contains(sessionId.ToString(), exception.Message, StringComparison.Ordinal);
        players.VerifyNoOtherCalls();
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenTokenBelongsToAnotherPlayer_ThrowsUnauthorized()
    {
        var requestedPlayerId = Guid.NewGuid();
        var session = new Session(Guid.NewGuid(), Guid.NewGuid());
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(session.SessionId))
            .ReturnsAsync(session);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var reconnector = CreateReconnector(sessions, players, rooms);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            reconnector.ReconnectPlayerAsync(
                requestedPlayerId,
                session.SessionId,
                CancellationToken.None));

        Assert.True(session.IsConnected);
        players.VerifyNoOtherCalls();
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenPlayerDoesNotExist_ThrowsWithoutMutatingSession()
    {
        var playerId = Guid.NewGuid();
        var session = CreateDisconnectedSession(playerId);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(session.SessionId))
            .ReturnsAsync(session);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(playerId))
            .ReturnsAsync((Player?)null);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var reconnector = CreateReconnector(sessions, players, rooms);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            reconnector.ReconnectPlayerAsync(playerId, session.SessionId, CancellationToken.None));

        Assert.Contains(playerId.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.False(session.IsConnected);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenPlayerHasNoRoom_ReconnectsSameSessionAndMarksPlayerFree()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var session = CreateDisconnectedSession(player.PlayerId);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(session.SessionId))
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
        var reconnector = CreateReconnector(sessions, players, rooms);

        var result = await reconnector.ReconnectPlayerAsync(
            player.PlayerId,
            session.SessionId,
            CancellationToken.None);

        Assert.Same(session, result);
        Assert.True(session.IsConnected);
        Assert.Null(session.DisconnectedAt);
        Assert.Equal(Now.UtcDateTime, session.LastHeartbeatAt);
        Assert.Equal(PlayerStatus.Free, player.Status);
        sessions.Verify(repository => repository.AddAsync(It.IsAny<Session>()), Times.Never);
        sessions.Verify(repository => repository.UpdateAsync(session), Times.Once);
        players.Verify(repository => repository.UpdateAsync(player), Times.Once);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task ReconnectPlayerAsync_ToWaitingRoomAfterGraceDeadline_StillReconnects()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var opponentId = Guid.NewGuid();
        var room = CreateRoom(player.PlayerId, opponentId, playing: false);
        room.MarkReady(player.PlayerId);
        room.MarkReady(opponentId);
        room.MarkDisconnected(
            player.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddMinutes(-2).UtcDateTime);
        var session = CreateDisconnectedSession(player.PlayerId);
        var (sessions, players, rooms) = CreateSuccessfulRepositories(player, session, room);
        var reconnector = CreateReconnector(sessions, players, rooms);

        var result = await reconnector.ReconnectPlayerAsync(
            player.PlayerId,
            session.SessionId,
            CancellationToken.None);

        Assert.Same(session, result);
        Assert.True(session.IsConnected);
        Assert.Equal(PlayerStatus.InMatch, player.Status);
        Assert.Empty(room.Disconnected);
        Assert.DoesNotContain(player.PlayerId, room.ReadyPlayers);
        Assert.Contains(opponentId, room.ReadyPlayers);
        Assert.Null(room.CurrentMatch);
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
    }

    [Fact]
    public async Task ReconnectPlayerAsync_ToPlayingRoomWithinGrace_ResumesPausedMatch()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var opponentId = Guid.NewGuid();
        var room = CreateRoom(player.PlayerId, opponentId, playing: true);
        room.MarkDisconnected(
            player.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-10).UtcDateTime);
        var session = CreateDisconnectedSession(player.PlayerId);
        var (sessions, players, rooms) = CreateSuccessfulRepositories(player, session, room);
        var reconnector = CreateReconnector(sessions, players, rooms);

        await reconnector.ReconnectPlayerAsync(
            player.PlayerId,
            session.SessionId,
            CancellationToken.None);

        Assert.True(session.IsConnected);
        Assert.Equal(PlayerStatus.InMatch, player.Status);
        Assert.Empty(room.Disconnected);
        Assert.False(room.CurrentMatch!.TurnManager.IsPaused);
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenOpponentRemainsDisconnected_KeepsMatchPaused()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var opponentId = Guid.NewGuid();
        var room = CreateRoom(player.PlayerId, opponentId, playing: true);
        room.MarkDisconnected(
            player.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-10).UtcDateTime);
        room.MarkDisconnected(
            opponentId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-5).UtcDateTime);
        var session = CreateDisconnectedSession(player.PlayerId);
        var (sessions, players, rooms) = CreateSuccessfulRepositories(player, session, room);
        var reconnector = CreateReconnector(sessions, players, rooms);

        await reconnector.ReconnectPlayerAsync(
            player.PlayerId,
            session.SessionId,
            CancellationToken.None);

        Assert.DoesNotContain(player.PlayerId, room.Disconnected.Keys);
        Assert.Contains(opponentId, room.Disconnected.Keys);
        Assert.True(room.CurrentMatch!.TurnManager.IsPaused);
    }

    [Fact]
    public async Task ReconnectPlayerAsync_AtPlayingRoomGraceDeadline_RejectsWithoutMutation()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var opponentId = Guid.NewGuid();
        var room = CreateRoom(player.PlayerId, opponentId, playing: true);
        room.MarkDisconnected(
            player.PlayerId,
            gracePeriodSeconds: 60,
            Now.AddSeconds(-60).UtcDateTime);
        var session = CreateDisconnectedSession(player.PlayerId);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(session.SessionId))
            .ReturnsAsync(session);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync([room]);
        var reconnector = CreateReconnector(sessions, players, rooms);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reconnector.ReconnectPlayerAsync(
                player.PlayerId,
                session.SessionId,
                CancellationToken.None));

        Assert.Contains("expired", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(session.IsConnected);
        Assert.Equal(PlayerStatus.Offline, player.Status);
        Assert.Contains(player.PlayerId, room.Disconnected.Keys);
        Assert.True(room.CurrentMatch!.TurnManager.IsPaused);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
        players.Verify(repository => repository.UpdateAsync(It.IsAny<Player>()), Times.Never);
        rooms.Verify(repository => repository.UpdateAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WithPreCancelledToken_ThrowsWithoutUsingRepositories()
    {
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var reconnector = CreateReconnector(sessions, players, rooms);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reconnector.ReconnectPlayerAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                cancellation.Token));

        sessions.VerifyNoOtherCalls();
        players.VerifyNoOtherCalls();
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenCancelledDuringRoomLookup_DoesNotMutateEntities()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var session = CreateDisconnectedSession(player.PlayerId);
        var roomCompletion = new TaskCompletionSource<IReadOnlyList<Room>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(session.SessionId))
            .ReturnsAsync(session);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .Returns(roomCompletion.Task);
        var reconnector = CreateReconnector(sessions, players, rooms);
        using var cancellation = new CancellationTokenSource();

        var operation = reconnector.ReconnectPlayerAsync(
            player.PlayerId,
            session.SessionId,
            cancellation.Token);
        cancellation.Cancel();
        roomCompletion.SetResult(Array.Empty<Room>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.False(session.IsConnected);
        Assert.Equal(PlayerStatus.Offline, player.Status);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
        players.Verify(repository => repository.UpdateAsync(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenSessionLookupFails_PropagatesException()
    {
        var playerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var expected = new InvalidOperationException("lookup failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(sessionId))
            .ThrowsAsync(expected);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var reconnector = CreateReconnector(sessions, players, rooms);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reconnector.ReconnectPlayerAsync(playerId, sessionId, CancellationToken.None));

        Assert.Same(expected, actual);
        players.VerifyNoOtherCalls();
        rooms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconnectPlayerAsync_WhenSessionUpdateFails_PropagatesWithoutLaterUpdates()
    {
        var player = new Player("Alice") { Status = PlayerStatus.Offline };
        var session = CreateDisconnectedSession(player.PlayerId);
        var expected = new InvalidOperationException("session update failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(session.SessionId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .ThrowsAsync(expected);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(player.PlayerId))
            .ReturnsAsync(player);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync(Array.Empty<Room>());
        var reconnector = CreateReconnector(sessions, players, rooms);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reconnector.ReconnectPlayerAsync(
                player.PlayerId,
                session.SessionId,
                CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.True(session.IsConnected);
        Assert.Equal(PlayerStatus.Free, player.Status);
        players.Verify(repository => repository.UpdateAsync(It.IsAny<Player>()), Times.Never);
    }

    private static PlayerReconnector CreateReconnector(
        Mock<ISessionRepository> sessions,
        Mock<IPlayerRepository> players,
        Mock<IRoomRepository> rooms) =>
        new(sessions.Object, players.Object, rooms.Object, new TestTimeProvider(Now));

    private static Session CreateDisconnectedSession(Guid playerId)
    {
        var session = new Session(
            playerId,
            Guid.NewGuid(),
            Now.AddMinutes(-2).UtcDateTime);
        session.MarkDisconnected(Now.AddMinutes(-1).UtcDateTime);
        return session;
    }

    private static Room CreateRoom(Guid playerXId, Guid playerOId, bool playing)
    {
        var room = new Room(
            new PlayerSlot(playerXId, Symbol.X),
            new PlayerSlot(playerOId, Symbol.O),
            createdAt: Now.AddMinutes(-1).UtcDateTime);

        if (playing)
        {
            room.MarkReady(playerXId);
            room.MarkReady(playerOId);
            room.StartNewMatch(Now.AddMinutes(-2).UtcDateTime);
        }

        return room;
    }

    private static (
        Mock<ISessionRepository> Sessions,
        Mock<IPlayerRepository> Players,
        Mock<IRoomRepository> Rooms) CreateSuccessfulRepositories(
            Player player,
            Session session,
            Room room)
    {
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByIdAsync(session.SessionId))
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
        return (sessions, players, rooms);
    }
}
