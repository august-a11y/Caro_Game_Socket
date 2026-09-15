namespace CaroGame.Application.UseCases.MatchMaking;

public interface IChallengeResponder
{
    string? Respond(string challengerId, string opponentId, bool accept);
}
