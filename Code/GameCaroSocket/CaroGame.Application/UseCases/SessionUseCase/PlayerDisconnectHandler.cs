using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Enum;
using System;
using System.Linq;

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

        public void Handle(Guid playerId)
        {
            var session = _sessionRepository.GetByPlayerId(playerId);
            if (session is null)
                return;


            var player = _playerRepository.GetById(playerId);
            if (player is null)
                throw new KeyNotFoundException($"Player with ID '{playerId}' was not found.");


            var ongoingRooms = _roomRepository.GetOngoingRooms();
            var room = ongoingRooms.FirstOrDefault(r => 
                r.IsActivePlayer(playerId));


            var now = _timeProvider.GetUtcNow().UtcDateTime;
            session.MarkDisconnected(now);
            player.Status = PlayerStatus.Offline;

            if (room is not null)
            {
                room.MarkDisconnected(playerId, gracePeriodSeconds: 60, now);
            }

            _sessionRepository.Update(session);
            _playerRepository.Update(player);

            if (room is not null)
                _roomRepository.Update(room);
        }
    }
}
