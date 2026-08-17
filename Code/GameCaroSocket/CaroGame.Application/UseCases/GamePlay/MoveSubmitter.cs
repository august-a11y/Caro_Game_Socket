namespace CaroGame.Application.UseCases.GamePlay
{
    using CaroGame.Application.Interfaces.Repositories;
    using CaroGame.Domain.Entities;
    using CaroGame.Domain.ValueObjects;
    using System.Threading;
    using System.Threading.Tasks;
    using System;

    public sealed class MoveSubmitter : IMoveSubmitter
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMatchEnder _matchEnder;

        public MoveSubmitter(IRoomRepository roomRepository, IMatchEnder matchEnder)
        {
            _roomRepository = roomRepository;
            _matchEnder = matchEnder;
        }

        public async Task<Room> SubmitMoveAsync(Guid roomId, Guid playerId, Position position, CancellationToken cancellationToken)
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room is null)
                throw new InvalidOperationException("Room not found.");

            room.ApplyMove(playerId, position);

            var result = room.CheckWinCondition();

            if (result != Domain.Enum.MatchResultType.Continue)
            {
                // delegate to match ender to finalize
                return await _matchEnder.EndMatchAsync(roomId, result, cancellationToken);
            }

            await _roomRepository.UpdateAsync(room);
            return room;
        }
    }
}

