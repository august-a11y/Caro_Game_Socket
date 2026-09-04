using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Domain.Tests.Entities;

public sealed class ChallengeTests
{
    private static readonly DateTime CreatedAt =
        new(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidValues_CreatesPendingChallengeWithDeterministicExpiration()
    {
        var challengerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var expiration = TimeSpan.FromMinutes(5);

        var challenge = new Challenge(challengerId, opponentId, expiration, CreatedAt);

        Assert.NotEqual(Guid.Empty, challenge.ChallengeId);
        Assert.Equal(challengerId, challenge.FromPlayerId);
        Assert.Equal(opponentId, challenge.ToPlayerId);
        Assert.Equal(ChallengeStatus.Pending, challenge.Status);
        Assert.Equal(CreatedAt, challenge.CreatedAt);
        Assert.Equal(CreatedAt.Add(expiration), challenge.ExpiresAt);
    }

    [Fact]
    public void Constructor_CreatesUniqueChallengeIdentifiers()
    {
        var challengerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();

        var first = new Challenge(challengerId, opponentId, TimeSpan.FromMinutes(5), CreatedAt);
        var second = new Challenge(challengerId, opponentId, TimeSpan.FromMinutes(5), CreatedAt);

        Assert.NotEqual(first.ChallengeId, second.ChallengeId);
    }

    [Fact]
    public void Constructor_WithEmptyChallengerId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Challenge(Guid.Empty, Guid.NewGuid(), TimeSpan.FromMinutes(5), CreatedAt));

        Assert.Equal("fromPlayerId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyOpponentId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Challenge(Guid.NewGuid(), Guid.Empty, TimeSpan.FromMinutes(5), CreatedAt));

        Assert.Equal("toPlayerId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithSamePlayer_ThrowsArgumentException()
    {
        var playerId = Guid.NewGuid();

        var exception = Assert.Throws<ArgumentException>(() =>
            new Challenge(playerId, playerId, TimeSpan.FromMinutes(5), CreatedAt));

        Assert.Equal("toPlayerId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveExpiration_ThrowsArgumentOutOfRangeException(int ticks)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Challenge(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromTicks(ticks), CreatedAt));

        Assert.Equal("expiration", exception.ParamName);
    }

    [Fact]
    public void Accept_WhenPending_ChangesStatusToAccepted()
    {
        var challenge = CreateChallenge();

        challenge.Accept();

        Assert.Equal(ChallengeStatus.Accepted, challenge.Status);
    }

    [Fact]
    public void Reject_WhenPending_ChangesStatusToRejected()
    {
        var challenge = CreateChallenge();

        challenge.Reject();

        Assert.Equal(ChallengeStatus.Rejected, challenge.Status);
    }

    [Fact]
    public void Expire_WhenPending_ChangesStatusToExpired()
    {
        var challenge = CreateChallenge();

        challenge.Expire();

        Assert.Equal(ChallengeStatus.Expired, challenge.Status);
    }

    [Theory]
    [InlineData(ChallengeStatus.Accepted)]
    [InlineData(ChallengeStatus.Rejected)]
    [InlineData(ChallengeStatus.Expired)]
    public void Accept_WhenNoLongerPending_ThrowsWithoutChangingStatus(ChallengeStatus terminalStatus)
    {
        var challenge = CreateChallengeInState(terminalStatus);

        Assert.Throws<InvalidOperationException>(challenge.Accept);

        Assert.Equal(terminalStatus, challenge.Status);
    }

    [Theory]
    [InlineData(ChallengeStatus.Accepted)]
    [InlineData(ChallengeStatus.Rejected)]
    [InlineData(ChallengeStatus.Expired)]
    public void Reject_WhenNoLongerPending_ThrowsWithoutChangingStatus(ChallengeStatus terminalStatus)
    {
        var challenge = CreateChallengeInState(terminalStatus);

        Assert.Throws<InvalidOperationException>(challenge.Reject);

        Assert.Equal(terminalStatus, challenge.Status);
    }

    [Theory]
    [InlineData(ChallengeStatus.Accepted)]
    [InlineData(ChallengeStatus.Rejected)]
    [InlineData(ChallengeStatus.Expired)]
    public void Expire_WhenAlreadyTerminal_IsIdempotent(ChallengeStatus terminalStatus)
    {
        var challenge = CreateChallengeInState(terminalStatus);

        challenge.Expire();
        challenge.Expire();

        Assert.Equal(terminalStatus, challenge.Status);
    }

    [Fact]
    public void IsExpired_UsesInclusiveDeadline()
    {
        var challenge = CreateChallenge();

        Assert.False(challenge.IsExpired(challenge.ExpiresAt.AddTicks(-1)));
        Assert.True(challenge.IsExpired(challenge.ExpiresAt));
        Assert.True(challenge.IsExpired(challenge.ExpiresAt.AddTicks(1)));
    }

    private static Challenge CreateChallenge() =>
        new(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(5), CreatedAt);

    private static Challenge CreateChallengeInState(ChallengeStatus status)
    {
        var challenge = CreateChallenge();
        switch (status)
        {
            case ChallengeStatus.Accepted:
                challenge.Accept();
                break;
            case ChallengeStatus.Rejected:
                challenge.Reject();
                break;
            case ChallengeStatus.Expired:
                challenge.Expire();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return challenge;
    }
}
