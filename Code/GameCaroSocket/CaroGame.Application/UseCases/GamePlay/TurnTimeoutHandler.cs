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
            if (room is null || room.CurrentMatch is null)
                return;

            var turnManager = room.CurrentMatch.TurnManager;

            if (turnManager.CurrentTurnPlayerId != playerId)
                return;

            if (!turnManager.IsTimeUp(DateTime.UtcNow))
                return;

            var result = playerId == room.CurrentMatch.PlayerXId
                ? MatchResultType.PlayerOWin
                : MatchResultType.PlayerXWin;

            await _matchEnder.EndMatchAsync(roomId, result, cancellationToken);
        }
    }
}