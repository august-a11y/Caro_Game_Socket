using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

public sealed class InMemoryPlayerRepository : IPlayerRepository
{
    private readonly ConcurrentDictionary<Guid, Player> _players = new();
    private readonly object _sync = new();

    public Task AddAsync(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        lock (_sync)
        {
            if (_players.ContainsKey(player.PlayerId))
                throw new InvalidOperationException($"Player with ID '{player.PlayerId}' already exists.");
            EnsureNicknameIsAvailable(player);

            if (!_players.TryAdd(player.PlayerId, player))
                throw new InvalidOperationException($"Player with ID '{player.PlayerId}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNicknameAsync(string nickname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        lock (_sync)
        {
            return Task.FromResult(_players.Values.Any(player =>
                string.Equals(player.Nickname, nickname.Trim(), StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<Player?> GetByIdAsync(Guid playerId)
    {
        _players.TryGetValue(playerId, out var player);
        return Task.FromResult(player);
    }

    public Task<Player?> GetByNicknameAsync(string nickname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        lock (_sync)
        {
            var normalizedNickname = nickname.Trim();
            var player = _players.Values.FirstOrDefault(candidate =>
                string.Equals(candidate.Nickname, normalizedNickname, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(player);
        }
    }

    public Task<IReadOnlyList<Player>> GetOnlinePlayersAsync()
    {
        lock (_sync)
        {
            IReadOnlyList<Player> players = _players.Values
                .Where(player => player.Status != PlayerStatus.Offline)
                .ToList();
            return Task.FromResult(players);
        }
    }

    public Task UpdateAsync(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        lock (_sync)
        {
            if (!_players.ContainsKey(player.PlayerId))
                throw new KeyNotFoundException($"Player with ID '{player.PlayerId}' was not found.");

            EnsureNicknameIsAvailable(player);
            _players[player.PlayerId] = player;
        }

        return Task.CompletedTask;
    }

    private void EnsureNicknameIsAvailable(Player player)
    {
        if (_players.Values.Any(existing =>
            existing.PlayerId != player.PlayerId &&
            string.Equals(existing.Nickname, player.Nickname, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Nickname '{player.Nickname}' is already in use.");
        }
    }
}
