using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaroGame.Infrastructure.InMemory
{
    public class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly ConcurrentDictionary<Guid, Player> _players = new();

        public Task AddAsync(Player player)
        {
            if (player is null)
                throw new ArgumentNullException(nameof(player));

            _players[player.PlayerId] = player;

            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNicknameAsync(string nickname)
        {
            var exists = _players.Values
                .Any(p => string.Equals(p.Nickname, nickname, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task<Player?> GetByIdAsync(Guid playerId)
        {
            _players.TryGetValue(playerId, out var player);

            return Task.FromResult(player);
        }

        public Task<Player?> GetByNicknameAsync(string nickname)
        {
            var player = _players.Values
                .FirstOrDefault(p => string.Equals(p.Nickname, nickname, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(player);
        }

        public Task<IReadOnlyList<Player>> GetOnlinePlayersAsync()
        {
            // Player entity hiện chưa có field trạng thái online/offline.
            // Repo chỉ lưu trữ; tạm thời trả về toàn bộ player đang có trong repo.
            // Nếu cần lọc đúng player đang online, nên kết hợp với ISessionRepository
            // ở tầng use case (OnlinePlayerFinder), hoặc bổ sung field Status vào
            // Player entity (domain) - phần này ngoài scope của Player repo.
            IReadOnlyList<Player> players = _players.Values.ToList();

            return Task.FromResult(players);
        }

        public Task UpdateAsync(Player player)
        {
            if (player is null)
                throw new ArgumentNullException(nameof(player));

            while (true)
            {
                if (!_players.TryGetValue(player.PlayerId, out var existing))
                {
                    break;
                }

                if (_players.TryUpdate(player.PlayerId, player, existing))
                {
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }
}