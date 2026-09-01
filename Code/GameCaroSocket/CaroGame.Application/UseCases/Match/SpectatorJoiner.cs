using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Match
{
    public class SpectatorJoiner : ISpectatorJoiner
    {
        private readonly IRoomRepository _roomRepository;
        public SpectatorJoiner(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }
        public async Task<Room> JoinSpectator(Guid roomId, Guid playerId)
        {
            if (roomId == Guid.Empty)
                throw new ArgumentException("roomId must not be empty", nameof(roomId));
            if (playerId == Guid.Empty)
                throw new ArgumentException("playerId must not be empty", nameof(playerId));
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null)
                throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");
            // Ensure player is not one of active players
            if (room.PlayerX.PlayerId == playerId || room.PlayerO.PlayerId == playerId)
                throw new InvalidOperationException("Active players cannot join as spectators.");
            // Add spectator (HashSet prevents duplicates)
            room.AddSpectator(playerId);
            await _roomRepository.UpdateAsync(room);
            return room;
        }
    }
}
