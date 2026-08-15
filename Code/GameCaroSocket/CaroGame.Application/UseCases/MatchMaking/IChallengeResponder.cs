using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.MatchMaking
{
    public interface IChallengeResponder
    { 
        Task<Challenge> ResponseChallenge(Guid fromPlayerId, Guid toPlayerId, bool isAccepted);
    }
}

