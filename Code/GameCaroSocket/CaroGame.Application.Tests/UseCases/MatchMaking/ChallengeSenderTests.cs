using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.MatchMaking;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.MatchMaking;

public sealed class ChallengeSenderTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullPlayerRepository_ThrowsArgumentNullException()
    {
        var challenges = new Mock<IChallengeRepository>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ChallengeSender(null!, challenges, new FixedTimeProvider(Now)));
    }

    [Fact]
    public void Constructor_WithNullChallengeRepository_ThrowsArgumentNullException()
    {
        var players = new Mock<IPlayerRepository>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ChallengeSender(players, null!, new FixedTimeProvider(Now)));
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var players = new Mock<IPlayerRepository>().Object;
        var challenges = new Mock<IChallengeRepository>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ChallengeSender(players, challenges, null!));
    }

    [Fact]
    public async Task SendChallengeAsync_WithInvalidEmptyOrSelfIdentifiers_ReturnsFalseWithoutRepositoryCalls()
    {
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var sender = new ChallengeSender(
            players.Object,
            challenges.Object,
            new FixedTimeProvider(Now));
        var playerId = Guid.NewGuid();
        var validId = playerId.ToString("D");
        var invalidInputs = new (string? Challenger, string? Opponent)[]
        {
            (null, validId),
            (validId, null),
            (string.Empty, validId),
            (validId, "not-a-guid"),
            (Guid.Empty.ToString(), validId),
            (validId, Guid.Empty.ToString()),
            (playerId.ToString("D"), playerId.ToString("B"))
        };

        foreach (var (challenger, opponent) in invalidInputs)
        {
            var result = await sender.SendChallengeAsync(challenger!, opponent!);
            Assert.False(result);
        }

        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task SendChallengeAsync_WhenChallengerDoesNotExist_ReturnsFalse()
    {
        var challengerId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(challengerId))
            .ReturnsAsync((Player?)null);
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challengerId.ToString(),
            opponentId.ToString());

        Assert.False(result);
        players.Verify(repository => repository.GetByIdAsync(opponentId), Times.Never);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Theory]
    [InlineData(PlayerStatus.Offline)]
    [InlineData(PlayerStatus.InMatch)]
    public async Task SendChallengeAsync_WhenChallengerIsNotFree_ReturnsFalse(PlayerStatus status)
    {
        var challenger = new Player("challenger") { Status = status };
        var opponent = new Player("opponent");
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(challenger.PlayerId))
            .ReturnsAsync(challenger);
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.False(result);
        players.Verify(repository => repository.GetByIdAsync(opponent.PlayerId), Times.Never);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task SendChallengeAsync_WhenOpponentDoesNotExist_ReturnsFalse()
    {
        var challenger = new Player("challenger");
        var opponentId = Guid.NewGuid();
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(challenger.PlayerId))
            .ReturnsAsync(challenger);
        players.Setup(repository => repository.GetByIdAsync(opponentId))
            .ReturnsAsync((Player?)null);
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponentId.ToString());

        Assert.False(result);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Theory]
    [InlineData(PlayerStatus.Offline)]
    [InlineData(PlayerStatus.InMatch)]
    public async Task SendChallengeAsync_WhenOpponentIsNotFree_ReturnsFalse(PlayerStatus status)
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent") { Status = status };
        var players = CreatePlayerRepository(challenger, opponent);
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.False(result);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task SendChallengeAsync_WhenBothPlayersAreFree_PersistsDeterministicFiveMinuteChallenge()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var players = CreatePlayerRepository(challenger, opponent);
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        SetupNoPendingChallenges(challenges, challenger.PlayerId, opponent.PlayerId);
        Challenge? persisted = null;
        challenges.Setup(repository => repository.AddAsync(It.IsAny<Challenge>()))
            .Callback<Challenge>(challenge => persisted = challenge)
            .Returns(Task.CompletedTask);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.True(result);
        Assert.NotNull(persisted);
        Assert.Equal(challenger.PlayerId, persisted.FromPlayerId);
        Assert.Equal(opponent.PlayerId, persisted.ToPlayerId);
        Assert.Equal(ChallengeStatus.Pending, persisted.Status);
        Assert.Equal(Now.UtcDateTime, persisted.CreatedAt);
        Assert.Equal(Now.UtcDateTime.AddMinutes(5), persisted.ExpiresAt);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Once);
    }

    [Fact]
    public async Task SendChallengeAsync_WhenActiveDirectChallengeExists_ReturnsFalse()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var duplicate = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var players = CreatePlayerRepository(challenger, opponent);
        var challenges = CreatePendingChallengeRepository(
            challenger.PlayerId,
            opponent.PlayerId,
            [duplicate],
            []);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.False(result);
        Assert.Equal(ChallengeStatus.Pending, duplicate.Status);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
        challenges.Verify(repository => repository.UpdateAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task SendChallengeAsync_WhenActiveReverseChallengeExists_ReturnsFalse()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var reverse = NewChallenge(opponent.PlayerId, challenger.PlayerId, Now.AddMinutes(-1));
        var players = CreatePlayerRepository(challenger, opponent);
        var challenges = CreatePendingChallengeRepository(
            challenger.PlayerId,
            opponent.PlayerId,
            [],
            [reverse]);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.False(result);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task SendChallengeAsync_WhenPreviousPairChallengeExpired_ExpiresItAndCreatesReplacement()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var expired = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-6));
        var players = CreatePlayerRepository(challenger, opponent);
        var challenges = CreatePendingChallengeRepository(
            challenger.PlayerId,
            opponent.PlayerId,
            [expired],
            []);
        challenges.Setup(repository => repository.UpdateAsync(expired)).Returns(Task.CompletedTask);
        Challenge? replacement = null;
        challenges.Setup(repository => repository.AddAsync(It.IsAny<Challenge>()))
            .Callback<Challenge>(challenge => replacement = challenge)
            .Returns(Task.CompletedTask);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.True(result);
        Assert.Equal(ChallengeStatus.Expired, expired.Status);
        Assert.NotNull(replacement);
        Assert.NotEqual(expired.ChallengeId, replacement.ChallengeId);
        Assert.Equal(Now.UtcDateTime, replacement.CreatedAt);
        challenges.Verify(repository => repository.UpdateAsync(expired), Times.Once);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Once);
    }

    [Fact]
    public async Task SendChallengeAsync_WhenExpiredAndActiveDuplicatesExist_ExpiresStaleOneAndDoesNotAddAnother()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var expired = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-6));
        var active = NewChallenge(opponent.PlayerId, challenger.PlayerId, Now.AddMinutes(-1));
        var players = CreatePlayerRepository(challenger, opponent);
        var challenges = CreatePendingChallengeRepository(
            challenger.PlayerId,
            opponent.PlayerId,
            [expired],
            [active]);
        challenges.Setup(repository => repository.UpdateAsync(expired)).Returns(Task.CompletedTask);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.False(result);
        Assert.Equal(ChallengeStatus.Expired, expired.Status);
        Assert.Equal(ChallengeStatus.Pending, active.Status);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task SendChallengeAsync_UnrelatedPendingChallengesDoNotBlockNewChallenge()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var thirdPlayer = new Player("third");
        var unrelated = NewChallenge(thirdPlayer.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var players = CreatePlayerRepository(challenger, opponent);
        var challenges = CreatePendingChallengeRepository(
            challenger.PlayerId,
            opponent.PlayerId,
            [unrelated],
            []);
        challenges.Setup(repository => repository.AddAsync(It.IsAny<Challenge>()))
            .Returns(Task.CompletedTask);
        var sender = CreateSender(players, challenges);

        var result = await sender.SendChallengeAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString());

        Assert.True(result);
        Assert.Equal(ChallengeStatus.Pending, unrelated.Status);
        challenges.Verify(repository => repository.UpdateAsync(unrelated), Times.Never);
    }

    [Fact]
    public async Task SendChallengeAsync_WithPreCancelledToken_ThrowsWithoutRepositoryCalls()
    {
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var sender = CreateSender(players, challenges);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sender.SendChallengeAsync(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                cancellation.Token));

        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        challenges.Verify(repository => repository.AddAsync(It.IsAny<Challenge>()), Times.Never);
    }

    private static ChallengeSender CreateSender(
        Mock<IPlayerRepository> players,
        Mock<IChallengeRepository> challenges) =>
        new(players.Object, challenges.Object, new FixedTimeProvider(Now));

    private static Mock<IPlayerRepository> CreatePlayerRepository(Player challenger, Player opponent)
    {
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetByIdAsync(challenger.PlayerId))
            .ReturnsAsync(challenger);
        repository.Setup(candidate => candidate.GetByIdAsync(opponent.PlayerId))
            .ReturnsAsync(opponent);
        return repository;
    }

    private static void SetupNoPendingChallenges(
        Mock<IChallengeRepository> repository,
        Guid challengerId,
        Guid opponentId)
    {
        repository.Setup(candidate => candidate.GetPendingForPlayerAsync(opponentId))
            .ReturnsAsync(Array.Empty<Challenge>());
        repository.Setup(candidate => candidate.GetPendingForPlayerAsync(challengerId))
            .ReturnsAsync(Array.Empty<Challenge>());
    }

    private static Mock<IChallengeRepository> CreatePendingChallengeRepository(
        Guid challengerId,
        Guid opponentId,
        IReadOnlyList<Challenge> toOpponent,
        IReadOnlyList<Challenge> toChallenger)
    {
        var repository = new Mock<IChallengeRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetPendingForPlayerAsync(opponentId))
            .ReturnsAsync(toOpponent);
        repository.Setup(candidate => candidate.GetPendingForPlayerAsync(challengerId))
            .ReturnsAsync(toChallenger);
        return repository;
    }

    private static Challenge NewChallenge(Guid from, Guid to, DateTimeOffset createdAt) =>
        new(from, to, TimeSpan.FromMinutes(5), createdAt.UtcDateTime);
}
