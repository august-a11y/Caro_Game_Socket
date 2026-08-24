using CaroGame.Application.Contracts;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.Lobby
{
    public sealed class PlayerJoiner : IPlayerJoiner
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly ISessionRepository _sessionRepository;

        public PlayerJoiner(IPlayerRepository playerRepository, ISessionRepository sessionRepository)
        {
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task<PlayerInfo> JoinAsync(Guid playerId, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            if (playerId == Guid.Empty)
                throw new ArgumentException("playerId is required", nameof(playerId));

            var player = await _playerRepository.GetByIdAsync(playerId);

            if (player is null)
                throw new InvalidOperationException("Player does not exist.");

            var session = await _sessionRepository.GetByPlayerIdAsync(playerId);

            if (session is null)
                throw new InvalidOperationException("Player has no active session.");

            var info = new PlayerInfo
            {
                UserId = player.PlayerId,
                Nickname = player.Nickname,
                Status = CaroGame.Application.Contracts.PlayerStatus.Online
            };

            return info;
        }
    }
}
 