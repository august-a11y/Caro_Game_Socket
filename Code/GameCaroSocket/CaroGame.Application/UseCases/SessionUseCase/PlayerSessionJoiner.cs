using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public sealed class PlayerSessionJoiner : IPlayerSessionJoiner
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly ISessionRepository _sessionRepository;

        public PlayerSessionJoiner(IPlayerRepository playerRepository, ISessionRepository sessionRepository)
        {
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task<Session> JoinAsync(string nickname, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                throw new ArgumentException("Nickname must be provided", nameof(nickname));

            cancellationToken.ThrowIfCancellationRequested();

            // Try find existing player by nickname
            var player = await _playerRepository.GetByNicknameAsync(nickname);

            if (player is null)
            {
                // Business: creating a Player during join is not authorized here.
                // Follow existing project conventions: throw InvalidOperationException when missing.
                throw new InvalidOperationException("Player not found");
            }

            // If the player already has an active session, return it
            var existingSession = await _sessionRepository.GetByPlayerIdAsync(player.PlayerId);
            if (existingSession is not null)
            {
                return existingSession;
            }

            // Create new session
            var session = new Session(player.PlayerId, Guid.NewGuid());
            await _sessionRepository.AddAsync(session);

            return session;
        }
    }
}
