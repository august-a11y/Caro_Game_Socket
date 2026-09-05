using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay
{
    public interface IMatchEnder
    {
        Task<Room> EndMatchAsync(
            Room room,
            MatchResultType matchResultType,
            CancellationToken cancellationToken);
    }
}

