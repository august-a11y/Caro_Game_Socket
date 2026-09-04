using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.Services;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Application.UseCases.GamePlay;

public sealed class MoveSubmitter : IMoveSubmitter
{
    private readonly IRoomRepository _roomRepository;
    private readonly IWinConditionChecker _winConditionChecker;
    private readonly IMatchEnder _matchEnder;
    private readonly TimeProvider _timeProvider;

    public MoveSubmitter(
        IRoomRepository roomRepository,
        IWinConditionChecker winConditionChecker,
        IMatchEnder matchEnder,
        TimeProvider timeProvider)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _winConditionChecker = winConditionChecker ?? throw new ArgumentNullException(nameof(winConditionChecker));
        _matchEnder = matchEnder ?? throw new ArgumentNullException(nameof(matchEnder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Room> SubmitMoveAsync(
        Guid roomId,
        Guid playerId,
        Position position,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (roomId == Guid.Empty)
            throw new ArgumentException("Room identifier must not be empty.", nameof(roomId));
        if (playerId == Guid.Empty)
            throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));

        var room = await _roomRepository.GetByIdAsync(roomId)
            ?? throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");

        cancellationToken.ThrowIfCancellationRequested();

        var move = room.ApplyMove(
            playerId,
            position,
            _timeProvider.GetUtcNow().UtcDateTime);
        var match = room.CurrentMatch!;
        var result = _winConditionChecker.Check(match.Board, move);

        if (result != MatchResultType.Continue)
            return await _matchEnder.EndMatchAsync(room, result, cancellationToken);

        await _roomRepository.UpdateAsync(room);
        return room;
    }
}
