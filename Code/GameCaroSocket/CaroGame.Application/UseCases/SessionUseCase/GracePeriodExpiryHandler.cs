using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public class GracePeriodExpiryHandler : IGracePeriodExpiryHandler
    {
        private readonly IRoomRepository _roomRepository;

        public GracePeriodExpiryHandler(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<Room> HandleAsync(Guid roomId, Guid playerId, CancellationToken cancellationToken)
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room == null) return null;

            if (room.Disconnected.TryGetValue(playerId, out var disconnectInfo))
            {
                if (DateTime.UtcNow >= disconnectInfo.GracePeriodEndsAt)
                {
                    // Người chơi đã quá hạn kết nối lại, xử thua.
                    room.CurrentMatch?.EndMatch(MatchResultType.Win);
                    await _roomRepository.UpdateAsync(room);
                }
            }

            return room;
        }
    }
}
