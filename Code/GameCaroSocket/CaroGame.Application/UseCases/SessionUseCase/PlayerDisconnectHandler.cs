using CaroGame.Application.Interfaces.Repositories;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public class PlayerDisconnectHandler : IPlayerDisconnectHandler
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IRoomRepository _roomRepository;

        public PlayerDisconnectHandler(ISessionRepository sessionRepository, IRoomRepository roomRepository)
        {
            _sessionRepository = sessionRepository;
            _roomRepository = roomRepository;
        }

        public async Task HandleAsync(Guid playerId, CancellationToken cancellationToken)
        {
            await _sessionRepository.RemoveAsync(playerId);

            var ongoingRooms = await _roomRepository.GetOngoingRoomsAsync();
            var room = ongoingRooms.FirstOrDefault(r => 
                r.PlayerX.PlayerId == playerId || r.PlayerO.PlayerId == playerId);

            if (room != null)
            {
                // Giả định Room có phương thức MarkDisconnected. 
                // Nếu chưa có, người phụ trách Domain cần bổ sung.
                // room.MarkDisconnected(playerId, gracePeriodSeconds: 60);
                await _roomRepository.UpdateAsync(room);
            }
        }
    }
}
