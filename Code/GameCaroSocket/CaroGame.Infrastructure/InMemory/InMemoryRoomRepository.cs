using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

// Callers coordinate entity changes and multi-step operations using shared lobby/room locks.
// ConcurrentDictionary protects individual dictionary operations only.
public sealed class InMemoryRoomRepository : IRoomRepository
{
    private readonly ConcurrentDictionary<Guid, Room> _rooms = new();
    public IReadOnlyList<Room> GetAll() => _rooms.Values.ToArray();

    public Room? GetById(Guid roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }

    public IReadOnlyList<Room> GetOngoingRooms()
    {
        IReadOnlyList<Room> rooms = _rooms.Values.Where(IsOngoing).ToList();
        return rooms;
    }

    public void Add(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (_rooms.ContainsKey(room.RoomId))
            throw new InvalidOperationException($"Room with ID '{room.RoomId}' already exists.");
        EnsurePlayersHaveNoOtherOngoingRoom(room);

        if (!_rooms.TryAdd(room.RoomId, room))
            throw new InvalidOperationException($"Room with ID '{room.RoomId}' already exists.");
    }

    public void Update(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        if (!_rooms.ContainsKey(room.RoomId))
            throw new KeyNotFoundException($"Room with ID '{room.RoomId}' was not found.");

        EnsurePlayersHaveNoOtherOngoingRoom(room);
        _rooms[room.RoomId] = room;
    }

    public void Remove(Guid roomId)
    {
        _rooms.TryRemove(roomId, out _);
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
