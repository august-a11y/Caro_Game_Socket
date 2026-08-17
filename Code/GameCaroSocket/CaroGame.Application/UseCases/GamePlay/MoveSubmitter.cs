using System.Threading;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Application.UseCases.GamePlay
{
    public sealed class MoveSubmitter : IMoveSubmitter
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMatchEnder _matchEnder;

        public MoveSubmitter(IRoomRepository roomRepository, IMatchEnder matchEnder)
        {
            _roomRepository = roomRepository;
            _matchEnder = matchEnder;
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

            if (room.Status == RoomStatus.Finished)
                throw new InvalidOperationException("This match has already finished.");

            room.MarkPlaying();

            room.ApplyMove(playerId, position);

            var result = room.CheckWinCondition();

            if (result != MatchResultType.Continue)
            {
                return await _matchEnder.EndMatchAsync(roomId, result, cancellationToken);
            }

            await _roomRepository.UpdateAsync(room);

            return room;
        }
    }
}