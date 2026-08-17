namespace CaroGame.Application.UseCases.GamePlay
{
    using CaroGame.Application.Interfaces.Repositories;
    using CaroGame.Domain.Entities;
    using CaroGame.Domain.Enum;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class EndMatchUseCase : IMatchEnder
    {
        private readonly IRoomRepository _roomRepository;

        public EndMatchUseCase(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task<Room> EndMatchAsync(Guid roomId, MatchResultType matchResultType, CancellationToken cancellationToken)
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room is null)
                throw new InvalidOperationException("Room not found.");

            room.EndMatch(matchResultType);

            await _roomRepository.UpdateAsync(room);

            // history persistence and notifications handled elsewhere

            return room;
        }
    }
}

