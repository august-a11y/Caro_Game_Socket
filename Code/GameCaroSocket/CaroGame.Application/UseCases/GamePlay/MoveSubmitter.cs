using System.Threading;
using System.Linq;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.Services;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Application.UseCases.GamePlay
{
    public sealed class MoveSubmitter : IMoveSubmitter
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMatchEnder _matchEnder;
        private readonly IWinConditionChecker _winConditionChecker;

        public MoveSubmitter(
            IRoomRepository roomRepository,
            IMatchEnder matchEnder,
            IWinConditionChecker winConditionChecker)
        {
            _roomRepository = roomRepository;
            _matchEnder = matchEnder;
            _winConditionChecker = winConditionChecker;
        }

        public async Task<Room> SubmitMoveAsync(
            Guid roomId,
            Guid playerId,
            Position position,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var room = await _roomRepository.GetByIdAsync(roomId)
                ?? throw new InvalidOperationException($"Room '{roomId}' was not found.");

            if (room.CurrentMatch is null)
                throw new InvalidOperationException("Room chua co tran dau nao dang dien ra.");

            room.CurrentMatch.ApplyMove(playerId, position);

            var lastMove = room.CurrentMatch.MoveHistory.Last();

            var result = _winConditionChecker.Check(room.CurrentMatch.Board, lastMove);

            if (result != MatchResultType.Continue)
            {
                return await _matchEnder.EndMatchAsync(roomId, result, cancellationToken);
            }

            await _roomRepository.UpdateAsync(room);

            return room;
        }
    }
}