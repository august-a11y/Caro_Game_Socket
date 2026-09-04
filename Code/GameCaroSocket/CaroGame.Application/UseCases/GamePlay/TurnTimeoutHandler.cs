using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay;

public sealed class TurnTimeoutHandler : ITurnTimeoutHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly IMatchEnder _matchEnder;
    private readonly TimeProvider _timeProvider;

    public TurnTimeoutHandler(
        IRoomRepository roomRepository,
        IMatchEnder matchEnder,
        TimeProvider timeProvider)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _matchEnder = matchEnder ?? throw new ArgumentNullException(nameof(matchEnder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task HandleTurnTimeoutAsync(
        Guid roomId,
        Guid playerId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (roomId == Guid.Empty)
            throw new ArgumentException("Room identifier must not be empty.", nameof(roomId));
        if (playerId == Guid.Empty)
            throw new ArgumentException("Player identifier must not be empty.", nameof(playerId));

        var room = await _roomRepository.GetByIdAsync(roomId);

        cancellationToken.ThrowIfCancellationRequested();

        if (room?.Status != RoomStatus.Playing || room.CurrentMatch is null)
            return;

        var turn = room.CurrentMatch.TurnManager;
        if (turn.IsPaused)
            return;

        if (turn.CurrentTurnPlayerId != playerId)
            return;

        if (!turn.IsTimeUp(_timeProvider.GetUtcNow().UtcDateTime))
            return;

        var result = playerId == room.PlayerX.PlayerId
            ? MatchResultType.PlayerOWin
            : MatchResultType.PlayerXWin;

        await _matchEnder.EndMatchAsync(room, result, cancellationToken);
    }
}
