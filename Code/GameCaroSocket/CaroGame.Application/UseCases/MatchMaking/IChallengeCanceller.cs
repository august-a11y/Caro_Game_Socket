using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.MatchMaking;

public interface IChallengeCanceller
{
    Challenge Cancel(Guid challengeId, Guid playerId);
}
