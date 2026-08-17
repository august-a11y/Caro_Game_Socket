using System.Threading;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay
{
    public sealed class TurnTimeoutHandler : ITurnTimeoutHandler
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMatchEnder _matchEnder;

        public TurnTimeoutHandler(IRoomRepository roomRepository, IMatchEnder matchEnder)
        {
            _roomRepository = roomRepository;
            _matchEnder = matchEnder;
        }

        public async Task HandleTurnTimeoutAsync(
            Guid roomId,
            Guid playerId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room is null || room.Status == RoomStatus.Finished)
                return;

            if (room.CurrentTurn != playerId)
                return;

            if (DateTime.UtcNow < room.TurnDeadline)
                return;

            var result = playerId == room.PlayerA.PlayerId
                ? MatchResultType.PlayerBWin
                : MatchResultType.PlayerAWin;

            await _matchEnder.EndMatchAsync(roomId, result, cancellationToken);
        }
    }
}