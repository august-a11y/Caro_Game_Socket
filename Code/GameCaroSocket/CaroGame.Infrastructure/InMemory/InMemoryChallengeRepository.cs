using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using System.Collections.Concurrent;

namespace CaroGame.Infrastructure.InMemory;

// Callers coordinate entity changes and multi-step operations using shared lobby/room locks.
// ConcurrentDictionary protects individual dictionary operations only.
public sealed class InMemoryChallengeRepository : IChallengeRepository
{
    private readonly ConcurrentDictionary<Guid, Challenge> _challenges = new();
    public IReadOnlyList<Challenge> GetAll() => _challenges.Values.ToArray();

    public void Add(Challenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);

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

    public Challenge? GetById(Guid challengeId)
    {
        _challenges.TryGetValue(challengeId, out var challenge);
        return challenge;
    }

    public IReadOnlyList<Challenge> GetPendingForPlayer(Guid playerId)
    {
        IReadOnlyList<Challenge> pending = _challenges.Values
            .Where(challenge =>
                challenge.ToPlayerId == playerId &&
                challenge.Status == ChallengeStatus.Pending)
            .ToList();
        return pending;
    }

    public IReadOnlyList<Challenge> GetPending()
    {
        return _challenges.Values.Where(challenge => challenge.Status == ChallengeStatus.Pending).ToArray();
    }

    public void Update(Challenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        if (!_challenges.ContainsKey(challenge.ChallengeId))
            throw new KeyNotFoundException($"Challenge with ID '{challenge.ChallengeId}' was not found.");

        _challenges[challenge.ChallengeId] = challenge;
    }

    public void Remove(Guid challengeId)
    {
        _challenges.TryRemove(challengeId, out _);
    }

    private static bool SamePair(Challenge first, Challenge second) =>
        (first.FromPlayerId == second.FromPlayerId && first.ToPlayerId == second.ToPlayerId) ||
        (first.FromPlayerId == second.ToPlayerId && first.ToPlayerId == second.FromPlayerId);
}
