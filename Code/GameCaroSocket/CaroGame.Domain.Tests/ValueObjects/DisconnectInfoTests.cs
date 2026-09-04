using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Tests.ValueObjects;

public sealed class DisconnectInfoTests
{
    private static readonly DateTime DisconnectedAt =
        new(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidValues_StoresPlayerAndGracePeriod()
    {
        var playerId = Guid.NewGuid();
        var gracePeriodEndsAt = DisconnectedAt.AddMinutes(1);

        var info = new DisconnectInfo(playerId, DisconnectedAt, gracePeriodEndsAt);

        Assert.Equal(playerId, info.PlayerId);
        Assert.Equal(DisconnectedAt, info.DisconnectedAt);
        Assert.Equal(gracePeriodEndsAt, info.GracePeriodEndsAt);
    }

    [Fact]
    public void Constructor_WithEmptyPlayerId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new DisconnectInfo(Guid.Empty, DisconnectedAt, DisconnectedAt.AddMinutes(1)));

        Assert.Equal("playerId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenGracePeriodDoesNotEndAfterDisconnection_ThrowsArgumentException(
        int offsetTicks)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new DisconnectInfo(
                Guid.NewGuid(),
                DisconnectedAt,
                DisconnectedAt.AddTicks(offsetTicks)));

        Assert.Equal("gracePeriodEndsAt", exception.ParamName);
    }
}
