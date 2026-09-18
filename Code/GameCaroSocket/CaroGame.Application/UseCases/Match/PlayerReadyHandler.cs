using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.Match;

public sealed class PlayerReadyHandler : IPlayerReadyHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly TimeProvider _timeProvider;

    public PlayerReadyHandler(
        IRoomRepository roomRepository,
        TimeProvider timeProvider)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Room Handle(
        Guid roomId,
        Guid playerId)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("Room identifier must not be empty.", nameof(roomId));
        if (playerId == Guid.Empty)
            throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));

        var room = _roomRepository.GetById(roomId)
            ?? throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");


        if (room.Status != RoomStatus.Waiting)
            throw new InvalidOperationException("Players can become ready only while the room is waiting.");
        if (!room.IsActivePlayer(playerId))
            throw new InvalidOperationException("Only an active player can become ready.");
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (room.HasReadyExpired(now))
            throw new InvalidOperationException("The ready deadline has expired.");
        if (!room.MarkReady(playerId))
            return room;

        if (room.ArePlayersReady)
            room.StartNewMatch(now);

        _roomRepository.Update(room);
        return room;
    }
}
