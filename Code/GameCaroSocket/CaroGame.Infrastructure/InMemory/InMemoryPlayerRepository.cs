using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

// Callers coordinate entity changes and multi-step operations using shared lobby/room locks.
// ConcurrentDictionary protects individual dictionary operations only.
public sealed class InMemoryPlayerRepository : IPlayerRepository
{
    private readonly ConcurrentDictionary<Guid, Player> _players = new();
    public void Remove(Guid playerId) => _players.TryRemove(playerId, out _);

    public void Add(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (_players.ContainsKey(player.PlayerId))
            throw new InvalidOperationException($"Player with ID '{player.PlayerId}' already exists.");
        EnsureNicknameIsAvailable(player);

        if (!_players.TryAdd(player.PlayerId, player))
            throw new InvalidOperationException($"Player with ID '{player.PlayerId}' already exists.");
    }

    public bool ExistsByNickname(string nickname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        return _players.Values.Any(player =>
            string.Equals(player.Nickname, nickname.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Player? GetById(Guid playerId)
    {
        _players.TryGetValue(playerId, out var player);
        return player;
    }

    public Player? GetByNickname(string nickname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);

        var normalizedNickname = nickname.Trim();
        var player = _players.Values.FirstOrDefault(candidate =>
            string.Equals(candidate.Nickname, normalizedNickname, StringComparison.OrdinalIgnoreCase));
        return player;
    }

    public IReadOnlyList<Player> GetOnlinePlayers()
    {
        IReadOnlyList<Player> players = _players.Values
            .Where(player => player.Status != PlayerStatus.Offline)
            .ToList();
        return players;
    }

    public void Update(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!_players.ContainsKey(player.PlayerId))
            throw new KeyNotFoundException($"Player with ID '{player.PlayerId}' was not found.");

        EnsureNicknameIsAvailable(player);
        _players[player.PlayerId] = player;
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
