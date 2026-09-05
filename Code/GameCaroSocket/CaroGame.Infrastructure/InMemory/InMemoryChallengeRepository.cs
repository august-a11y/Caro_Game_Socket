using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

public sealed class InMemoryChallengeRepository : IChallengeRepository
{
    private readonly ConcurrentDictionary<Guid, Challenge> _challenges = new();
    private readonly object _sync = new();

    public Task AddAsync(Challenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        lock (_sync)
        {
            if (_challenges.ContainsKey(challenge.ChallengeId))
                throw new InvalidOperationException($"Challenge with ID '{challenge.ChallengeId}' already exists.");
            if (_challenges.Values.Any(existing =>
                existing.Status == ChallengeStatus.Pending && SamePair(existing, challenge)))
            {
                throw new InvalidOperationException("A pending challenge already exists for this player pair.");
            }

            if (!_challenges.TryAdd(challenge.ChallengeId, challenge))
                throw new InvalidOperationException($"Challenge with ID '{challenge.ChallengeId}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Challenge?> GetByIdAsync(Guid challengeId)
    {
        _challenges.TryGetValue(challengeId, out var challenge);
        return Task.FromResult(challenge);
    }

    public Task<IReadOnlyList<Challenge>> GetPendingForPlayerAsync(Guid playerId)
    {
        lock (_sync)
        {
            IReadOnlyList<Challenge> pending = _challenges.Values
                .Where(challenge =>
                    challenge.ToPlayerId == playerId &&
                    challenge.Status == ChallengeStatus.Pending)
                .ToList();
            return Task.FromResult(pending);
        }
    }

    public Task UpdateAsync(Challenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        lock (_sync)
        {
            if (!_challenges.ContainsKey(challenge.ChallengeId))
                throw new KeyNotFoundException($"Challenge with ID '{challenge.ChallengeId}' was not found.");

            _challenges[challenge.ChallengeId] = challenge;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid challengeId)
    {
        _challenges.TryRemove(challengeId, out _);
        return Task.CompletedTask;
    }

    private static bool SamePair(Challenge first, Challenge second) =>
        (first.FromPlayerId == second.FromPlayerId && first.ToPlayerId == second.ToPlayerId) ||
        (first.FromPlayerId == second.ToPlayerId && first.ToPlayerId == second.FromPlayerId);
}
