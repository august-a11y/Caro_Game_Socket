using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.MatchMaking;

public sealed class ChallengeResponder : IChallengeResponder
{
    private readonly IMatchRepository _matchRepository;
    private readonly IChallengeRepository _challengeRepository;

    public ChallengeResponder(
        IMatchRepository matchRepository, 
        IChallengeRepository challengeRepository)
    {
        _matchRepository = matchRepository;
        _challengeRepository = challengeRepository;
    }

    public async Task<string?> RespondAsync(string challengerId, string opponentId, bool accept, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(challengerId, out var challengerGuid) || !Guid.TryParse(opponentId, out var opponentGuid))
            return null;

        var pendingChallenges = await _challengeRepository.GetPendingForPlayerAsync(opponentGuid);
        var challenge = pendingChallenges.FirstOrDefault(c => c.ChallengerId == challengerGuid);

        if (challenge == null)
            return null;

        if (!accept)
        {
            challenge.Status = ChallengeStatus.Rejected;
            await _challengeRepository.UpdateAsync(challenge);
            return null;
        }

        challenge.Status = ChallengeStatus.Accepted;
        await _challengeRepository.UpdateAsync(challenge);

        var newMatch = new Match(challengerGuid, opponentGuid);
        await _matchRepository.AddAsync(newMatch);

        return $"{challengerGuid}_{opponentGuid}";
    }
}
