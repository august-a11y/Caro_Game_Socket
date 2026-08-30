using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public class PlayerReconnector : IPlayerReconnector
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IRoomRepository _roomRepository;

        public PlayerReconnector(ISessionRepository sessionRepository, IPlayerRepository playerRepository, IRoomRepository roomRepository)
        {
            _sessionRepository = sessionRepository;
            _playerRepository = playerRepository;
            _roomRepository = roomRepository;
        }

        public async Task<Session> ReconnectPlayerAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken)
        {
            var player = await _playerRepository.GetByIdAsync(playerId);
            if (player == null)
            {
                throw new Exception("Player not found");
            }

            var session = new Session(playerId, sessionId);
            // Có thể cần Update thay vì Add nếu logic cho phép ghi đè
            await _sessionRepository.AddAsync(session);

            var ongoingRooms = await _roomRepository.GetOngoingRoomsAsync();
            var room = ongoingRooms.FirstOrDefault(r => 
                r.PlayerX.PlayerId == playerId || r.PlayerO.PlayerId == playerId);

            if (room != null)
            {
                // Giả định Room có phương thức MarkReconnected
                // room.MarkReconnected(playerId);
                await _roomRepository.UpdateAsync(room);
            }

            return session;
        }
    }
}
