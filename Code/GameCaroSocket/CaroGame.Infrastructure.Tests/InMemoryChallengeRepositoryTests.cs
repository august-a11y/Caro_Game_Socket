using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Infrastructure.InMemory;

namespace CaroGame.Infrastructure.Tests;

public sealed class InMemoryChallengeRepositoryTests
{
    [Fact]
    public async Task AddAndQueries_ReturnPendingChallengeOnlyForRecipient()
    {
        var repository = new InMemoryChallengeRepository();
        var challenge = CreateChallenge(Guid.NewGuid(), Guid.NewGuid());

        await repository.AddAsync(challenge);

        Assert.Same(challenge, await repository.GetByIdAsync(challenge.ChallengeId));
        Assert.Contains(challenge, await repository.GetPendingForPlayerAsync(challenge.ToPlayerId));
        Assert.Empty(await repository.GetPendingForPlayerAsync(challenge.FromPlayerId));
    }

    [Fact]
    public async Task AddAsync_WhenChallengeIsNull_Throws()
    {
        var repository = new InMemoryChallengeRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WhenIdentifierAlreadyExists_Throws()
    {
        var repository = new InMemoryChallengeRepository();
        var challenge = CreateChallenge(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(challenge);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(challenge));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAsync_WhenPendingPairAlreadyExists_RejectsBothDirections(bool reverse)
    {
        var repository = new InMemoryChallengeRepository();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var original = CreateChallenge(firstId, secondId);
        var duplicate = reverse
            ? CreateChallenge(secondId, firstId)
            : CreateChallenge(firstId, secondId);
        await repository.AddAsync(original);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(duplicate));

        Assert.Null(await repository.GetByIdAsync(duplicate.ChallengeId));
    }

    [Fact]
    public async Task ConcurrentAdds_ForSamePair_PersistExactlyOnePendingChallenge()
    {
        var repository = new InMemoryChallengeRepository();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var challenges = Enumerable.Range(0, 20)
            .Select(index => index % 2 == 0
                ? CreateChallenge(firstId, secondId)
                : CreateChallenge(secondId, firstId))
            .ToList();

        var outcomes = await Task.WhenAll(challenges.Select(challenge => Task.Run(async () =>
        {
            try
            {
                await repository.AddAsync(challenge);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));

        Assert.Single(outcomes, success => success);
        var incomingFirst = await repository.GetPendingForPlayerAsync(firstId);
        var incomingSecond = await repository.GetPendingForPlayerAsync(secondId);
        Assert.Single(incomingFirst.Concat(incomingSecond));
    }

    [Theory]
    [InlineData(ChallengeStatus.Accepted)]
    [InlineData(ChallengeStatus.Rejected)]
    [InlineData(ChallengeStatus.Expired)]
    public async Task AddAsync_WhenPreviousPairChallengeIsFinal_AllowsNewPendingChallenge(
        ChallengeStatus finalStatus)
    {
        var repository = new InMemoryChallengeRepository();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var previous = CreateChallenge(firstId, secondId);
        await repository.AddAsync(previous);
        SetStatus(previous, finalStatus);
        await repository.UpdateAsync(previous);
        var next = CreateChallenge(secondId, firstId);

        await repository.AddAsync(next);

        Assert.Same(next, await repository.GetByIdAsync(next.ChallengeId));
    }

    [Fact]
    public async Task UpdateAsync_WhenChallengeDoesNotExist_Throws()
    {
        var repository = new InMemoryChallengeRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            repository.UpdateAsync(CreateChallenge(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task RemoveAsync_IsIdempotent()
    {
        var repository = new InMemoryChallengeRepository();
        var challenge = CreateChallenge(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(challenge);

        await repository.RemoveAsync(challenge.ChallengeId);
        await repository.RemoveAsync(challenge.ChallengeId);

        Assert.Null(await repository.GetByIdAsync(challenge.ChallengeId));
    }

    private static Challenge CreateChallenge(Guid fromPlayerId, Guid toPlayerId) =>
        new(fromPlayerId, toPlayerId, TimeSpan.FromMinutes(5));

    private static void SetStatus(Challenge challenge, ChallengeStatus status)
    {
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
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
