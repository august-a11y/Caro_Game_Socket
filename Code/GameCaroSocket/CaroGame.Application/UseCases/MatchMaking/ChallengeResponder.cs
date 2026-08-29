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

        if (!Guid.TryParse(challengerId, out var challengerGuid) || !Guid.TryParse(opponentId, out var opponentGuid))
            return null;

        var newMatch = new Match(challengerGuid, opponentGuid);
        await _matchRepository.AddAsync(newMatch, cancellationToken);
        
        return $"{challengerGuid}_{opponentGuid}";
    }
}
