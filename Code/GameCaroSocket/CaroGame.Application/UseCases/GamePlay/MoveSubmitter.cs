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
    private readonly TimeProvider _timeProvider;

    public MoveSubmitter(
        IRoomRepository roomRepository,
        IWinConditionChecker winConditionChecker,
        IMatchEnder matchEnder,
        TimeProvider timeProvider)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _winConditionChecker = winConditionChecker ?? throw new ArgumentNullException(nameof(winConditionChecker));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public MatchResultType SubmitMove(
        Guid roomId,
        Guid playerId,
        Position position)
    {
       
        if (roomId == Guid.Empty)
            throw new ArgumentException("Room identifier must not be empty.", nameof(roomId));
         var room = _roomRepository.GetById(roomId)
            ?? throw new KeyNotFoundException($"Room with ID '{roomId}' was not found.");
        if (playerId == Guid.Empty)
            throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));

       

        var move = room.ApplyMove(
            playerId,
            position,
            _timeProvider.GetUtcNow().UtcDateTime);
        var match = room.CurrentMatch!;
        var result = _winConditionChecker.Check(match.Board, move);

        _roomRepository.Update(room);
        return result;
    }
}
