using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.Match;

public sealed class SpectatorJoiner : ISpectatorJoiner
{
    private readonly IRoomRepository _roomRepository;

    public SpectatorJoiner(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
    }

    public Room JoinSpectator(
        Guid roomId,
        Guid playerId)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("Room identifier must not be empty.", nameof(roomId));
        if (playerId == Guid.Empty)
            throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));

        var room = _roomRepository.GetById(roomId)
            ?? throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");


        if (room.Status != RoomStatus.Playing)
            throw new InvalidOperationException("Spectators can only join a match in progress.");
        if (room.IsActivePlayer(playerId))
            throw new InvalidOperationException("Active players cannot join as spectators.");
        if (!room.AddSpectator(playerId))
            return room;

        _roomRepository.Update(room);
        return room;
    }
}
