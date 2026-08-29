namespace CaroGame.Application.UseCases.MatchMaking;

public interface IChallengeSender
{
    Task<bool> SendChallengeAsync(string challengerId, string opponentId, CancellationToken cancellationToken = default);
}
