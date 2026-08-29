using CaroGame.Application.Interfaces.Repositories;

namespace CaroGame.Application.UseCases.MatchMaking;

public sealed class ChallengeSender : IChallengeSender
{
    private readonly IPlayerRepository _playerRepository;

    public ChallengeSender(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<bool> SendChallengeAsync(string challengerId, string opponentId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(challengerId, opponentId, StringComparison.OrdinalIgnoreCase))
            return false;

        var opponent = await _playerRepository.GetByIdAsync(opponentId, cancellationToken);
        if (opponent == null || !opponent.IsOnline || opponent.IsInMatch)
            return false;

        return true;
    }
}

