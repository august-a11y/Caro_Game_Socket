namespace CaroGame.Application.UseCases.GamePlay
{
    using CaroGame.Application.Interfaces.Repositories;
    using CaroGame.Domain.Enum;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class TurnTimeoutHandler : ITurnTimeoutHandler
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMatchEnder _matchEnder;

        public TurnTimeoutHandler(IRoomRepository roomRepository, IMatchEnder matchEnder)
        {
            _roomRepository = roomRepository;
            _matchEnder = matchEnder;
        }

        public async Task HandleTurnTimeoutAsync(Guid roomId, Guid playerId, CancellationToken cancellationToken)
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room is null)
                return;

            // Only handle timeouts for playing rooms
            if (room.Status != RoomStatus.Playing)
                return;

            // Only proceed if it is indeed the player's turn
            if (room.CurrentTurn != playerId)
                return;

            // Ensure the deadline has been reached
            if (room.TurnDeadline == DateTime.MinValue || DateTime.UtcNow < room.TurnDeadline)
                return;

            // Opponent wins by timeout
            var winner = room.PlayerA.PlayerId == playerId ? MatchResultType.PlayerBWin : MatchResultType.PlayerAWin;

            await _matchEnder.EndMatchAsync(roomId, winner, cancellationToken);
        }
    }
}

