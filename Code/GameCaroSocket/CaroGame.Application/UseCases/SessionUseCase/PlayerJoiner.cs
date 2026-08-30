using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public sealed class PlayerJoiner : IPlayerJoiner
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly ISessionRepository _sessionRepository;

        public PlayerJoiner(IPlayerRepository playerRepository, ISessionRepository sessionRepository)
        {
            _playerRepository = playerRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task<Session> JoinAsync(string nickname, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                throw new ArgumentException("Nickname cannot be empty", nameof(nickname));
            }

            var player = await _playerRepository.GetByNicknameAsync(nickname);
            if (player == null)
            {
                player = new Player(nickname);
                await _playerRepository.AddAsync(player);
            }

            // Remove old session if exists? The current design allows multiple sessions or overwrites.
            // Let's check if session exists for this player and remove it first to avoid stale sessions.
            var existingSession = await _sessionRepository.GetByPlayerIdAsync(player.PlayerId);
            if (existingSession != null)
            {
                await _sessionRepository.RemoveAsync(player.PlayerId);
            }

            var newSession = new Session(player.PlayerId, Guid.NewGuid());
            await _sessionRepository.AddAsync(newSession);

            return newSession;
        }
    }
}
