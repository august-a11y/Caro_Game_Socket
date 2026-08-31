using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public sealed class PlayerReconnector : IPlayerReconnector
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly ISessionRepository _sessionRepository;

        public PlayerReconnector(IPlayerRepository playerRepository, ISessionRepository sessionRepository)
        {
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task<Session> ReconnectPlayerAsync(Guid playerId, Guid sessionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var player = await _playerRepository.GetByIdAsync(playerId);
            if (player is null)
                throw new InvalidOperationException("Player not found");

            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                throw new InvalidOperationException("Session not found");

            if (session.PlayerId != playerId)
                throw new InvalidOperationException("Session does not belong to the player");

            // Update heartbeat to reflect reconnection
            session.UpdateHeartbeat();
            await _sessionRepository.UpdateAsync(session);

            return session;
        }
    }
}

