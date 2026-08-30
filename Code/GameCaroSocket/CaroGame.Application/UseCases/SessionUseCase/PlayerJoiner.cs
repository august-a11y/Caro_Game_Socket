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

            // Theo logic mới: Luôn tạo Player (kèm Guid mới) mỗi khi Client kết nối lần đầu
            var player = new Player(nickname);
            await _playerRepository.AddAsync(player);

            var newSession = new Session(player.PlayerId, Guid.NewGuid());
            await _sessionRepository.AddAsync(newSession);

            return newSession;
        }
    }
}
