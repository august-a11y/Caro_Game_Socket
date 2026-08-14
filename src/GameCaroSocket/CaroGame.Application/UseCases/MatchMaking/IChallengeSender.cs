using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.MatchMaking
{
    public interface IChallengeSender
    {
        Task<Challenge> SendAsync(Guid fromPlayerId, Guid toPlayerId, CancellationToken cancellationToken);
    }
}

