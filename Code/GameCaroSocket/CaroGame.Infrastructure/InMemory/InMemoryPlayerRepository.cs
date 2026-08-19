using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System.Collections.Concurrent;
using System.Linq;

namespace CaroGame.Infrastructure.InMemory
{
   
    public class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly ConcurrentDictionary<Guid, Player> _players = new();

        public Task AddAsync(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            _players[player.PlayerId] = player;

            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNicknameAsync(string nickname)
        {
            var exists = _players.Values.Any(p =>
                string.Equals(p.Nickname, nickname, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task<Player?> GetByIdAsync(Guid playerId)
        {
            _players.TryGetValue(playerId, out var player);

            return Task.FromResult(player);
        }

        public Task<Player?> GetByNicknameAsync(string nickname)
        {
            var player = _players.Values.FirstOrDefault(p =>
                string.Equals(p.Nickname, nickname, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(player);
        }

        public Task<IReadOnlyList<Player>> GetOnlinePlayersAsync()
        {
            .
            IReadOnlyList<Player> players = _players.Values.ToList();

            return Task.FromResult(players);
        }

        public Task UpdateAsync(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            _players[player.PlayerId] = player;

            return Task.CompletedTask;
        }
    }
}