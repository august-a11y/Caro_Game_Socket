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

    public string? Respond(string challengerId, string opponentId, bool accept)
    {
        if (!Guid.TryParse(challengerId, out var challengerGuid) ||
            !Guid.TryParse(opponentId, out var opponentGuid) ||
            challengerGuid == Guid.Empty ||
            opponentGuid == Guid.Empty ||
            challengerGuid == opponentGuid)
            return null;

        var pendingChallenges = _challengeRepository.GetPendingForPlayer(opponentGuid);

        var challenge = pendingChallenges.FirstOrDefault(c =>
            c.FromPlayerId == challengerGuid &&
            c.ToPlayerId == opponentGuid);

        if (challenge is null)
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (challenge.IsExpired(now))
        {
            challenge.Expire(now);
            _challengeRepository.Update(challenge);
            return null;
        }

        if (!accept)
        {
            challenge.Reject(now);
            _challengeRepository.Update(challenge);
            return null;
        }

        var challenger = _playerRepository.GetById(challengerGuid);

        if (challenger is null || challenger.Status != PlayerStatus.Free)
            return null;

        var opponent = _playerRepository.GetById(opponentGuid);

        if (opponent is null || opponent.Status != PlayerStatus.Free)
            return null;

        var room = new Room(
            new PlayerSlot(challengerGuid, Symbol.X, challenger.Nickname),
            new PlayerSlot(opponentGuid, Symbol.O, opponent.Nickname),
            createdAt: now);

        _roomRepository.Add(room);

        challenger.Status = PlayerStatus.InMatch;
        opponent.Status = PlayerStatus.InMatch;
        _playerRepository.Update(challenger);
        _playerRepository.Update(opponent);

        challenge.Accept(now);
        _challengeRepository.Update(challenge);

        return room.RoomId.ToString();
    }
}
