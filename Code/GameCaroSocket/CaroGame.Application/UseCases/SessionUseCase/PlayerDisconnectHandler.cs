using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Enum;
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
        private readonly IPlayerRepository _playerRepository;
        private readonly TimeProvider _timeProvider;

        public PlayerDisconnectHandler(
            ISessionRepository sessionRepository,
            IRoomRepository roomRepository,
            IPlayerRepository playerRepository,
            TimeProvider timeProvider)
        {
            _sessionRepository = sessionRepository
                ?? throw new ArgumentNullException(nameof(sessionRepository));
            _roomRepository = roomRepository
                ?? throw new ArgumentNullException(nameof(roomRepository));
            _playerRepository = playerRepository
                ?? throw new ArgumentNullException(nameof(playerRepository));
            _timeProvider = timeProvider
                ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task HandleAsync(Guid playerId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var session = await _sessionRepository.GetByPlayerIdAsync(playerId);
            if (session is null)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            var player = await _playerRepository.GetByIdAsync(playerId);
            if (player is null)
                throw new KeyNotFoundException($"Player with ID '{playerId}' was not found.");

            cancellationToken.ThrowIfCancellationRequested();

            var ongoingRooms = await _roomRepository.GetOngoingRoomsAsync();
            var room = ongoingRooms.FirstOrDefault(r => 
                r.IsActivePlayer(playerId));

            cancellationToken.ThrowIfCancellationRequested();

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            session.MarkDisconnected(now);
            player.Status = PlayerStatus.Offline;

            if (room is not null)
            {
                room.MarkDisconnected(playerId, gracePeriodSeconds: 60, now);
            }

            await _sessionRepository.UpdateAsync(session);
            await _playerRepository.UpdateAsync(player);

            if (room is not null)
                await _roomRepository.UpdateAsync(room);
        }
    }
}
