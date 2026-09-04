using CaroGame.Domain.Entities;

namespace CaroGame.Domain.Tests.Entities;

public sealed class SessionTests
{
    private static readonly DateTime CreatedAt =
        new(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidValues_CreatesConnectedSessionAtSpecifiedTime()
    {
        var playerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var session = new Session(playerId, sessionId, CreatedAt);

        Assert.Equal(playerId, session.PlayerId);
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal(CreatedAt, session.LastHeartbeatAt);
        Assert.True(session.IsConnected);
        Assert.Null(session.DisconnectedAt);
        Assert.Null(session.UdpEndpoint);
    }

    [Fact]
    public void Constructor_WithEmptyPlayerId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Session(Guid.Empty, Guid.NewGuid(), CreatedAt));

        Assert.Equal("playerId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptySessionId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Session(Guid.NewGuid(), Guid.Empty, CreatedAt));

        Assert.Equal("sessionId", exception.ParamName);
    }

    [Fact]
    public void UpdateHeartbeat_WhenConnected_UsesSpecifiedTimestamp()
    {
        var session = CreateSession();
        var heartbeatAt = CreatedAt.AddSeconds(15);

        session.UpdateHeartbeat(heartbeatAt);

        Assert.Equal(heartbeatAt, session.LastHeartbeatAt);
        Assert.True(session.IsConnected);
    }

    [Fact]
    public void UpdateHeartbeat_WhenDisconnected_ThrowsWithoutChangingHeartbeat()
    {
        var session = CreateSession();
        session.MarkDisconnected(CreatedAt.AddSeconds(10));

        Assert.Throws<InvalidOperationException>(() =>
            session.UpdateHeartbeat(CreatedAt.AddSeconds(20)));

        Assert.Equal(CreatedAt, session.LastHeartbeatAt);
    }

    [Fact]
    public void MarkDisconnected_WhenConnected_RecordsTimestampAndChangesState()
    {
        var session = CreateSession();
        var disconnectedAt = CreatedAt.AddMinutes(1);

        session.MarkDisconnected(disconnectedAt);

        Assert.False(session.IsConnected);
        Assert.Equal(disconnectedAt, session.DisconnectedAt);
    }

    [Fact]
    public void MarkDisconnected_WhenAlreadyDisconnected_IsIdempotentAndPreservesFirstTimestamp()
    {
        var session = CreateSession();
        var firstTimestamp = CreatedAt.AddMinutes(1);
        session.MarkDisconnected(firstTimestamp);

        session.MarkDisconnected(CreatedAt.AddMinutes(2));

        Assert.False(session.IsConnected);
        Assert.Equal(firstTimestamp, session.DisconnectedAt);
    }

    [Fact]
    public void MarkReconnected_ClearsDisconnectionAndRefreshesHeartbeat()
    {
        var session = CreateSession();
        session.MarkDisconnected(CreatedAt.AddMinutes(1));
        var reconnectedAt = CreatedAt.AddMinutes(2);

        session.MarkReconnected(reconnectedAt);

        Assert.True(session.IsConnected);
        Assert.Null(session.DisconnectedAt);
        Assert.Equal(reconnectedAt, session.LastHeartbeatAt);
    }

    [Fact]
    public void MarkReconnected_WhenAlreadyConnected_RemainsConnectedAndRefreshesHeartbeat()
    {
        var session = CreateSession();
        var refreshedAt = CreatedAt.AddMinutes(2);

        session.MarkReconnected(refreshedAt);

        Assert.True(session.IsConnected);
        Assert.Null(session.DisconnectedAt);
        Assert.Equal(refreshedAt, session.LastHeartbeatAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetUdpEndpoint_WithMissingAddress_ThrowsArgumentException(string? address)
    {
        var session = CreateSession();

        var exception = Assert.Throws<ArgumentException>(() =>
            session.SetUdpEndpoint(address!, 3000));

        Assert.Equal("address", exception.ParamName);
        Assert.Null(session.UdpEndpoint);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    public void SetUdpEndpoint_WithPortOutsideValidRange_ThrowsArgumentOutOfRangeException(int port)
    {
        var session = CreateSession();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            session.SetUdpEndpoint("127.0.0.1", port));

        Assert.Equal("port", exception.ParamName);
        Assert.Null(session.UdpEndpoint);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(65535)]
    public void SetUdpEndpoint_WithBoundaryPort_TrimsAddressAndStoresEndpoint(int port)
    {
        var session = CreateSession();

        session.SetUdpEndpoint(" 127.0.0.1 ", port);

        Assert.Equal($"127.0.0.1:{port}", session.UdpEndpoint);
    }

    [Fact]
    public void SetUdpEndpoint_WhenCalledAgain_ReplacesPreviousEndpoint()
    {
        var session = CreateSession();
        session.SetUdpEndpoint("127.0.0.1", 3000);

        session.SetUdpEndpoint("::1", 4000);

        Assert.Equal("::1:4000", session.UdpEndpoint);
    }

    private static Session CreateSession() =>
        new(Guid.NewGuid(), Guid.NewGuid(), CreatedAt);
}
