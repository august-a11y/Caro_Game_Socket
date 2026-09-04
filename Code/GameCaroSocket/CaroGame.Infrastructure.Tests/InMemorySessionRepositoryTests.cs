using CaroGame.Domain.Entities;
using CaroGame.Infrastructure.InMemory;

namespace CaroGame.Infrastructure.Tests;

public sealed class InMemorySessionRepositoryTests
{
    [Fact]
    public async Task AddAndQueries_ReturnSameSessionByTokenAndPlayer()
    {
        var repository = new InMemorySessionRepository();
        var session = new Session(Guid.NewGuid(), Guid.NewGuid());

        await repository.AddAsync(session);

        Assert.True(await repository.ExistsAsync(session.PlayerId));
        Assert.Same(session, await repository.GetByIdAsync(session.SessionId));
        Assert.Same(session, await repository.GetByPlayerIdAsync(session.PlayerId));
    }

    [Fact]
    public async Task AddAsync_WhenSessionIsNull_Throws()
    {
        var repository = new InMemorySessionRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WhenTokenAlreadyExists_Throws()
    {
        var repository = new InMemorySessionRepository();
        var token = Guid.NewGuid();
        await repository.AddAsync(new Session(Guid.NewGuid(), token));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddAsync(new Session(Guid.NewGuid(), token)));
    }

    [Fact]
    public async Task AddAsync_WhenPlayerAlreadyHasSession_ThrowsAndKeepsOldToken()
    {
        var repository = new InMemorySessionRepository();
        var playerId = Guid.NewGuid();
        var original = new Session(playerId, Guid.NewGuid());
        var replacement = new Session(playerId, Guid.NewGuid());
        await repository.AddAsync(original);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(replacement));

        Assert.Same(original, await repository.GetByPlayerIdAsync(playerId));
        Assert.Null(await repository.GetByIdAsync(replacement.SessionId));
    }

    [Fact]
    public async Task ConcurrentAdds_ForSamePlayer_PersistExactlyOneSession()
    {
        var repository = new InMemorySessionRepository();
        var playerId = Guid.NewGuid();
        var sessions = Enumerable.Range(0, 20)
            .Select(_ => new Session(playerId, Guid.NewGuid()))
            .ToList();

        var outcomes = await Task.WhenAll(sessions.Select(session => Task.Run(async () =>
        {
            try
            {
                await repository.AddAsync(session);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));

        Assert.Single(outcomes, success => success);
        var persisted = Assert.IsType<Session>(await repository.GetByPlayerIdAsync(playerId));
        Assert.Contains(persisted, sessions);
    }

    [Fact]
    public async Task DisconnectedSession_RemainsAvailableByOldTokenAfterUpdate()
    {
        var repository = new InMemorySessionRepository();
        var session = new Session(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(session);
        session.MarkDisconnected();

        await repository.UpdateAsync(session);

        var persisted = Assert.IsType<Session>(await repository.GetByIdAsync(session.SessionId));
        Assert.Same(session, persisted);
        Assert.False(persisted.IsConnected);
    }

    [Fact]
    public async Task UpdateAsync_WhenSessionDoesNotExist_Throws()
    {
        var repository = new InMemorySessionRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            repository.UpdateAsync(new Session(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task RemoveAsync_IsIdempotentAndRemovesSessionByPlayer()
    {
        var repository = new InMemorySessionRepository();
        var session = new Session(Guid.NewGuid(), Guid.NewGuid());
        await repository.AddAsync(session);

        await repository.RemoveAsync(session.PlayerId);
        await repository.RemoveAsync(session.PlayerId);

        Assert.False(await repository.ExistsAsync(session.PlayerId));
        Assert.Null(await repository.GetByIdAsync(session.SessionId));
    }
}
