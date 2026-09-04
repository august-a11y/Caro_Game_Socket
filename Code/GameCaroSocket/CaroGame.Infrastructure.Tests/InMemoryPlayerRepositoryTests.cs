using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Infrastructure.InMemory;

namespace CaroGame.Infrastructure.Tests;

public sealed class InMemoryPlayerRepositoryTests
{
    [Fact]
    public async Task AddAndQueries_AreCaseInsensitiveAndTrimNickname()
    {
        var repository = new InMemoryPlayerRepository();
        var player = new Player("Alice");

        await repository.AddAsync(player);

        Assert.Same(player, await repository.GetByIdAsync(player.PlayerId));
        Assert.Same(player, await repository.GetByNicknameAsync("  ALICE  "));
        Assert.True(await repository.ExistsByNicknameAsync(" alice "));
    }

    [Fact]
    public async Task AddAsync_WhenPlayerIsNull_Throws()
    {
        var repository = new InMemoryPlayerRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WhenIdentifierAlreadyExists_ThrowsWithoutReplacingPlayer()
    {
        var repository = new InMemoryPlayerRepository();
        var player = new Player("Alice");
        await repository.AddAsync(player);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(player));

        Assert.Same(player, await repository.GetByIdAsync(player.PlayerId));
    }

    [Fact]
    public async Task AddAsync_WhenNicknameDiffersOnlyByCase_Throws()
    {
        var repository = new InMemoryPlayerRepository();
        await repository.AddAsync(new Player("Alice"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddAsync(new Player("aLiCe")));
    }

    [Fact]
    public async Task ConcurrentAdds_WithSameNickname_PersistExactlyOnePlayer()
    {
        var repository = new InMemoryPlayerRepository();
        var players = Enumerable.Range(0, 20).Select(_ => new Player("same-name")).ToList();

        var outcomes = await Task.WhenAll(players.Select(player => Task.Run(async () =>
        {
            try
            {
                await repository.AddAsync(player);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));

        Assert.Single(outcomes, success => success);
        Assert.Single(await repository.GetOnlinePlayersAsync());
    }

    [Fact]
    public async Task GetOnlinePlayersAsync_ReturnsFreeAndInMatchButExcludesOffline()
    {
        var repository = new InMemoryPlayerRepository();
        var free = new Player("free");
        var inMatch = new Player("in-match") { Status = PlayerStatus.InMatch };
        var offline = new Player("offline") { Status = PlayerStatus.Offline };
        await repository.AddAsync(free);
        await repository.AddAsync(inMatch);
        await repository.AddAsync(offline);

        var result = await repository.GetOnlinePlayersAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(free, result);
        Assert.Contains(inMatch, result);
        Assert.DoesNotContain(offline, result);
    }

    [Fact]
    public async Task UpdateAsync_WhenPlayerDoesNotExist_Throws()
    {
        var repository = new InMemoryPlayerRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            repository.UpdateAsync(new Player("missing")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NicknameQueries_WhenNicknameIsBlank_Throw(string? nickname)
    {
        var repository = new InMemoryPlayerRepository();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repository.GetByNicknameAsync(nickname!));
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repository.ExistsByNicknameAsync(nickname!));
    }
}
