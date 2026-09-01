using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Spectator
namespace CaroGame.Application.UseCases.Match
{
    public class SpectatorLeaver
    public class SpectatorLeaver : ILeaveSpectatorUseCase
    {
        private readonly IRoomRepository _roomRepository;

        public SpectatorLeaver(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<Room> LeaveSpectator(Guid roomId, Guid playerId)
        {
            if (roomId == Guid.Empty)
                throw new ArgumentException("roomId must not be empty", nameof(roomId));

            if (playerId == Guid.Empty)
                throw new ArgumentException("playerId must not be empty", nameof(playerId));

            var room = await _roomRepository.GetByIdAsync(roomId);

            if (room == null)
                throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");

            if (!room.Spectators.Contains(playerId))
                throw new InvalidOperationException("Player is not a spectator in this room.");

            room.RemoveSpectator(playerId);

            await _roomRepository.UpdateAsync(room);

            return room;
        }
    }
}
