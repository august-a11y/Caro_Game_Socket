using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.MatchMaking;

public sealed class ChallengeResponder : IChallengeResponder
{
    private readonly IMatchRepository _matchRepository;

    public ChallengeResponder(IMatchRepository matchRepository)
    {
        _matchRepository = matchRepository;
    }

    public async Task<string?> RespondAsync(string challengerId, string opponentId, bool accept, CancellationToken cancellationToken = default)
    {
        if (!accept)
            return null;

        var newMatch = new Match(challengerId, opponentId);
        await _matchRepository.AddAsync(newMatch, cancellationToken);

        return newMatch.Id;
    }
}
