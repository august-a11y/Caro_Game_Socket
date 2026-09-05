using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
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
        private readonly TimeProvider _timeProvider;

        public PlayerReconnector(
            ISessionRepository sessionRepository,
            IPlayerRepository playerRepository,
            IRoomRepository roomRepository,
            TimeProvider timeProvider)
        {
            _sessionRepository = sessionRepository
                ?? throw new ArgumentNullException(nameof(sessionRepository));
            _playerRepository = playerRepository
                ?? throw new ArgumentNullException(nameof(playerRepository));
            _roomRepository = roomRepository
                ?? throw new ArgumentNullException(nameof(roomRepository));
            _timeProvider = timeProvider
                ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<Session> ReconnectPlayerAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                throw new KeyNotFoundException($"Session with ID '{sessionId}' was not found.");
            if (session.PlayerId != playerId)
                throw new UnauthorizedAccessException(
                    "The session does not belong to the requested player.");

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
            if (room?.Status == RoomStatus.Playing &&
                room.Disconnected.TryGetValue(playerId, out var disconnectInfo) &&
                now >= disconnectInfo.GracePeriodEndsAt)
            {
                throw new InvalidOperationException(
                    "The reconnection grace period has expired.");
            }

            session.MarkReconnected(now);
            player.Status = room?.Status is RoomStatus.Waiting or RoomStatus.Playing
                ? PlayerStatus.InMatch
                : PlayerStatus.Free;

            if (room is not null)
            {
                room.MarkReconnected(playerId, now);
            }

            await _sessionRepository.UpdateAsync(session);
            await _playerRepository.UpdateAsync(player);

            if (room is not null)
                await _roomRepository.UpdateAsync(room);

            return session;
        }
    }
}
