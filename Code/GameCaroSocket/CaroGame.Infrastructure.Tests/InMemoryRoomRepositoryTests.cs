using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Infrastructure.InMemory;

namespace CaroGame.Infrastructure.Tests;

public sealed class InMemoryRoomRepositoryTests
{
    private static readonly DateTime StartTime =
        new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetOngoingRoomsAsync_ReturnsWaitingAndPlayingButExcludesFinished()
    {
        var repository = new InMemoryRoomRepository();
        var waiting = CreateRoom();
        var playing = CreateRoom();
        var finished = CreateRoom();
        Start(playing);
        Start(finished);
        finished.EndMatch(MatchResultType.Draw);
        await repository.AddAsync(waiting);
        await repository.AddAsync(playing);
        await repository.AddAsync(finished);

        var result = await repository.GetOngoingRoomsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(waiting, result);
        Assert.Contains(playing, result);
        Assert.DoesNotContain(finished, result);
    }

    [Fact]
    public async Task AddAsync_WhenRoomIsNull_Throws()
    {
        var repository = new InMemoryRoomRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WhenRoomIdentifierAlreadyExists_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var room = CreateRoom();
        await repository.AddAsync(room);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(room));
    }

    [Fact]
    public async Task AddAsync_WhenPlayerAlreadyBelongsToOngoingRoom_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var sharedPlayerId = Guid.NewGuid();
        var first = CreateRoom(sharedPlayerId, Guid.NewGuid());
        var second = CreateRoom(Guid.NewGuid(), sharedPlayerId);
        await repository.AddAsync(first);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(second));

        Assert.Null(await repository.GetByIdAsync(second.RoomId));
    }

    [Fact]
    public async Task ConcurrentAdds_WithSharedPlayer_PersistExactlyOneOngoingRoom()
    {
        var repository = new InMemoryRoomRepository();
        var sharedPlayerId = Guid.NewGuid();
        var rooms = Enumerable.Range(0, 20)
            .Select(index => index % 2 == 0
                ? CreateRoom(sharedPlayerId, Guid.NewGuid())
                : CreateRoom(Guid.NewGuid(), sharedPlayerId))
            .ToList();

        var outcomes = await Task.WhenAll(rooms.Select(room => Task.Run(async () =>
        {
            try
            {
                await repository.AddAsync(room);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        })));

        Assert.Single(outcomes, success => success);
        Assert.Single(await repository.GetOngoingRoomsAsync());
    }

    [Fact]
    public async Task AddAsync_AllowsPlayerFromFinishedRoomToEnterNewRoom()
    {
        var repository = new InMemoryRoomRepository();
        var reusablePlayerId = Guid.NewGuid();
        var finished = CreateRoom(reusablePlayerId, Guid.NewGuid());
        Start(finished);
        finished.EndMatch(MatchResultType.PlayerXWin);
        var replacement = CreateRoom(Guid.NewGuid(), reusablePlayerId);
        await repository.AddAsync(finished);

        await repository.AddAsync(replacement);

        Assert.Same(replacement, await repository.GetByIdAsync(replacement.RoomId));
    }

    [Fact]
    public async Task UpdateAsync_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateAsync(CreateRoom()));
    }

    [Fact]
    public async Task RemoveAsync_IsIdempotent()
    {
        var repository = new InMemoryRoomRepository();
        var room = CreateRoom();
        await repository.AddAsync(room);

        await repository.RemoveAsync(room.RoomId);
        await repository.RemoveAsync(room.RoomId);

        Assert.Null(await repository.GetByIdAsync(room.RoomId));
    }

    private static Room CreateRoom(Guid? playerXId = null, Guid? playerOId = null) =>
        new(
            new PlayerSlot(playerXId ?? Guid.NewGuid(), Symbol.X),
            new PlayerSlot(playerOId ?? Guid.NewGuid(), Symbol.O));

    private static void Start(Room room)
    {
        room.MarkReady(room.PlayerX.PlayerId);
        room.MarkReady(room.PlayerO.PlayerId);
        room.StartNewMatch(StartTime);
    }
}
