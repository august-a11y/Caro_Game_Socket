namespace CaroGame.Application.UseCases.MatchMaking;

public interface IChallengeSender
{
    bool SendChallenge(string challengerId, string opponentId);
}
