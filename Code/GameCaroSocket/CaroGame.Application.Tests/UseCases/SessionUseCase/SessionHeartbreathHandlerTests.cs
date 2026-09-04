using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Entities;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.SessionUseCase;

public sealed class SessionHeartbeatHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SessionHeartbeatHandler(null!, new TestTimeProvider(Now)));

        Assert.Equal("sessionRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var sessions = new Mock<ISessionRepository>().Object;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SessionHeartbeatHandler(sessions, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    [Fact]
    public async Task HandleAsync_WithConnectedSession_UpdatesHeartbeatAtClockTime()
    {
        var playerId = Guid.NewGuid();
        var session = new Session(
            playerId,
            Guid.NewGuid(),
            Now.AddMinutes(-1).UtcDateTime);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(sessions);

        await handler.HandleAsync(playerId, CancellationToken.None);

        Assert.Equal(Now.UtcDateTime, session.LastHeartbeatAt);
        sessions.Verify(repository => repository.UpdateAsync(session), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionDoesNotExist_IsSilentNoOp()
    {
        var playerId = Guid.NewGuid();
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync((Session?)null);
        var handler = CreateHandler(sessions);

        await handler.HandleAsync(playerId, CancellationToken.None);

        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionIsDisconnected_IsSilentNoOp()
    {
        var playerId = Guid.NewGuid();
        var disconnectedAt = Now.AddSeconds(-10).UtcDateTime;
        var session = new Session(playerId, Guid.NewGuid(), Now.AddMinutes(-1).UtcDateTime);
        session.MarkDisconnected(disconnectedAt);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        var handler = CreateHandler(sessions);

        await handler.HandleAsync(playerId, CancellationToken.None);

        Assert.Equal(disconnectedAt, session.DisconnectedAt);
        Assert.Equal(Now.AddMinutes(-1).UtcDateTime, session.LastHeartbeatAt);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithPreCancelledToken_ThrowsWithoutQueryingRepository()
    {
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var handler = CreateHandler(sessions);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(Guid.NewGuid(), cancellation.Token));

        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCancelledDuringLookup_DoesNotMutateSession()
    {
        var playerId = Guid.NewGuid();
        var originalHeartbeat = Now.AddMinutes(-1).UtcDateTime;
        var session = new Session(playerId, Guid.NewGuid(), originalHeartbeat);
        var completion = new TaskCompletionSource<Session?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .Returns(completion.Task);
        var handler = CreateHandler(sessions);
        using var cancellation = new CancellationTokenSource();

        var operation = handler.HandleAsync(playerId, cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(session);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(originalHeartbeat, session.LastHeartbeatAt);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenLookupFails_PropagatesException()
    {
        var playerId = Guid.NewGuid();
        var expected = new InvalidOperationException("lookup failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ThrowsAsync(expected);
        var handler = CreateHandler(sessions);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(playerId, CancellationToken.None));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task HandleAsync_WhenUpdateFails_PropagatesAfterUpdatingEntity()
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid(), Now.AddMinutes(-1).UtcDateTime);
        var expected = new InvalidOperationException("update failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .ThrowsAsync(expected);
        var handler = CreateHandler(sessions);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(playerId, CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(Now.UtcDateTime, session.LastHeartbeatAt);
    }

    private static SessionHeartbeatHandler CreateHandler(
        Mock<ISessionRepository> sessions) =>
        new(sessions.Object, new TestTimeProvider(Now));
}
