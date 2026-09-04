using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.MatchMaking;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;
using Xunit;

namespace CaroGame.Application.Tests.UseCases.MatchMaking;

public sealed class ChallengeResponderTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithNullChallengeRepository_ThrowsArgumentNullException()
    {
        var players = new Mock<IPlayerRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ChallengeResponder(null!, players, rooms, new FixedTimeProvider(Now)));
    }

    [Fact]
    public void Constructor_WithNullPlayerRepository_ThrowsArgumentNullException()
    {
        var challenges = new Mock<IChallengeRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ChallengeResponder(challenges, null!, rooms, new FixedTimeProvider(Now)));
    }

    [Fact]
    public void Constructor_WithNullRoomRepository_ThrowsArgumentNullException()
    {
        var challenges = new Mock<IChallengeRepository>().Object;
        var players = new Mock<IPlayerRepository>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ChallengeResponder(challenges, players, null!, new FixedTimeProvider(Now)));
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var challenges = new Mock<IChallengeRepository>().Object;
        var players = new Mock<IPlayerRepository>().Object;
        var rooms = new Mock<IRoomRepository>().Object;

        Assert.Throws<ArgumentNullException>(() =>
            new ChallengeResponder(challenges, players, rooms, null!));
    }

    [Fact]
    public async Task RespondAsync_WithInvalidEmptyOrSelfIdentifiers_ReturnsNullWithoutRepositoryCalls()
    {
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);
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
            var result = await responder.RespondAsync(challenger!, opponent!, accept: true);
            Assert.Null(result);
        }

        challenges.Verify(
            repository => repository.GetPendingForPlayerAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task RespondAsync_WhenNoMatchingIncomingChallengeExists_ReturnsNullWithoutUpdates()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var anotherChallenger = new Player("another");
        var unrelated = NewChallenge(anotherChallenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        challenges.Setup(repository => repository.GetPendingForPlayerAsync(opponent.PlayerId))
            .ReturnsAsync([unrelated]);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: true);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Pending, unrelated.Status);
        challenges.Verify(repository => repository.UpdateAsync(It.IsAny<Challenge>()), Times.Never);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task RespondAsync_WhenChallengeHasExpired_MarksItExpiredAndStops()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var challenge = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-6));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        challenges.Setup(repository => repository.UpdateAsync(challenge)).Returns(Task.CompletedTask);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: true);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Expired, challenge.Status);
        challenges.Verify(repository => repository.UpdateAsync(challenge), Times.Once);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task RespondAsync_WhenChallengeExpiresExactlyNow_MarksItExpired()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var challenge = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-5));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        challenges.Setup(repository => repository.UpdateAsync(challenge)).Returns(Task.CompletedTask);
        var responder = CreateResponder(
            challenges,
            new Mock<IPlayerRepository>(MockBehavior.Strict),
            new Mock<IRoomRepository>(MockBehavior.Strict));

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: false);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Expired, challenge.Status);
    }

    [Fact]
    public async Task RespondAsync_WhenRejected_UpdatesOnlyTheChallenge()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var challenge = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        challenges.Setup(repository => repository.UpdateAsync(challenge)).Returns(Task.CompletedTask);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: false);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Rejected, challenge.Status);
        challenges.Verify(repository => repository.UpdateAsync(challenge), Times.Once);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task RespondAsync_WhenChallengerDoesNotExist_DoesNotAcceptChallenge()
    {
        var challengerId = Guid.NewGuid();
        var opponent = new Player("opponent");
        var challenge = NewChallenge(challengerId, opponent.PlayerId, Now.AddMinutes(-1));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(challengerId))
            .ReturnsAsync((Player?)null);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challengerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: true);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Pending, challenge.Status);
        players.Verify(repository => repository.GetByIdAsync(opponent.PlayerId), Times.Never);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
        challenges.Verify(repository => repository.UpdateAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Theory]
    [InlineData(PlayerStatus.Offline)]
    [InlineData(PlayerStatus.InMatch)]
    public async Task RespondAsync_WhenChallengerIsNotFree_DoesNotAcceptChallenge(PlayerStatus status)
    {
        var challenger = new Player("challenger") { Status = status };
        var opponent = new Player("opponent");
        var challenge = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(challenger.PlayerId))
            .ReturnsAsync(challenger);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: true);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Pending, challenge.Status);
        players.Verify(repository => repository.GetByIdAsync(opponent.PlayerId), Times.Never);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Fact]
    public async Task RespondAsync_WhenOpponentDoesNotExist_DoesNotAcceptChallenge()
    {
        var challenger = new Player("challenger");
        var opponentId = Guid.NewGuid();
        var challenge = NewChallenge(challenger.PlayerId, opponentId, Now.AddMinutes(-1));
        var challenges = CreateRepositoryWithIncomingChallenge(opponentId, challenge);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(challenger.PlayerId))
            .ReturnsAsync(challenger);
        players.Setup(repository => repository.GetByIdAsync(opponentId))
            .ReturnsAsync((Player?)null);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponentId.ToString(),
            accept: true);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Pending, challenge.Status);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    [Theory]
    [InlineData(PlayerStatus.Offline)]
    [InlineData(PlayerStatus.InMatch)]
    public async Task RespondAsync_WhenOpponentIsNotFree_DoesNotAcceptChallenge(PlayerStatus status)
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent") { Status = status };
        var challenge = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        var players = CreatePlayerRepository(challenger, opponent);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: true);

        Assert.Null(result);
        Assert.Equal(ChallengeStatus.Pending, challenge.Status);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
        challenges.Verify(repository => repository.UpdateAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task RespondAsync_WhenAccepted_CreatesWaitingRoomAndMarksBothPlayersInMatch()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var challenge = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        challenges.Setup(repository => repository.UpdateAsync(challenge)).Returns(Task.CompletedTask);
        var players = CreatePlayerRepository(challenger, opponent);
        players.Setup(repository => repository.UpdateAsync(challenger)).Returns(Task.CompletedTask);
        players.Setup(repository => repository.UpdateAsync(opponent)).Returns(Task.CompletedTask);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        Room? persistedRoom = null;
        rooms.Setup(repository => repository.AddAsync(It.IsAny<Room>()))
            .Callback<Room>(room => persistedRoom = room)
            .Returns(Task.CompletedTask);
        var responder = CreateResponder(challenges, players, rooms);

        var result = await responder.RespondAsync(
            challenger.PlayerId.ToString(),
            opponent.PlayerId.ToString(),
            accept: true);

        Assert.NotNull(result);
        Assert.NotNull(persistedRoom);
        Assert.True(Guid.TryParse(result, out var returnedRoomId));
        Assert.Equal(persistedRoom.RoomId, returnedRoomId);
        Assert.Equal(RoomStatus.Waiting, persistedRoom.Status);
        Assert.Null(persistedRoom.CurrentMatch);
        Assert.Equal(Now.UtcDateTime, persistedRoom.CreatedAt);
        Assert.Equal(challenger.PlayerId, persistedRoom.PlayerX.PlayerId);
        Assert.Equal(Symbol.X, persistedRoom.PlayerX.Symbol);
        Assert.Equal(opponent.PlayerId, persistedRoom.PlayerO.PlayerId);
        Assert.Equal(Symbol.O, persistedRoom.PlayerO.Symbol);
        Assert.Equal(PlayerStatus.InMatch, challenger.Status);
        Assert.Equal(PlayerStatus.InMatch, opponent.Status);
        Assert.Equal(ChallengeStatus.Accepted, challenge.Status);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Once);
        players.Verify(repository => repository.UpdateAsync(challenger), Times.Once);
        players.Verify(repository => repository.UpdateAsync(opponent), Times.Once);
        challenges.Verify(repository => repository.UpdateAsync(challenge), Times.Once);
    }

    [Fact]
    public async Task RespondAsync_WhenRoomCannotBePersisted_DoesNotMutatePlayersOrChallenge()
    {
        var challenger = new Player("challenger");
        var opponent = new Player("opponent");
        var challenge = NewChallenge(challenger.PlayerId, opponent.PlayerId, Now.AddMinutes(-1));
        var challenges = CreateRepositoryWithIncomingChallenge(opponent.PlayerId, challenge);
        var players = CreatePlayerRepository(challenger, opponent);
        var expected = new InvalidOperationException("room persistence failed");
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.AddAsync(It.IsAny<Room>())).ThrowsAsync(expected);
        var responder = CreateResponder(challenges, players, rooms);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            responder.RespondAsync(
                challenger.PlayerId.ToString(),
                opponent.PlayerId.ToString(),
                accept: true));

        Assert.Same(expected, actual);
        Assert.Equal(PlayerStatus.Free, challenger.Status);
        Assert.Equal(PlayerStatus.Free, opponent.Status);
        Assert.Equal(ChallengeStatus.Pending, challenge.Status);
        players.Verify(repository => repository.UpdateAsync(It.IsAny<Player>()), Times.Never);
        challenges.Verify(repository => repository.UpdateAsync(It.IsAny<Challenge>()), Times.Never);
    }

    [Fact]
    public async Task RespondAsync_WithPreCancelledToken_ThrowsWithoutRepositoryCalls()
    {
        var challenges = new Mock<IChallengeRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var responder = CreateResponder(challenges, players, rooms);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            responder.RespondAsync(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                accept: true,
                cancellation.Token));

        challenges.Verify(
            repository => repository.GetPendingForPlayerAsync(It.IsAny<Guid>()),
            Times.Never);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        rooms.Verify(repository => repository.AddAsync(It.IsAny<Room>()), Times.Never);
    }

    private static ChallengeResponder CreateResponder(
        Mock<IChallengeRepository> challenges,
        Mock<IPlayerRepository> players,
        Mock<IRoomRepository> rooms) =>
        new(
            challenges.Object,
            players.Object,
            rooms.Object,
            new FixedTimeProvider(Now));

    private static Mock<IChallengeRepository> CreateRepositoryWithIncomingChallenge(
        Guid opponentId,
        Challenge challenge)
    {
        var repository = new Mock<IChallengeRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetPendingForPlayerAsync(opponentId))
            .ReturnsAsync([challenge]);
        return repository;
    }

    private static Mock<IPlayerRepository> CreatePlayerRepository(Player challenger, Player opponent)
    {
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetByIdAsync(challenger.PlayerId))
            .ReturnsAsync(challenger);
        repository.Setup(candidate => candidate.GetByIdAsync(opponent.PlayerId))
            .ReturnsAsync(opponent);
        return repository;
    }

    private static Challenge NewChallenge(Guid from, Guid to, DateTimeOffset createdAt) =>
        new(from, to, TimeSpan.FromMinutes(5), createdAt.UtcDateTime);
}
