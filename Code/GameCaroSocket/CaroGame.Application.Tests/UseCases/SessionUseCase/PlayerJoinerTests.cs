using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.SessionUseCase;

public sealed class PlayerJoinerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 4, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullPlayerRepository_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerJoiner(null!, sessions, new TestTimeProvider(Now)));

        Assert.Equal("playerRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSessionRepository_ThrowsArgumentNullException()
    {
        var players = new Mock<IPlayerRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerJoiner(players, null!, new TestTimeProvider(Now)));

        Assert.Equal("sessionRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var players = new Mock<IPlayerRepository>().Object;
        var sessions = new Mock<ISessionRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PlayerJoiner(players, sessions, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task JoinAsync_WithBlankNickname_ThrowsWithoutUsingRepositories(string? nickname)
    {
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var joiner = CreateJoiner(players, sessions);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            joiner.JoinAsync(nickname!, CancellationToken.None));

        Assert.Equal("nickname", exception.ParamName);
        players.VerifyNoOtherCalls();
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JoinAsync_WithUniqueNickname_PersistsLinkedPlayerAndSessionAtClockTime()
    {
        Player? persistedPlayer = null;
        Session? persistedSession = null;
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.ExistsByNicknameAsync("Alice"))
            .ReturnsAsync(false);
        players.Setup(repository => repository.AddAsync(It.IsAny<Player>()))
            .Callback<Player>(player => persistedPlayer = player)
            .Returns(Task.CompletedTask);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.AddAsync(It.IsAny<Session>()))
            .Callback<Session>(session => persistedSession = session)
            .Returns(Task.CompletedTask);
        var joiner = CreateJoiner(players, sessions);

        var result = await joiner.JoinAsync("  Alice  ", CancellationToken.None);

        Assert.NotNull(persistedPlayer);
        Assert.Same(persistedSession, result);
        Assert.Equal("Alice", persistedPlayer.Nickname);
        Assert.Equal(PlayerStatus.Free, persistedPlayer.Status);
        Assert.NotEqual(Guid.Empty, persistedPlayer.PlayerId);
        Assert.Equal(persistedPlayer.PlayerId, result.PlayerId);
        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Equal(Now.UtcDateTime, result.LastHeartbeatAt);
        Assert.True(result.IsConnected);
        Assert.Null(result.DisconnectedAt);
        players.Verify(repository => repository.ExistsByNicknameAsync("Alice"), Times.Once);
        players.Verify(repository => repository.AddAsync(persistedPlayer), Times.Once);
        sessions.Verify(repository => repository.AddAsync(result), Times.Once);
    }

    [Fact]
    public async Task JoinAsync_WhenNicknameAlreadyExists_ThrowsWithoutCreatingPlayerOrSession()
    {
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.ExistsByNicknameAsync("ALICE"))
            .ReturnsAsync(true);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var joiner = CreateJoiner(players, sessions);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            joiner.JoinAsync(" ALICE ", CancellationToken.None));

        Assert.Contains("ALICE", exception.Message, StringComparison.Ordinal);
        players.Verify(repository => repository.AddAsync(It.IsAny<Player>()), Times.Never);
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JoinAsync_WithPreCancelledToken_ThrowsWithoutUsingRepositories()
    {
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var joiner = CreateJoiner(players, sessions);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            joiner.JoinAsync("Alice", cancellation.Token));

        players.VerifyNoOtherCalls();
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JoinAsync_WhenCancelledDuringNicknameLookup_DoesNotPersistAnything()
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.ExistsByNicknameAsync("Alice"))
            .Returns(completion.Task);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var joiner = CreateJoiner(players, sessions);
        using var cancellation = new CancellationTokenSource();

        var operation = joiner.JoinAsync("Alice", cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(false);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        players.Verify(repository => repository.AddAsync(It.IsAny<Player>()), Times.Never);
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JoinAsync_WhenCancelledAfterPlayerIsPersisted_DoesNotCreateSession()
    {
        using var cancellation = new CancellationTokenSource();
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.ExistsByNicknameAsync("Alice"))
            .ReturnsAsync(false);
        players.Setup(repository => repository.AddAsync(It.IsAny<Player>()))
            .Returns(() =>
            {
                cancellation.Cancel();
                return Task.CompletedTask;
            });
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var joiner = CreateJoiner(players, sessions);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            joiner.JoinAsync("Alice", cancellation.Token));

        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JoinAsync_WhenNicknameLookupFails_PropagatesException()
    {
        var expected = new InvalidOperationException("lookup failed");
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.ExistsByNicknameAsync("Alice"))
            .ThrowsAsync(expected);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var joiner = CreateJoiner(players, sessions);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            joiner.JoinAsync("Alice", CancellationToken.None));

        Assert.Same(expected, actual);
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JoinAsync_WhenPlayerPersistenceFails_DoesNotCreateSession()
    {
        var expected = new InvalidOperationException("player persistence failed");
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.ExistsByNicknameAsync("Alice"))
            .ReturnsAsync(false);
        players.Setup(repository => repository.AddAsync(It.IsAny<Player>()))
            .ThrowsAsync(expected);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var joiner = CreateJoiner(players, sessions);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            joiner.JoinAsync("Alice", CancellationToken.None));

        Assert.Same(expected, actual);
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task JoinAsync_WhenSessionPersistenceFails_PropagatesException()
    {
        var expected = new InvalidOperationException("session persistence failed");
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.ExistsByNicknameAsync("Alice"))
            .ReturnsAsync(false);
        players.Setup(repository => repository.AddAsync(It.IsAny<Player>()))
            .Returns(Task.CompletedTask);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.AddAsync(It.IsAny<Session>()))
            .ThrowsAsync(expected);
        var joiner = CreateJoiner(players, sessions);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            joiner.JoinAsync("Alice", CancellationToken.None));

        Assert.Same(expected, actual);
    }

    private static PlayerJoiner CreateJoiner(
        Mock<IPlayerRepository> players,
        Mock<ISessionRepository> sessions) =>
        new(players.Object, sessions.Object, new TestTimeProvider(Now));
}
