using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.Match;

public sealed class SpectatorLeaver : ISpectatorLeaver
{
    private readonly IRoomRepository _roomRepository;

    public SpectatorLeaver(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
    }

    public Room LeaveSpectator(
        Guid roomId,
        Guid playerId)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("Room identifier must not be empty.", nameof(roomId));
        if (playerId == Guid.Empty)
            throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));

        var room = _roomRepository.GetById(roomId)
            ?? throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");


        if (!room.RemoveSpectator(playerId))
            return room;

        _roomRepository.Update(room);
        return room;
    }
}
