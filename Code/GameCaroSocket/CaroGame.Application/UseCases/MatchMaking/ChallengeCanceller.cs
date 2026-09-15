using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.MatchMaking;

public sealed class ChallengeCanceller(IChallengeRepository challenges, TimeProvider time) : IChallengeCanceller
{
    public Challenge Cancel(Guid challengeId, Guid playerId)
    {
        if (challengeId == Guid.Empty || playerId == Guid.Empty)
            throw new ArgumentException("Challenge and player identifiers must not be empty.");
        var challenge = challenges.GetById(challengeId)
            ?? throw new KeyNotFoundException("Challenge was not found.");
        if (challenge.FromPlayerId != playerId)
            throw new UnauthorizedAccessException("Only the challenger can cancel this invitation.");
        if (challenge.Status != ChallengeStatus.Pending)
            throw new InvalidOperationException("Challenge is no longer pending.");
        var now = time.GetUtcNow().UtcDateTime;
        if (challenge.IsExpired(now)) challenge.Expire(now);
        else challenge.Cancel(now);
        challenges.Update(challenge);
        return challenge;
    }
}
