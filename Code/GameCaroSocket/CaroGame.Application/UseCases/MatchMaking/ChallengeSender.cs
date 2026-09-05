using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.MatchMaking;

public sealed class ChallengeSender : IChallengeSender
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly IPlayerRepository _playerRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly TimeProvider _timeProvider;

    public ChallengeSender(
        IPlayerRepository playerRepository,
        IChallengeRepository challengeRepository,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(playerRepository);
        ArgumentNullException.ThrowIfNull(challengeRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _playerRepository = playerRepository;
        _challengeRepository = challengeRepository;
        _timeProvider = timeProvider;
    }

    public async Task<bool> SendChallengeAsync(string challengerId, string opponentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Guid.TryParse(challengerId, out var challengerGuid) ||
            !Guid.TryParse(opponentId, out var opponentGuid) ||
            challengerGuid == Guid.Empty ||
            opponentGuid == Guid.Empty ||
            challengerGuid == opponentGuid)
            return false;

        var challenger = await _playerRepository.GetByIdAsync(challengerGuid);
        cancellationToken.ThrowIfCancellationRequested();

        if (challenger is null || challenger.Status != PlayerStatus.Free)
            return false;

        var opponent = await _playerRepository.GetByIdAsync(opponentGuid);
        cancellationToken.ThrowIfCancellationRequested();

        if (opponent is null || opponent.Status != PlayerStatus.Free)
            return false;

        var challengesToOpponent = await _challengeRepository.GetPendingForPlayerAsync(opponentGuid);
        cancellationToken.ThrowIfCancellationRequested();

        var challengesToChallenger = await _challengeRepository.GetPendingForPlayerAsync(challengerGuid);
        cancellationToken.ThrowIfCancellationRequested();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var pairChallenges = challengesToOpponent
            .Concat(challengesToChallenger)
            .Where(challenge => IsForPair(challenge, challengerGuid, opponentGuid))
            .DistinctBy(challenge => challenge.ChallengeId)
            .ToList();

        var hasActiveChallenge = false;
        foreach (var existingChallenge in pairChallenges)
        {
            if (!existingChallenge.IsExpired(now))
            {
                hasActiveChallenge = true;
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            existingChallenge.Expire();
            await _challengeRepository.UpdateAsync(existingChallenge);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (hasActiveChallenge)
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        var challenge = new Challenge(
            challengerGuid,
            opponentGuid,
            ChallengeLifetime,
            now);

        await _challengeRepository.AddAsync(challenge);
        return true;
    }

    private static bool IsForPair(Challenge challenge, Guid firstPlayerId, Guid secondPlayerId) =>
        (challenge.FromPlayerId == firstPlayerId && challenge.ToPlayerId == secondPlayerId) ||
        (challenge.FromPlayerId == secondPlayerId && challenge.ToPlayerId == firstPlayerId);
}
