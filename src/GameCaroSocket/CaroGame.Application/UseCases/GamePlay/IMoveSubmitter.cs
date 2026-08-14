using CaroGame.Domain.Entities;
using CaroGame.Domain.ValueObjects;

namespace CaroGame.Application.UseCases.GamePlay
{
    public interface IMoveSubmitter
    {
        Task<Room> SubmitMoveAsync(Guid roomId, Guid playerId, Position position, CancellationToken cancellationToken);
    }
}

