namespace CaroGame.Application.UseCases.MatchMaking;

public interface IChallengeResponder
{
    Task<string?> RespondAsync(string challengerId, string opponentId, bool accept, CancellationToken cancellationToken = default);
}
