using System.Threading;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;

namespace CaroGame.Application.UseCases.GamePlay
{
    public interface IMatchEnder
    {
        Task<Room> EndMatchAsync(Guid roomId, MatchResultType matchResultType, CancellationToken cancellationToken);
    }
}