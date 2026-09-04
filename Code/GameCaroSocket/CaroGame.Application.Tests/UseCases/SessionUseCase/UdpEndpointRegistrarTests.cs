using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.SessionUseCase;
using CaroGame.Domain.Entities;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.SessionUseCase;

public sealed class UdpEndpointRegistrarTests
{
    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new UdpEndpointRegistrar(null!));

        Assert.Equal("sessionRepository", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(65535)]
    [InlineData(24567)]
    public async Task RegisterAsync_WithConnectedSessionAndValidEndpoint_PersistsEndpoint(int port)
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid());
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .Returns(Task.CompletedTask);
        var registrar = new UdpEndpointRegistrar(sessions.Object);

        await registrar.RegisterAsync(
            playerId,
            "  127.0.0.1  ",
            port,
            CancellationToken.None);

        Assert.Equal($"127.0.0.1:{port}", session.UdpEndpoint);
        sessions.Verify(repository => repository.UpdateAsync(session), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenSessionDoesNotExist_IsSilentNoOp()
    {
        var playerId = Guid.NewGuid();
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync((Session?)null);
        var registrar = new UdpEndpointRegistrar(sessions.Object);

        await registrar.RegisterAsync(playerId, "invalid input is ignored", -1);

        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenSessionIsDisconnected_IsSilentNoOp()
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid());
        session.MarkDisconnected();
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        var registrar = new UdpEndpointRegistrar(sessions.Object);

        await registrar.RegisterAsync(playerId, "invalid input is ignored", -1);

        Assert.Null(session.UdpEndpoint);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_WithBlankAddress_ThrowsWithoutUpdatingSession(string? address)
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid());
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        var registrar = new UdpEndpointRegistrar(sessions.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            registrar.RegisterAsync(playerId, address!, 5000));

        Assert.Equal("address", exception.ParamName);
        Assert.Null(session.UdpEndpoint);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    public async Task RegisterAsync_WithInvalidPort_ThrowsWithoutUpdatingSession(int port)
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid());
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        var registrar = new UdpEndpointRegistrar(sessions.Object);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            registrar.RegisterAsync(playerId, "127.0.0.1", port));

        Assert.Equal("port", exception.ParamName);
        Assert.Null(session.UdpEndpoint);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithPreCancelledToken_ThrowsWithoutQueryingRepository()
    {
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        var registrar = new UdpEndpointRegistrar(sessions.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            registrar.RegisterAsync(
                Guid.NewGuid(),
                "127.0.0.1",
                5000,
                cancellation.Token));

        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_WhenCancelledDuringLookup_DoesNotMutateSession()
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid());
        var completion = new TaskCompletionSource<Session?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .Returns(completion.Task);
        var registrar = new UdpEndpointRegistrar(sessions.Object);
        using var cancellation = new CancellationTokenSource();

        var operation = registrar.RegisterAsync(
            playerId,
            "127.0.0.1",
            5000,
            cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(session);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Null(session.UdpEndpoint);
        sessions.Verify(repository => repository.UpdateAsync(It.IsAny<Session>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenLookupFails_PropagatesException()
    {
        var playerId = Guid.NewGuid();
        var expected = new InvalidOperationException("lookup failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ThrowsAsync(expected);
        var registrar = new UdpEndpointRegistrar(sessions.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registrar.RegisterAsync(playerId, "127.0.0.1", 5000));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task RegisterAsync_WhenUpdateFails_PropagatesAfterMutatingEntity()
    {
        var playerId = Guid.NewGuid();
        var session = new Session(playerId, Guid.NewGuid());
        var expected = new InvalidOperationException("update failed");
        var sessions = new Mock<ISessionRepository>(MockBehavior.Strict);
        sessions.Setup(repository => repository.GetByPlayerIdAsync(playerId))
            .ReturnsAsync(session);
        sessions.Setup(repository => repository.UpdateAsync(session))
            .ThrowsAsync(expected);
        var registrar = new UdpEndpointRegistrar(sessions.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registrar.RegisterAsync(playerId, "127.0.0.1", 5000));

        Assert.Same(expected, actual);
        Assert.Equal("127.0.0.1:5000", session.UdpEndpoint);
    }
}
