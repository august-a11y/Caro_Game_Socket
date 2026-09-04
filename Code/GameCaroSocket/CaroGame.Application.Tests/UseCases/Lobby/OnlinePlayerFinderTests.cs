using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.Lobby;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;

namespace CaroGame.Application.Tests.UseCases.Lobby;

public sealed class OnlinePlayerFinderTests
{
    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OnlinePlayerFinder(null!));
    }

    [Fact]
    public async Task FindOnlinePlayersAsync_WhenRepositoryIsEmpty_ReturnsEmptyList()
    {
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetOnlinePlayersAsync())
            .ReturnsAsync(Array.Empty<Player>());
        var finder = new OnlinePlayerFinder(repository.Object);

        var result = await finder.FindOnlinePlayersAsync(CancellationToken.None);

        Assert.Empty(result);
        repository.Verify(candidate => candidate.GetOnlinePlayersAsync(), Times.Once);
    }

    [Fact]
    public async Task FindOnlinePlayersAsync_MapsIdentityNicknameAndActualStatusInRepositoryOrder()
    {
        var freePlayer = new Player("free-player");
        var playingPlayer = new Player("playing-player")
        {
            Status = PlayerStatus.InMatch
        };
        IReadOnlyList<Player> players = [freePlayer, playingPlayer];

        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetOnlinePlayersAsync())
            .ReturnsAsync(players);
        var finder = new OnlinePlayerFinder(repository.Object);

        var result = await finder.FindOnlinePlayersAsync(CancellationToken.None);

        Assert.Collection(
            result,
            player =>
            {
                Assert.Equal(freePlayer.PlayerId, player.UserId);
                Assert.Equal(freePlayer.Nickname, player.Nickname);
                Assert.Equal(PlayerStatus.Free, player.Status);
            },
            player =>
            {
                Assert.Equal(playingPlayer.PlayerId, player.UserId);
                Assert.Equal(playingPlayer.Nickname, player.Nickname);
                Assert.Equal(PlayerStatus.InMatch, player.Status);
            });
    }

    [Fact]
    public async Task FindOnlinePlayersAsync_WithPreCancelledToken_ThrowsWithoutQueryingRepository()
    {
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var finder = new OnlinePlayerFinder(repository.Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            finder.FindOnlinePlayersAsync(cancellation.Token));

        repository.Verify(candidate => candidate.GetOnlinePlayersAsync(), Times.Never);
    }

    [Fact]
    public async Task FindOnlinePlayersAsync_WhenCancelledDuringQuery_ThrowsAfterQueryCompletes()
    {
        var queryCompletion = new TaskCompletionSource<IReadOnlyList<Player>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetOnlinePlayersAsync())
            .Returns(queryCompletion.Task);
        var finder = new OnlinePlayerFinder(repository.Object);
        using var cancellation = new CancellationTokenSource();

        var operation = finder.FindOnlinePlayersAsync(cancellation.Token);
        await cancellation.CancelAsync();
        queryCompletion.SetResult(Array.Empty<Player>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

    [Fact]
    public async Task FindOnlinePlayersAsync_WhenRepositoryFails_PropagatesException()
    {
        var expected = new InvalidOperationException("repository failure");
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetOnlinePlayersAsync())
            .ThrowsAsync(expected);
        var finder = new OnlinePlayerFinder(repository.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finder.FindOnlinePlayersAsync(CancellationToken.None));

        Assert.Same(expected, actual);
    }
}
