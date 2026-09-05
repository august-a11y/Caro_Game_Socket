using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

public sealed class InMemoryRoomRepository : IRoomRepository
{
    private readonly ConcurrentDictionary<Guid, Room> _rooms = new();
    private readonly object _sync = new();

    public Task<Room?> GetByIdAsync(Guid roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return Task.FromResult(room);
    }

    public Task<IReadOnlyList<Room>> GetOngoingRoomsAsync()
    {
        lock (_sync)
        {
            IReadOnlyList<Room> rooms = _rooms.Values.Where(IsOngoing).ToList();
            return Task.FromResult(rooms);
        }
    }

    public Task AddAsync(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        lock (_sync)
        {
            if (_rooms.ContainsKey(room.RoomId))
                throw new InvalidOperationException($"Room with ID '{room.RoomId}' already exists.");
            EnsurePlayersHaveNoOtherOngoingRoom(room);

            if (!_rooms.TryAdd(room.RoomId, room))
                throw new InvalidOperationException($"Room with ID '{room.RoomId}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        lock (_sync)
        {
            if (!_rooms.ContainsKey(room.RoomId))
                throw new KeyNotFoundException($"Room with ID '{room.RoomId}' was not found.");

            EnsurePlayersHaveNoOtherOngoingRoom(room);
            _rooms[room.RoomId] = room;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid roomId)
    {
        _rooms.TryRemove(roomId, out _);
        return Task.CompletedTask;
    }

    private void EnsurePlayersHaveNoOtherOngoingRoom(Room room)
    {
        if (!IsOngoing(room))
            return;

        var hasConflict = _rooms.Values.Any(existing =>
            existing.RoomId != room.RoomId &&
            IsOngoing(existing) &&
            (existing.IsActivePlayer(room.PlayerX.PlayerId) ||
             existing.IsActivePlayer(room.PlayerO.PlayerId)));

        if (hasConflict)
            throw new InvalidOperationException("A player can belong to only one ongoing room.");
    }

    private static bool IsOngoing(Room room) =>
        room.Status is RoomStatus.Waiting or RoomStatus.Playing;
}
