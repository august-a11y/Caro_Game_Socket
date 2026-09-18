using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System;
using System.Linq;

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

        public Session ReconnectPlayer(Guid playerId, Guid sessionId)
        {
            var session = _sessionRepository.GetById(sessionId);
            if (session is null)
                throw new KeyNotFoundException($"Session with ID '{sessionId}' was not found.");
            if (session.PlayerId != playerId)
                throw new UnauthorizedAccessException(
                    "The session does not belong to the requested player.");


            var player = _playerRepository.GetById(playerId);
            if (player is null)
                throw new KeyNotFoundException($"Player with ID '{playerId}' was not found.");


            var ongoingRooms = _roomRepository.GetOngoingRooms();
            var room = ongoingRooms.FirstOrDefault(r => 
                r.IsActivePlayer(playerId));


            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (room?.HasReadyExpired(now) == true)
                throw new InvalidOperationException("The ready deadline has expired.");
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

            _sessionRepository.Update(session);
            _playerRepository.Update(player);

            if (room is not null)
                _roomRepository.Update(room);

            return session;
        }
    }
}
