using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.MatchMaking;

public sealed class ChallengeSender : IChallengeSender
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IChallengeRepository _challengeRepository;

    public ChallengeSender(
        IPlayerRepository playerRepository, 
        IChallengeRepository challengeRepository)
    {
        _playerRepository = playerRepository;
        _challengeRepository = challengeRepository;
    }

    public async Task<bool> SendChallengeAsync(string challengerId, string opponentId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(challengerId, opponentId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!Guid.TryParse(challengerId, out var challengerGuid) || !Guid.TryParse(opponentId, out var opponentGuid))
            return false;

        var opponent = await _playerRepository.GetByIdAsync(opponentGuid, cancellationToken);
        if (opponent == null || !opponent.IsOnline || opponent.IsInMatch)
            return false;

        var challenge = new Challenge(challengerGuid, opponentGuid)
        {
            Status = ChallengeStatus.Pending
        };

        await _challengeRepository.AddAsync(challenge);

        return true;
    }
}
