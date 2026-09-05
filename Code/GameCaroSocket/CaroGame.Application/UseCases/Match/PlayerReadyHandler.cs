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

    public async Task<Room> HandleAsync(
        Guid roomId,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (roomId == Guid.Empty)
            throw new ArgumentException("Room identifier must not be empty.", nameof(roomId));
        if (playerId == Guid.Empty)
            throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));

        var room = await _roomRepository.GetByIdAsync(roomId)
            ?? throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");

        cancellationToken.ThrowIfCancellationRequested();

        if (room.Status != RoomStatus.Waiting)
            throw new InvalidOperationException("Players can become ready only while the room is waiting.");
        if (!room.IsActivePlayer(playerId))
            throw new InvalidOperationException("Only an active player can become ready.");
        if (!room.MarkReady(playerId))
            return room;

        if (room.ArePlayersReady)
            room.StartNewMatch(_timeProvider.GetUtcNow().UtcDateTime);

        await _roomRepository.UpdateAsync(room);
        return room;
    }
}
