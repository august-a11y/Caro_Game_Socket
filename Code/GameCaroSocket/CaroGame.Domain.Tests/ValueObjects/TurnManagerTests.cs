using CaroGame.Domain.ValueObjects;

namespace CaroGame.Domain.Tests.ValueObjects;

public sealed class TurnManagerTests
{
    private static readonly DateTime StartTime =
        new(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidValues_InitializesTurnAndDeadline()
    {
        var playerId = Guid.NewGuid();

        var manager = new TurnManager(playerId, 30, StartTime);

        Assert.Equal(playerId, manager.CurrentTurnPlayerId);
        Assert.Equal(30, manager.DurationInSeconds);
        Assert.Equal(StartTime, manager.TurnStartedAt);
        Assert.Equal(StartTime.AddSeconds(30), manager.TurnDeadline);
        Assert.False(manager.IsPaused);
    }

    [Fact]
    public void Constructor_WithEmptyStartingPlayer_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new TurnManager(Guid.Empty, 30, StartTime));

        Assert.Equal("startingPlayerId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Constructor_WithNonPositiveDuration_ThrowsArgumentOutOfRangeException(int duration)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TurnManager(Guid.NewGuid(), duration, StartTime));

        Assert.Equal("durationInSeconds", exception.ParamName);
    }

    [Fact]
    public void IsTimeUp_UsesInclusiveDeadline()
    {
        var manager = CreateManager();

        Assert.False(manager.IsTimeUp(manager.TurnDeadline.AddTicks(-1)));
        Assert.True(manager.IsTimeUp(manager.TurnDeadline));
        Assert.True(manager.IsTimeUp(manager.TurnDeadline.AddTicks(1)));
    }

    [Fact]
    public void SwitchTurn_ChangesPlayerAndResetsFullDeadline()
    {
        var manager = CreateManager();
        var nextPlayerId = Guid.NewGuid();
        var switchedAt = StartTime.AddSeconds(12);

        manager.SwitchTurn(nextPlayerId, switchedAt);

        Assert.Equal(nextPlayerId, manager.CurrentTurnPlayerId);
        Assert.Equal(switchedAt, manager.TurnStartedAt);
        Assert.Equal(switchedAt.AddSeconds(30), manager.TurnDeadline);
        Assert.False(manager.IsPaused);
    }

    [Fact]
    public void SwitchTurn_WithEmptyPlayer_ThrowsWithoutChangingCurrentTurn()
    {
        var manager = CreateManager();
        var originalPlayer = manager.CurrentTurnPlayerId;
        var originalStartedAt = manager.TurnStartedAt;
        var originalDeadline = manager.TurnDeadline;

        var exception = Assert.Throws<ArgumentException>(() =>
            manager.SwitchTurn(Guid.Empty, StartTime.AddSeconds(10)));

        Assert.Equal("nextPlayerId", exception.ParamName);
        Assert.Equal(originalPlayer, manager.CurrentTurnPlayerId);
        Assert.Equal(originalStartedAt, manager.TurnStartedAt);
        Assert.Equal(originalDeadline, manager.TurnDeadline);
    }

    [Fact]
    public void Pause_DuringTurn_PreservesRemainingTimeAndDisablesTimeout()
    {
        var manager = CreateManager();

        manager.Pause(StartTime.AddSeconds(10));

        Assert.True(manager.IsPaused);
        Assert.False(manager.IsTimeUp(StartTime.AddHours(1)));

        manager.Resume(StartTime.AddMinutes(5));

        Assert.Equal(StartTime.AddMinutes(5), manager.TurnStartedAt);
        Assert.Equal(StartTime.AddMinutes(5).AddSeconds(20), manager.TurnDeadline);
        Assert.False(manager.IsPaused);
    }

    [Fact]
    public void Pause_BeforeTurnStarted_ThrowsWithoutPausing()
    {
        var manager = CreateManager();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            manager.Pause(StartTime.AddTicks(-1)));

        Assert.Equal("currentTime", exception.ParamName);
        Assert.False(manager.IsPaused);
        Assert.Equal(StartTime.AddSeconds(30), manager.TurnDeadline);
    }

    [Fact]
    public void Pause_WhenAlreadyPaused_IsIdempotentAndDoesNotRecalculateRemainingTime()
    {
        var manager = CreateManager();
        manager.Pause(StartTime.AddSeconds(10));

        manager.Pause(StartTime.AddSeconds(25));
        manager.Resume(StartTime.AddMinutes(1));

        Assert.Equal(StartTime.AddMinutes(1).AddSeconds(20), manager.TurnDeadline);
    }

    [Fact]
    public void Pause_AfterDeadline_StoresZeroRemainingTime()
    {
        var manager = CreateManager();
        manager.Pause(StartTime.AddSeconds(31));

        Assert.True(manager.IsPaused);
        Assert.False(manager.IsTimeUp(StartTime.AddHours(1)));

        var resumedAt = StartTime.AddMinutes(2);
        manager.Resume(resumedAt);

        Assert.Equal(resumedAt, manager.TurnDeadline);
        Assert.True(manager.IsTimeUp(resumedAt));
    }

    [Fact]
    public void Resume_WhenNotPaused_IsIdempotent()
    {
        var manager = CreateManager();
        var originalStartedAt = manager.TurnStartedAt;
        var originalDeadline = manager.TurnDeadline;

        manager.Resume(StartTime.AddMinutes(1));

        Assert.False(manager.IsPaused);
        Assert.Equal(originalStartedAt, manager.TurnStartedAt);
        Assert.Equal(originalDeadline, manager.TurnDeadline);
    }

    [Fact]
    public void SwitchTurn_WhenPaused_DiscardsPauseAndStartsFullTurnForNextPlayer()
    {
        var manager = CreateManager();
        manager.Pause(StartTime.AddSeconds(10));
        var nextPlayerId = Guid.NewGuid();
        var switchedAt = StartTime.AddMinutes(1);

        manager.SwitchTurn(nextPlayerId, switchedAt);

        Assert.False(manager.IsPaused);
        Assert.Equal(nextPlayerId, manager.CurrentTurnPlayerId);
        Assert.Equal(switchedAt, manager.TurnStartedAt);
        Assert.Equal(switchedAt.AddSeconds(30), manager.TurnDeadline);
    }

    private static TurnManager CreateManager() =>
        new(Guid.NewGuid(), 30, StartTime);
}
