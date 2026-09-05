using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.MatchMaking;

public sealed class ChallengeResponder : IChallengeResponder
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly TimeProvider _timeProvider;

    public ChallengeResponder(
        IChallengeRepository challengeRepository,
        IPlayerRepository playerRepository,
        IRoomRepository roomRepository,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(challengeRepository);
        ArgumentNullException.ThrowIfNull(playerRepository);
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _challengeRepository = challengeRepository;
        _playerRepository = playerRepository;
        _roomRepository = roomRepository;
        _timeProvider = timeProvider;
    }

    public async Task<string?> RespondAsync(string challengerId, string opponentId, bool accept, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Guid.TryParse(challengerId, out var challengerGuid) ||
            !Guid.TryParse(opponentId, out var opponentGuid) ||
            challengerGuid == Guid.Empty ||
            opponentGuid == Guid.Empty ||
            challengerGuid == opponentGuid)
            return null;

        var pendingChallenges = await _challengeRepository.GetPendingForPlayerAsync(opponentGuid);
        cancellationToken.ThrowIfCancellationRequested();

        var challenge = pendingChallenges.FirstOrDefault(c =>
            c.FromPlayerId == challengerGuid &&
            c.ToPlayerId == opponentGuid);

        if (challenge is null)
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (challenge.IsExpired(now))
        {
            challenge.Expire();
            await _challengeRepository.UpdateAsync(challenge);
            return null;
        }

        if (!accept)
        {
            challenge.Reject();
            await _challengeRepository.UpdateAsync(challenge);
            return null;
        }

        var challenger = await _playerRepository.GetByIdAsync(challengerGuid);
        cancellationToken.ThrowIfCancellationRequested();

        if (challenger is null || challenger.Status != PlayerStatus.Free)
            return null;

        var opponent = await _playerRepository.GetByIdAsync(opponentGuid);
        cancellationToken.ThrowIfCancellationRequested();

        if (opponent is null || opponent.Status != PlayerStatus.Free)
            return null;

        var room = new Room(
            new PlayerSlot(challengerGuid, Symbol.X),
            new PlayerSlot(opponentGuid, Symbol.O),
            createdAt: now);

        await _roomRepository.AddAsync(room);

        challenger.Status = PlayerStatus.InMatch;
        opponent.Status = PlayerStatus.InMatch;
        await _playerRepository.UpdateAsync(challenger);
        await _playerRepository.UpdateAsync(opponent);

        challenge.Accept();
        await _challengeRepository.UpdateAsync(challenge);

        return room.RoomId.ToString();
    }
}
