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
        private readonly TimeProvider _timeProvider;

        public PlayerJoiner(
            IPlayerRepository playerRepository,
            ISessionRepository sessionRepository,
            TimeProvider timeProvider)
        {
            _playerRepository = playerRepository
                ?? throw new ArgumentNullException(nameof(playerRepository));
            _sessionRepository = sessionRepository
                ?? throw new ArgumentNullException(nameof(sessionRepository));
            _timeProvider = timeProvider
                ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<Session> JoinAsync(string nickname, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(nickname))
            {
                throw new ArgumentException("Nickname cannot be empty", nameof(nickname));
            }

            // Theo logic mới: Luôn tạo Player (kèm Guid mới) mỗi khi Client kết nối lần đầu
            var normalizedNickname = nickname.Trim();
            if (await _playerRepository.ExistsByNicknameAsync(normalizedNickname))
            {
                throw new InvalidOperationException(
                    $"Nickname '{normalizedNickname}' is already in use.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var player = new Player(normalizedNickname);
            await _playerRepository.AddAsync(player);

            cancellationToken.ThrowIfCancellationRequested();

            var newSession = new Session(
                player.PlayerId,
                Guid.NewGuid(),
                _timeProvider.GetUtcNow().UtcDateTime);
            await _sessionRepository.AddAsync(newSession);

            return newSession;
        }
    }
}
